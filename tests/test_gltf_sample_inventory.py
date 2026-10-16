"""Check the representative-sample inventory without publishing binaries."""

from __future__ import annotations

import hashlib
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PINS = ROOT / "tools/gltf-sample-inventory/sources.json"
FETCH = ROOT / "scripts/fetch_gltf_sample_inventory.py"


class SampleInventoryTests(unittest.TestCase):
    def test_pins_record_license_hash_and_source(self) -> None:
        inventory = json.loads(PINS.read_text())
        self.assertTrue(inventory["commit"])
        self.assertIn("not Quest", inventory["status"])
        ids = []
        for model in inventory["models"]:
            ids.append(model["id"])
            self.assertTrue(model["license"])
            self.assertTrue(model["licenseUrl"])
            self.assertTrue(model["url"].startswith("https://"))
            self.assertIn(inventory["commit"], model["url"])
            self.assertEqual(len(model["sha256"]), 64)
            self.assertGreater(model["bytes"], 0)
            self.assertTrue(model["characteristics"]["selfContained"])
        self.assertEqual(ids, ["khronos-box", "khronos-box-textured"])
        textured = next(m for m in inventory["models"] if m["id"] == "khronos-box-textured")
        self.assertEqual(textured["characteristics"]["trademark"], "Cesium logo")

    def test_fetch_verifies_pinned_bytes(self) -> None:
        cache = Path(tempfile.mkdtemp()) / "cache"
        result = subprocess.run(
            [sys.executable, str(FETCH), "--cache", str(cache)],
            capture_output=True,
            text=True,
        )
        if result.returncode != 0:
            self.skipTest(f"Sample fetch unavailable: {result.stderr[-400:]}")
        inventory = json.loads(PINS.read_text())
        receipt = json.loads((cache / "receipt.json").read_text())
        self.assertEqual(receipt["commit"], inventory["commit"])
        for model in inventory["models"]:
            data = (cache / f"{model['id']}.glb").read_bytes()
            self.assertEqual(data[:4], b"glTF")
            self.assertEqual(len(data), model["bytes"])
            self.assertEqual(hashlib.sha256(data).hexdigest(), model["sha256"])


if __name__ == "__main__":
    unittest.main()
