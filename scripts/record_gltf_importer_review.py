#!/usr/bin/env python3
"""Record an isolated importer run without promoting it to adoption evidence."""

import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / "build/gltf-importer-trial"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def file_hashes(directory: Path, *, exclude_meta: bool = False) -> dict[str, str]:
    return {
        str(p.relative_to(directory)): sha256(p)
        for p in sorted(directory.rglob("*"))
        if p.is_file() and (not exclude_meta or not p.name.lower().endswith(".meta"))
    }


def fixture_hashes(directory: Path) -> dict[str, str]:
    """Return the exact StreamingAssets fixture bytes addressed by the Edit Mode test."""
    if not directory.is_dir():
        raise ValueError(f"Missing trial fixture directory: {directory}")
    return file_hashes(directory, exclude_meta=True)


def require_bound_hashes(
    report: dict, field: str, expected: dict[str, str], label: str
) -> None:
    """Refuse receipts not bound to the files present before and after their test run."""
    recorded = report.get(field)
    if not isinstance(recorded, dict):
        raise ValueError(f"Importer report has no test-time {label} identity")
    if recorded != expected:
        raise ValueError(f"Importer report {label} identity differs from reviewed files")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    baseline = json.loads((WORK / "trial.json").read_text())
    trial = Path(baseline["project"])
    if not (trial / "trial-project.marker").is_file():
        raise ValueError("Expected an isolated trial project")
    sources = file_hashes(ROOT / "tools/gltf-importer-trial/unity", exclude_meta=True)
    if file_hashes(trial / "Assets/Trial", exclude_meta=True) != sources:
        raise ValueError("Trial harness differs from current repository sources")
    test_report = json.loads((trial / "build/importer-editor-report.json").read_text())
    fixtures = fixture_hashes(trial / "Assets/StreamingAssets/Fixtures")
    expected_fixtures = {
        entry["path"] + ".bytes": entry["sha256"]
        for entry in baseline["fixtureManifest"]["files"]
    }
    if fixtures != expected_fixtures:
        raise ValueError("Trial fixtures differ from the prepared fixture manifest")
    require_bound_hashes(test_report, "harnessSha256", sources, "harness")
    require_bound_hashes(test_report, "fixtureSha256", fixtures, "fixture")
    application = {
        str(p.relative_to(ROOT)): sha256(p)
        for area in ["Assets", "Packages", "ProjectSettings"]
        for p in (ROOT / area).rglob("*")
        if p.is_file()
    }
    old_application = baseline["applicationSources"]
    changes = sorted(
        p
        for p in application.keys() | old_application.keys()
        if application.get(p) != old_application.get(p)
    )
    cache = trial / "Library/PackageCache"
    gltfast = next(cache.glob("com.unity.cloud.gltfast@*"))
    archive = WORK / "gltfast/package"
    archive_hashes, resolved_hashes = file_hashes(archive), file_hashes(gltfast)
    differences = {
        p: {
            "archiveSha256": archive_hashes.get(p),
            "resolvedSha256": resolved_hashes.get(p),
        }
        for p in sorted(archive_hashes.keys() | resolved_hashes.keys())
        if archive_hashes.get(p) != resolved_hashes.get(p)
    }
    archive_manifest = json.loads((archive / "package.json").read_text())
    resolved_manifest = json.loads((gltfast / "package.json").read_text())
    manifest_changes = {
        k: {"archive": archive_manifest.get(k), "resolved": resolved_manifest.get(k)}
        for k in sorted(archive_manifest.keys() | resolved_manifest.keys())
        if archive_manifest.get(k) != resolved_manifest.get(k)
    }
    notices = {}
    for name in [
        "com.unity.cloud.gltfast",
        "com.unity.burst",
        "com.unity.collections",
        "com.unity.mathematics",
    ]:
        package = next(cache.glob(name + "@*"))
        notices[name] = {
            p.name: {"sha256": sha256(p), "text": p.read_text()}
            for p in sorted(package.iterdir())
            if p.is_file()
            and not p.name.lower().endswith(".meta")
            and ("license" in p.name.lower() or "notice" in p.name.lower())
        }
    record = {
        "scope": "Isolated Edit Mode correctness evidence; not importer adoption or Quest acceptance",
        "project": str(trial),
        "unityVersion": (trial / "ProjectSettings/ProjectVersion.txt").read_text(),
        "manifest": json.loads((trial / "Packages/manifest.json").read_text()),
        "lock": json.loads((trial / "Packages/packages-lock.json").read_text()),
        "harnessSha256": sources,
        "fixtureManifest": baseline["fixtureManifest"],
        "applicationFilesCompared": len(old_application),
        "applicationFilesChanged": changes,
        "gltfastArchiveFileCount": len(archive_hashes),
        "gltfastResolvedFileCount": len(resolved_hashes),
        "gltfastFileDifferences": differences,
        "gltfastManifestDifferences": manifest_changes,
        "licenseAndNoticeArtifacts": notices,
        "unityTestSourceBinding": "ancillary pipeline status; not test-time source-bound",
        "unityTest": json.loads((trial / "Temp/pipeline_test_status.json").read_text()),
        "importerCases": test_report,
    }
    # Never silently replace an earlier qualification result.
    with args.output.open("x") as stream:
        stream.write(json.dumps(record, indent=2) + "\n")
    print(args.output)


if __name__ == "__main__":
    main()
