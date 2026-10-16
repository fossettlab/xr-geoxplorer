"""Exercise the offline safeguards around the Android glTF trial recorder."""

from __future__ import annotations

import importlib.util
import hashlib
import io
import json
import shutil
import sys
import tempfile
import unittest
import zipfile
from contextlib import redirect_stdout
from pathlib import Path
from unittest import mock


ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts/record_gltf_importer_android.py"
SOURCE = ROOT / "tools/gltf-importer-trial/unity"
REQUIRED_SHADERS = {
    "glTF/PbrMetallicRoughness",
    "glTF/PbrSpecularGlossiness",
    "glTF/Unlit",
}

SPEC = importlib.util.spec_from_file_location("record_gltf_importer_android", SCRIPT)
if SPEC is None or SPEC.loader is None:
    raise RuntimeError(f"Cannot import recorder: {SCRIPT}")
RECORDER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(RECORDER)


class GltfImporterAndroidRecorderTests(unittest.TestCase):
    def test_artifact_guard_rejects_each_existing_output_without_modifying_it(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            apk = root / "trial.apk"
            receipt = root / "trial.json"
            for existing, other in ((apk, receipt), (receipt, apk)):
                existing.write_bytes(b"pre-existing evidence")
                before = existing.read_bytes()
                with self.subTest(existing=existing.name), self.assertRaisesRegex(
                    FileExistsError, existing.name
                ):
                    RECORDER.ensure_new_artifacts(apk, receipt)
                self.assertEqual(existing.read_bytes(), before)
                existing.unlink()
                self.assertFalse(other.exists())

    def test_artifact_guard_allows_two_new_output_paths(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            RECORDER.ensure_new_artifacts(root / "trial.apk", root / "trial.json")
            self.assertEqual(list(root.iterdir()), [])

    def test_harness_identity_records_every_matching_source_file(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            trial = Path(directory) / "trial"
            destination = trial / "Assets/Trial"
            shutil.copytree(SOURCE, destination)

            expected = {
                str(path.relative_to(SOURCE)): RECORDER.identity(path)["sha256"]
                for path in sorted(SOURCE.rglob("*"))
                if path.is_file()
            }
            self.assertEqual(RECORDER.harness_identity(trial), expected)

    def test_harness_identity_rejects_changed_trial_source(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            trial = Path(directory) / "trial"
            destination = trial / "Assets/Trial"
            shutil.copytree(SOURCE, destination)
            changed = next(path for path in sorted(SOURCE.rglob("*.cs")))
            trial_copy = destination / changed.relative_to(SOURCE)
            trial_copy.write_bytes(trial_copy.read_bytes() + b"\n")

            with self.assertRaisesRegex(
                ValueError, str(changed.relative_to(SOURCE)).replace(".", r"\.")
            ):
                RECORDER.harness_identity(trial)

    def test_recorder_writes_receipt_after_all_offline_gates_pass(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root, trial, apk, manifest = self._fixture(Path(directory))
            output = self._run_recorder(root, "2026-09-12")

            receipt_path = root / "docs/contracts/gltf-importer-android-2026-09-12.json"
            receipt = json.loads(receipt_path.read_text())
            self.assertEqual(receipt["status"], "device_execution_pending")
            self.assertFalse(receipt["productionAdoptionApproved"])
            self.assertFalse(receipt["questExecutionObserved"])
            self.assertEqual(receipt["harnessSha256"], self._harness_hashes(trial, root))
            self.assertEqual(receipt["apk"]["sha256"], RECORDER.identity(apk)["sha256"])
            self.assertEqual(receipt["apk"]["fixtures"]["triangle.glb"], manifest["files"][0]["sha256"])
            self.assertTrue(REQUIRED_SHADERS.issubset(receipt["unity"]["includedShaders"]))
            self.assertEqual(receipt["applicationIntegrity"]["changed"], [])
            self.assertIn('"deviceExecution": "pending"', output)

    def test_recorder_refuses_tampered_build_identity_before_copying_apk(self) -> None:
        for field, value in (
            ("harnessSha256", {"Runtime/ImporterPlayer.cs": "tampered"}),
            ("apkSha256", "tampered"),
        ):
            with self.subTest(field=field), tempfile.TemporaryDirectory() as directory:
                root, trial, _, _ = self._fixture(Path(directory))
                identity_path = trial / "build/build-identity.json"
                identity_data = json.loads(identity_path.read_text())
                identity_data[field] = value
                identity_path.write_text(json.dumps(identity_data))

                with self.assertRaisesRegex(ValueError, "identity"):
                    self._run_recorder(root, "2026-09-12")
                self.assertFalse(
                    (root / "build/gltf-importer-trial/GeoX-GltfTrial-2026-09-12.apk").exists()
                )
                self.assertFalse(
                    (root / "docs/contracts/gltf-importer-android-2026-09-12.json").exists()
                )

    def test_recorder_refuses_missing_required_shader_before_copying_apk(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root, trial, _, _ = self._fixture(Path(directory))
            (trial / "build/included-shaders.txt").write_text(
                "\n".join(sorted(REQUIRED_SHADERS - {"glTF/Unlit"})) + "\n"
            )

            with self.assertRaisesRegex(RuntimeError, "required glTFast shaders"):
                self._run_recorder(root, "2026-09-12")
            self.assertFalse(
                (root / "build/gltf-importer-trial/GeoX-GltfTrial-2026-09-12.apk").exists()
            )

    def _fixture(self, root: Path) -> tuple[Path, Path, Path, dict]:
        """Create a minimal isolated trial that exercises recorder.main offline."""
        (root / "ProjectSettings").mkdir(parents=True)
        (root / "Packages").mkdir()
        (root / "Assets").mkdir()
        (root / "ProjectSettings/ProjectVersion.txt").write_text(
            "m_EditorVersion: 6000.4.4f1\n"
        )
        source = root / "tools/gltf-importer-trial/unity"
        shutil.copytree(SOURCE, source)

        trial = root / "trial-project"
        (trial / "Assets/Trial").mkdir(parents=True)
        shutil.copytree(source, trial / "Assets/Trial", dirs_exist_ok=True)
        (trial / "trial-project.marker").write_text("isolated trial\n")
        build = trial / "build"
        build.mkdir()
        apk = build / "GeoX-GltfTrial.apk"
        fixture_data = b"synthetic triangle fixture"
        with zipfile.ZipFile(apk, "w") as archive:
            archive.writestr("lib/arm64-v8a/libil2cpp.so", b"il2cpp")
            archive.writestr("assets/Fixtures/triangle.glb.bytes", fixture_data)
        manifest = {
            "files": [
                {
                    "path": "triangle.glb",
                    "bytes": len(fixture_data),
                    "sha256": hashlib.sha256(fixture_data).hexdigest(),
                }
            ]
        }
        manifest_path = root / "tests/fixtures/model-inputs/generated/manifest.json"
        manifest_path.parent.mkdir(parents=True)
        manifest_path.write_text(json.dumps(manifest))
        (build / "build-result.txt").write_text("Succeeded\nErrors: 0\nWarnings: 0\n")
        (build / "included-shaders.txt").write_text("\n".join(sorted(REQUIRED_SHADERS)) + "\n")
        harness = self._harness_hashes(trial, root)
        (build / "build-identity.json").write_text(
            json.dumps({"harnessSha256": harness, "apkSha256": RECORDER.identity(apk)["sha256"]})
        )

        baseline_file = root / "ProjectSettings/ProjectVersion.txt"
        baseline = {
            "project": str(trial),
            "applicationSources": {
                "ProjectSettings/ProjectVersion.txt": RECORDER.identity(baseline_file)["sha256"]
            },
        }
        work = root / "build/gltf-importer-trial"
        work.mkdir(parents=True)
        (work / "trial.json").write_text(json.dumps(baseline))
        (root / "docs/contracts").mkdir(parents=True)
        return root, trial, apk, manifest

    @staticmethod
    def _harness_hashes(trial: Path, root: Path) -> dict[str, str]:
        with mock.patch.object(RECORDER, "ROOT", root):
            return RECORDER.harness_identity(trial)

    def _run_recorder(self, root: Path, date: str) -> str:
        def fake_run(command: list[str], **_: object) -> mock.Mock:
            tool = Path(command[0]).name
            outputs = {
                "apksigner": "Verifies\n",
                "zipalign": "Verification successful\n",
                "aapt": (
                    "package: name='edu.wustl.fossett.geoxgltftrial'\n"
                    "sdkVersion:'29'\n"
                    "targetSdkVersion:'34'\n"
                    "native-code: 'arm64-v8a'\n"
                    "launchable-activity: name='Trial'\n"
                ),
            }
            return mock.Mock(returncode=0, stdout=outputs[tool])

        work = root / "build/gltf-importer-trial"
        with (
            mock.patch.object(RECORDER, "ROOT", root),
            mock.patch.object(RECORDER, "WORK", work),
            mock.patch.object(RECORDER.subprocess, "run", side_effect=fake_run),
            mock.patch.object(sys, "argv", [str(SCRIPT), "--date", date]),
        ):
            output = io.StringIO()
            with redirect_stdout(output):
                RECORDER.main()
            return output.getvalue()


if __name__ == "__main__":
    unittest.main()
