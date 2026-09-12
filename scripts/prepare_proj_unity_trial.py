#!/usr/bin/env python3
"""Create a fresh isolated Unity project for the native PROJ qualification.

The normal GeoXplorer scenes, runtime packages and project settings are not copied
or modified. This is an Android ABI probe, not an XR experience acceptance build.
"""

import hashlib
import json
import shutil
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / "build/proj-qualification"
TOOLS = ROOT / "tools/proj-qualification"


def main() -> None:
    for target in ["macos", "android"]:
        if not (WORK / target / "build-receipt.json").exists():
            raise FileNotFoundError(f"Build {target} first")
    if not json.loads((WORK / "desktop-receipt.json").read_text())["success"]:
        raise ValueError("Run desktop qualification successfully first")
    trial = Path(
        tempfile.mkdtemp(prefix="geox-proj-qualification-", dir="/private/tmp")
    )
    (trial / "qualification-project.marker").write_text(
        "Isolated PROJ qualification only.\n"
    )
    shutil.copytree(TOOLS / "unity", trial / "Assets/Qualification")
    settings = trial / "ProjectSettings"
    settings.mkdir()
    shutil.copy2(ROOT / "ProjectSettings/ProjectVersion.txt", settings)
    packages = trial / "Packages"
    packages.mkdir()
    current = json.loads((ROOT / "Packages/manifest.json").read_text())["dependencies"]
    names = [
        "com.unity.pipeline",
        "com.unity.test-framework",
        "com.unity.modules.androidjni",
        "com.unity.modules.imgui",
        "com.unity.modules.jsonserialize",
        "com.unity.modules.unitywebrequest",
    ]
    (packages / "manifest.json").write_text(
        json.dumps({"dependencies": {name: current[name] for name in names}}, indent=2)
        + "\n"
    )
    resources = trial / "Assets/StreamingAssets/ProjQualification"
    resources.mkdir(parents=True)
    for source in (WORK / "android/bundle").iterdir():
        if source.suffix != ".so":
            shutil.copy2(source, resources)
    shutil.copy2(WORK / "fixtures.json", resources)
    for target, folder, suffix in [
        ("macos", "macOS", "dylib"),
        ("android", "Android/arm64-v8a", "so"),
    ]:
        destination = trial / "Assets/Plugins" / folder
        destination.mkdir(parents=True)
        shutil.copy2(
            WORK / target / "bundle" / f"libgeox_proj_probe.{suffix}", destination
        )
    receipt = {
        "project": str(trial),
        "files": {
            str(p.relative_to(trial)): hashlib.sha256(p.read_bytes()).hexdigest()
            for area in ["Assets", "Packages", "ProjectSettings"]
            for p in sorted((trial / area).rglob("*"))
            if p.is_file()
        },
    }
    (WORK / "unity-trial.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(trial)


if __name__ == "__main__":
    main()
