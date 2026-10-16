"""Desk measurements for authored pressure workloads. Not Quest budgets."""

from __future__ import annotations

import hashlib
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

GENERATOR = (
    Path(__file__).resolve().parents[1] / "scripts/generate_model_input_workloads.py"
)


class WorkloadFixtureTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.temp = tempfile.TemporaryDirectory()
        cls.addClassCleanup(cls.temp.cleanup)
        cls.output = Path(cls.temp.name) / "workloads"
        subprocess.run(
            [sys.executable, str(GENERATOR), "--output", str(cls.output)],
            check=True,
            capture_output=True,
        )

    def test_manifest_hashes_parameters_and_measures(self) -> None:
        manifest = json.loads((self.output / "manifest.json").read_text())
        self.assertIn("not Quest", manifest["status"])
        by_name = {item["path"]: item for item in manifest["files"]}
        self.assertEqual(
            set(by_name),
            {"mesh-pressure.glb", "texture-pressure.glb", "hierarchy-pressure.glb"},
        )
        for item in manifest["files"]:
            data = (self.output / item["path"]).read_bytes()
            self.assertEqual(data[:4], b"glTF")
            self.assertEqual(len(data), item["bytes"])
            self.assertEqual(hashlib.sha256(data).hexdigest(), item["sha256"])

        mesh = by_name["mesh-pressure.glb"]
        self.assertEqual(mesh["parameters"], {"columns": 16, "rows": 16})
        self.assertEqual(mesh["measures"]["vertices"], 17 * 17)
        self.assertEqual(mesh["measures"]["triangles"], 16 * 16 * 2)
        self.assertEqual(
            mesh["measures"]["decodedPositionBytes"], 17 * 17 * 3 * 4
        )

        texture = by_name["texture-pressure.glb"]
        self.assertEqual(texture["measures"]["decodedRgbaBytes"], 64 * 64 * 4)
        self.assertGreater(texture["measures"]["expansionRatio"], 1)
        self.assertLess(texture["measures"]["pngBytes"], texture["measures"]["decodedRgbaBytes"])

        hierarchy = by_name["hierarchy-pressure.glb"]
        self.assertEqual(hierarchy["measures"]["nodes"], 32)

    def test_existing_output_is_preserved(self) -> None:
        before = (self.output / "manifest.json").read_bytes()
        result = subprocess.run(
            [sys.executable, str(GENERATOR), "--output", str(self.output)],
            capture_output=True,
        )
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual((self.output / "manifest.json").read_bytes(), before)


if __name__ == "__main__":
    unittest.main()
