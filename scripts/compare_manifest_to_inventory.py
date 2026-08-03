#!/usr/bin/env python3
"""Cross-check AssetBundle manifest against Azure inventory CSV (#6).

Usage:
  python3 scripts/compare_manifest_to_inventory.py
  python3 scripts/compare_manifest_to_inventory.py --platform android

Compares `docs/assetbundle-metadata-manifest.json` blob names and sizes to
`docs/azure-haringerverdiag-inventory.csv` for the three platform containers
(android, ios, wsa). Exit 0 when every manifest entry exists in inventory with
matching size; exit 1 when mismatches are found.

See docs/assetbundle-rebake.md and docs/azure-storage-inventory.md.
"""

from __future__ import annotations

import argparse
import csv
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_MANIFEST = ROOT / "docs" / "assetbundle-metadata-manifest.json"
DEFAULT_INVENTORY = ROOT / "docs" / "azure-haringerverdiag-inventory.csv"
PLATFORM_CONTAINERS = ("android", "ios", "wsa")


def load_manifest(path: Path) -> dict:
    return json.loads(path.read_text())


def load_inventory(path: Path) -> dict[str, dict[str, int]]:
    """Map container -> blob name -> size bytes."""
    by_container: dict[str, dict[str, int]] = {name: {} for name in PLATFORM_CONTAINERS}
    with path.open(newline="", encoding="utf-8") as handle:
        reader = csv.DictReader(handle)
        for row in reader:
            container = row["Container"].strip()
            if container not in by_container:
                continue
            blob_name = row["BlobName"].strip()
            by_container[container][blob_name] = int(row["Size_Bytes"])
    return by_container


def compare_platform(
    platform: str,
    expected: dict[str, int],
    inventory: dict[str, int],
) -> tuple[list[str], list[str], list[str]]:
    missing = sorted(set(expected) - set(inventory))
    extra = sorted(set(inventory) - set(expected))
    size_mismatch = sorted(
        name for name in expected if name in inventory and inventory[name] != expected[name]
    )
    return missing, extra, size_mismatch


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--manifest",
        type=Path,
        default=DEFAULT_MANIFEST,
        help="Path to assetbundle-metadata-manifest.json",
    )
    parser.add_argument(
        "--inventory",
        type=Path,
        default=DEFAULT_INVENTORY,
        help="Path to azure-haringerverdiag-inventory.csv",
    )
    parser.add_argument(
        "--platform",
        choices=PLATFORM_CONTAINERS,
        help="Check one platform container only (default: all three)",
    )
    args = parser.parse_args()

    manifest = load_manifest(args.manifest)
    inventory = load_inventory(args.inventory)
    containers = manifest.get("containers", {})
    platforms = [args.platform] if args.platform else list(PLATFORM_CONTAINERS)

    exit_code = 0
    for platform in platforms:
        if platform not in containers:
            print(f"Platform '{platform}' not in manifest.", file=sys.stderr)
            exit_code = 2
            continue

        expected = {entry["name"]: entry["size"] for entry in containers[platform]}
        inv = inventory.get(platform, {})
        missing, extra, size_mismatch = compare_platform(platform, expected, inv)

        print(f"\n== {platform} ==")
        print(f"Manifest entries: {len(expected)}")
        print(f"Inventory blobs:  {len(inv)}")
        print(f"Missing:          {len(missing)}")
        print(f"Extra in Azure:   {len(extra)}")
        print(f"Size mismatch:    {len(size_mismatch)}")

        if missing:
            exit_code = 1
            print("\nMissing from inventory (first 10):")
            for name in missing[:10]:
                print(f"  - {name}")

        if size_mismatch:
            exit_code = 1
            print("\nSize mismatch (first 10):")
            for name in size_mismatch[:10]:
                print(
                    f"  - {name}: manifest={expected[name]} inventory={inv[name]}"
                )

        if not missing and not size_mismatch:
            print("OK — manifest matches inventory for this container.")

    return exit_code


if __name__ == "__main__":
    raise SystemExit(main())
