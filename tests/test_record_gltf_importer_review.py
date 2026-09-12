"""Exercise test-time identity safeguards for the Edit Mode importer receipt."""

from __future__ import annotations

import hashlib
import importlib.util
import io
import json
import sys
import tempfile
import unittest
from contextlib import redirect_stdout
from pathlib import Path
from unittest import mock


ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts/record_gltf_importer_review.py"

SPEC = importlib.util.spec_from_file_location("record_gltf_importer_review", SCRIPT)
if SPEC is None or SPEC.loader is None:
    raise RuntimeError(f"Cannot import recorder: {SCRIPT}")
RECORDER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(RECORDER)


class GltfImporterReviewRecorderTests(unittest.TestCase):
    def test_require_bound_hashes_accepts_matching_test_time_identity(self) -> None:
        expected = {"Runtime/Importer.cs": "expected-sha256"}

        RECORDER.require_bound_hashes(
            {"harnessSha256": expected}, "harnessSha256", expected, "harness"
        )

    def test_require_bound_hashes_refuses_missing_or_mismatched_identity(self) -> None:
        expected = {"Runtime/Importer.cs": "expected-sha256"}
        cases = (
            ({}, "no test-time harness identity"),
            ({"harnessSha256": ["not-a-map"]}, "no test-time harness identity"),
            ({"harnessSha256": {"Runtime/Importer.cs": "different"}}, "differs"),
        )

        for report, message in cases:
            with self.subTest(report=report), self.assertRaisesRegex(ValueError, message):
                RECORDER.require_bound_hashes(
                    report, "harnessSha256", expected, "harness"
                )

    def test_fixture_hashes_excludes_generated_meta_files(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            fixtures = Path(directory) / "Fixtures"
            fixtures.mkdir()
            fixture = fixtures / "triangle.glb.bytes"
            fixture.write_bytes(b"fixture")
            (fixtures / "triangle.glb.bytes.meta").write_text("generated metadata")
            (fixtures / "Nested.META").write_text("generated metadata")

            self.assertEqual(
                RECORDER.fixture_hashes(fixtures),
                {"triangle.glb.bytes": hashlib.sha256(b"fixture").hexdigest()},
            )

    def test_fixture_hashes_refuses_missing_fixture_directory(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            with self.assertRaisesRegex(ValueError, "Missing trial fixture directory"):
                RECORDER.fixture_hashes(Path(directory) / "Fixtures")

    def test_main_refuses_missing_or_mismatched_test_time_report_identity(self) -> None:
        cases = (
            ("missing_harness", "no test-time harness identity"),
            ("mismatched_harness", "harness identity differs"),
            ("mismatched_fixture", "fixture identity differs"),
        )
        for condition, message in cases:
            with self.subTest(condition=condition), tempfile.TemporaryDirectory() as directory:
                root, output, report = self._fixture(Path(directory))
                if condition == "missing_harness":
                    report.pop("harnessSha256")
                elif condition == "mismatched_harness":
                    report["harnessSha256"] = {"Runtime/Importer.cs": "different"}
                else:
                    report["fixtureSha256"] = {"triangle.glb.bytes": "different"}
                trial = Path(
                    json.loads((root / "build/gltf-importer-trial/trial.json").read_text())["project"]
                )
                (trial / "build/importer-editor-report.json").write_text(json.dumps(report))

                with self.assertRaisesRegex(ValueError, message):
                    self._run_main(root, output)
                self.assertFalse(output.exists())

    def test_main_refuses_fixture_manifest_mismatch_before_writing_receipt(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root, output, report = self._fixture(Path(directory))
            baseline_path = root / "build/gltf-importer-trial/trial.json"
            baseline = json.loads(baseline_path.read_text())
            baseline["fixtureManifest"]["files"][0]["sha256"] = "different"
            baseline_path.write_text(json.dumps(baseline))
            trial = Path(baseline["project"])
            (trial / "build/importer-editor-report.json").write_text(json.dumps(report))

            with self.assertRaisesRegex(ValueError, "Trial fixtures differ"):
                self._run_main(root, output)
            self.assertFalse(output.exists())

    def test_main_refuses_an_extra_trial_harness_file_before_writing_receipt(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root, output, _ = self._fixture(Path(directory))
            trial = Path(
                json.loads((root / "build/gltf-importer-trial/trial.json").read_text())["project"]
            )
            (trial / "Assets/Trial/Runtime/Unexpected.cs").write_text("// unreviewed\n")

            with self.assertRaisesRegex(ValueError, "Trial harness differs"):
                self._run_main(root, output)
            self.assertFalse(output.exists())

    def _fixture(self, root: Path) -> tuple[Path, Path, dict]:
        source = root / "tools/gltf-importer-trial/unity"
        source_file = source / "Runtime/Importer.cs"
        source_file.parent.mkdir(parents=True)
        source_file.write_text("// reviewed source\n")
        (source / "Runtime/Importer.cs.meta").write_text("Unity-generated metadata")

        trial = root / "trial"
        trial_source = trial / "Assets/Trial/Runtime"
        trial_source.mkdir(parents=True)
        (trial_source / "Importer.cs").write_text(source_file.read_text())
        (trial_source / "Importer.cs.meta").write_text("Unity-generated metadata")
        (trial / "trial-project.marker").write_text("isolated trial\n")
        fixtures = trial / "Assets/StreamingAssets/Fixtures"
        fixtures.mkdir(parents=True)
        fixture = fixtures / "triangle.glb.bytes"
        fixture.write_bytes(b"fixture")
        (fixtures / "triangle.glb.bytes.meta").write_text("Unity-generated metadata")

        harness = RECORDER.file_hashes(source, exclude_meta=True)
        fixture_hashes = RECORDER.fixture_hashes(fixtures)
        report = {"harnessSha256": harness, "fixtureSha256": fixture_hashes, "results": []}
        build = trial / "build"
        build.mkdir()
        (build / "importer-editor-report.json").write_text(json.dumps(report))

        work = root / "build/gltf-importer-trial"
        work.mkdir(parents=True)
        baseline = {
            "project": str(trial),
            "fixtureManifest": {
                "files": [
                    {
                        "path": "triangle.glb",
                        "sha256": fixture_hashes["triangle.glb.bytes"],
                    }
                ]
            },
        }
        (work / "trial.json").write_text(json.dumps(baseline))
        return root, root / "receipt.json", report

    @staticmethod
    def _run_main(root: Path, output: Path) -> None:
        with (
            mock.patch.object(RECORDER, "ROOT", root),
            mock.patch.object(RECORDER, "WORK", root / "build/gltf-importer-trial"),
            mock.patch.object(sys, "argv", [str(SCRIPT), "--output", str(output)]),
        ):
            with redirect_stdout(io.StringIO()):
                RECORDER.main()


if __name__ == "__main__":
    unittest.main()
