"""Fetch hash-pinned Khronos samples into a local cache. Do not commit the binaries."""

from __future__ import annotations

import argparse
import hashlib
import json
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PINS = ROOT / "tools/gltf-sample-inventory/sources.json"
CACHE = ROOT / "build/gltf-sample-inventory"


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cache", type=Path, default=CACHE)
    args = parser.parse_args()
    inventory = json.loads(PINS.read_text())
    args.cache.mkdir(parents=True, exist_ok=True)
    receipt = []
    for model in inventory["models"]:
        destination = args.cache / (model["id"] + ".glb")
        if destination.exists():
            data = destination.read_bytes()
        else:
            with urllib.request.urlopen(model["url"], timeout=30) as response:
                data = response.read()
        if len(data) != model["bytes"] or hashlib.sha256(data).hexdigest() != model["sha256"]:
            raise ValueError(f"Sample hash mismatch: {model['id']}")
        if data[:4] != b"glTF":
            raise ValueError(f"Sample is not a GLB: {model['id']}")
        if not destination.exists():
            destination.write_bytes(data)
        receipt.append(
            {
                "id": model["id"],
                "path": str(destination),
                "bytes": len(data),
                "sha256": model["sha256"],
                "license": model["license"],
            }
        )
        print(f"Verified {model['id']} {model['license']} {model['bytes']} bytes")
    (args.cache / "receipt.json").write_text(
        json.dumps(
            {
                "commit": inventory["commit"],
                "status": inventory["status"],
                "models": receipt,
            },
            indent=2,
        )
        + "\n"
    )


if __name__ == "__main__":
    main()
