#!/usr/bin/env python3
"""Create an isolated Unity project for the reviewed glTFast trial."""

import hashlib
import json
import shutil
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / "build/gltf-importer-trial"


def main() -> None:
    trial = Path(tempfile.mkdtemp(prefix="geox-gltf-trial-", dir="/private/tmp"))
    shutil.copytree(ROOT / "tools/gltf-importer-trial/unity", trial / "Assets/Trial")
    (trial / "trial-project.marker").write_text("Isolated importer qualification\n")
    (trial / "Packages").mkdir()
    manifest = json.loads((ROOT / "Packages/manifest.json").read_text())["dependencies"]
    names = [
        "com.unity.pipeline",
        "com.unity.test-framework",
        "com.unity.modules.jsonserialize",
        "com.unity.modules.unitywebrequest",
        "com.unity.modules.unitywebrequesttexture",
        "com.unity.modules.imageconversion",
        "com.unity.modules.imgui",
        "com.unity.modules.animation",
        "com.unity.modules.physics",
    ]
    dependencies = {name: manifest[name] for name in names}
    dependencies["com.unity.cloud.gltfast"] = "6.20.0"
    (trial / "Packages/manifest.json").write_text(
        json.dumps({"dependencies": dependencies}, indent=2) + "\n"
    )
    (trial / "ProjectSettings").mkdir()
    shutil.copy2(ROOT / "ProjectSettings/ProjectVersion.txt", trial / "ProjectSettings")
    fixtures = ROOT / "tests/fixtures/model-inputs/generated"
    inventory = json.loads((fixtures / "manifest.json").read_text())
    destination = trial / "Assets/StreamingAssets/Fixtures"
    destination.mkdir(parents=True)
    for entry in inventory["files"]:
        data = (fixtures / entry["path"]).read_bytes()
        assert hashlib.sha256(data).hexdigest() == entry["sha256"]
        # .bytes prevents Editor asset import of deliberately malformed GLBs.
        (destination / (entry["path"] + ".bytes")).write_bytes(data)
    workloads = ROOT / "tests/fixtures/model-inputs/workloads"
    if (workloads / "manifest.json").is_file():
        work_inventory = json.loads((workloads / "manifest.json").read_text())
        work_destination = trial / "Assets/StreamingAssets/Workloads"
        work_destination.mkdir(parents=True)
        for entry in work_inventory["files"]:
            data = (workloads / entry["path"]).read_bytes()
            assert hashlib.sha256(data).hexdigest() == entry["sha256"]
            (work_destination / (entry["path"] + ".bytes")).write_bytes(data)
    WORK.mkdir(parents=True, exist_ok=True)
    record = {
        "project": str(trial),
        "fixtureManifest": inventory,
        "applicationSources": {
            str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest()
            for area in ["Assets", "Packages", "ProjectSettings"]
            for p in (ROOT / area).rglob("*")
            if p.is_file()
        },
    }
    (WORK / "trial.json").write_text(json.dumps(record, indent=2) + "\n")
    print(trial)


if __name__ == "__main__":
    main()
