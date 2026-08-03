#!/usr/bin/env python3
"""Compare a local AssetBundle build output tree against the deployed manifest (#6).

Usage:
  python3 scripts/compare_bundle_manifest.py \\
    --platform android \\
    --build-dir AssetBundles/android

Exit 0 when every manifest blob exists locally with matching size (strict mode),
or when reporting available-source partial coverage (--allow-missing).

See docs/assetbundle-rebake.md for pipeline context.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_MANIFEST = ROOT / "docs" / "assetbundle-metadata-manifest.json"


def load_manifest(path: Path) -> dict:
    return json.loads(path.read_text())


def local_bundle_map(build_dir: Path) -> dict[str, int]:
    """Map manifest-style blob name -> file size in bytes."""
    result: dict[str, int] = {}
    if not build_dir.is_dir():
        return result
    for file_path in build_dir.rglob("*"):
        if not file_path.is_file():
            continue
        if file_path.name.endswith(".manifest") or file_path.name == "Android":
            continue
        rel = file_path.relative_to(build_dir).as_posix()
        result[rel] = file_path.stat().st_size
    return result


def compare(platform: str, build_dir: Path, manifest_path: Path, allow_missing: bool) -> int:
    manifest = load_manifest(manifest_path)
    containers = manifest.get("containers", {})
    if platform not in containers:
        print(f"Platform '{platform}' not in manifest.", file=sys.stderr)
        return 2

    expected = {entry["name"]: entry["size"] for entry in containers[platform]}
    actual = local_bundle_map(build_dir)

    missing = sorted(set(expected) - set(actual))
    extra = sorted(set(actual) - set(expected))
    size_mismatch = sorted(
        name for name in expected
        if name in actual and actual[name] != expected[name]
    )

    print(f"Manifest entries: {len(expected)}")
    print(f"Local bundles:    {len(actual)}")
    print(f"Missing:          {len(missing)}")
    print(f"Extra:            {len(extra)}")
    print(f"Size mismatch:    {len(size_mismatch)}")

    if missing:
        print("\nMissing blobs (first 20):")
        for name in missing[:20]:
            print(f"  - {name}")
        if len(missing) > 20:
            print(f"  ... and {len(missing) - 20} more")

    if extra:
        print("\nExtra local files (first 10):")
        for name in extra[:10]:
            print(f"  + {name}")

    if size_mismatch:
        print("\nSize mismatches (first 10):")
        for name in size_mismatch[:10]:
            print(f"  ! {name}: expected {expected[name]}, got {actual[name]}")

    if allow_missing:
        if size_mismatch or extra:
            return 1
        return 0

    if missing or extra or size_mismatch:
        return 1
    print("\nOK — local build matches manifest for platform", platform)
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--platform", default="android", help="Manifest container key")
    parser.add_argument("--build-dir", type=Path, required=True, help="Local build output folder")
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument(
        "--allow-missing",
        action="store_true",
        help="Pass when sizes match for present blobs; missing manifest entries are OK (#6 partial bake)",
    )
    args = parser.parse_args()
    return compare(args.platform, args.build_dir, args.manifest, args.allow_missing)


if __name__ == "__main__":
    sys.exit(main())
