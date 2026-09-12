"""Offline fixture integrity checks, not a replacement for Khronos validation."""

import hashlib
import json
import struct
import subprocess
import sys
import tempfile
import unittest
import zlib
from pathlib import Path

GENERATOR = (
    Path(__file__).resolve().parents[1] / "scripts/generate_model_input_fixtures.py"
)


class ModelFixtureTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.temp = tempfile.TemporaryDirectory()
        cls.addClassCleanup(cls.temp.cleanup)
        cls.output = Path(cls.temp.name) / "fixtures"
        subprocess.run(
            [sys.executable, str(GENERATOR), "--output", str(cls.output)],
            check=True,
            capture_output=True,
        )

    def test_manifest_hashes_and_determinism(self) -> None:
        other = Path(self.temp.name) / "repeat"
        subprocess.run(
            [sys.executable, str(GENERATOR), "--output", str(other)],
            check=True,
            capture_output=True,
        )
        manifest = json.loads((self.output / "manifest.json").read_text())
        for item in manifest["files"]:
            data = (self.output / item["path"]).read_bytes()
            self.assertEqual(len(data), item["bytes"])
            self.assertEqual(hashlib.sha256(data).hexdigest(), item["sha256"])
            self.assertEqual(data, (other / item["path"]).read_bytes())
        self.assertEqual(
            (self.output / "manifest.json").read_bytes(),
            (other / "manifest.json").read_bytes(),
        )

    def test_existing_output_is_preserved(self) -> None:
        before = (self.output / "manifest.json").read_bytes()
        result = subprocess.run(
            [sys.executable, str(GENERATOR), "--output", str(self.output)],
            capture_output=True,
        )
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual((self.output / "manifest.json").read_bytes(), before)

    def test_expected_containers_and_buffer_ranges(self) -> None:
        for name in [
            "triangle.glb",
            "hierarchy.glb",
            "textured.glb",
            "external-image.glb",
            "missing-image.glb",
            "unsupported-required.glb",
        ]:
            with self.subTest(name=name):
                data = (self.output / name).read_bytes()
                self.assertEqual(data[:4], b"glTF")
                version, total = struct.unpack_from("<II", data, 4)
                self.assertEqual(version, 2)
                self.assertEqual(total, len(data))
                json_length = struct.unpack_from("<I", data, 12)[0]
                self.assertEqual(data[16:20], b"JSON")
                self.assertEqual(json_length % 4, 0)
                doc = json.loads(data[20 : 20 + json_length])
                offset = 20 + json_length
                binary_length = struct.unpack_from("<I", data, offset)[0]
                self.assertEqual(data[offset + 4 : offset + 8], b"BIN\0")
                self.assertEqual(offset + 8 + binary_length, len(data))
                self.assertGreaterEqual(binary_length, doc["buffers"][0]["byteLength"])
                for view in doc["bufferViews"]:
                    self.assertLessEqual(
                        view.get("byteOffset", 0) + view["byteLength"],
                        doc["buffers"][0]["byteLength"],
                    )

    def test_deliberately_bad_containers_remain_bad(self) -> None:
        bad = (self.output / "bad-magic.glb").read_bytes()
        self.assertNotEqual(bad[:4], b"glTF")
        truncated = (self.output / "truncated.glb").read_bytes()
        self.assertGreater(struct.unpack_from("<I", truncated, 8)[0], len(truncated))
        self.assertFalse((self.output / "missing.png").exists())

    def test_pixel_crc_and_decoded_payload(self) -> None:
        data = (self.output / "pixel.png").read_bytes()
        self.assertEqual(data[:8], b"\x89PNG\r\n\x1a\n")
        offset, payload = 8, b""
        kinds = []
        while offset < len(data):
            length = struct.unpack_from(">I", data, offset)[0]
            kind = data[offset + 4 : offset + 8]
            body = data[offset + 8 : offset + 8 + length]
            crc = struct.unpack_from(">I", data, offset + 8 + length)[0]
            self.assertEqual(zlib.crc32(kind + body), crc)
            kinds.append(kind)
            if kind == b"IHDR":
                self.assertEqual(struct.unpack(">IIBBBBB", body), (1, 1, 8, 6, 0, 0, 0))
            if kind == b"IDAT":
                payload += body
            offset += length + 12
        self.assertEqual(kinds, [b"IHDR", b"IDAT", b"IEND"])
        self.assertEqual(zlib.decompress(payload), bytes((0, 64, 128, 192, 255)))


if __name__ == "__main__":
    unittest.main()
