#!/usr/bin/env python3
"""Fetch hash-pinned public importer/validator archives without npm install hooks."""

import hashlib
import json
import tarfile
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / "build/gltf-importer-trial"


def main() -> None:
    pins = json.loads((ROOT / "tools/gltf-importer-trial/sources.json").read_text())
    WORK.mkdir(parents=True, exist_ok=True)
    for name, filename in [
        ("gltfast", "gltfast-6.20.0.tgz"),
        ("validator", "gltf-validator.tgz"),
    ]:
        pin = pins[name]
        archive = WORK / filename
        if archive.exists():
            data = archive.read_bytes()
        else:
            with urllib.request.urlopen(pin["url"], timeout=30) as response:
                data = response.read()
        if hashlib.sha256(data).hexdigest() != pin["sha256"]:
            raise ValueError(f"Archive hash mismatch: {name}")
        if not archive.exists():
            archive.write_bytes(data)
        with tarfile.open(archive) as bundle:
            bundle.extractall(WORK / name, filter="data")
        print(f"Verified and extracted {name} {pin['version']}")


if __name__ == "__main__":
    main()
