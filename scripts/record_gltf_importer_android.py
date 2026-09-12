"""Verify the isolated importer APK. Device execution stays pending."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import shutil
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / "build/gltf-importer-trial"


def identity(path: Path) -> dict:
    return {
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        "bytes": path.stat().st_size,
    }


def ensure_new_artifacts(apk: Path, receipt: Path) -> None:
    """Refuse reuse of a published artifact identity, including partial attempts."""
    for path in (apk, receipt):
        if path.exists():
            raise FileExistsError(
                f"Preserve existing evidence; choose a new tag: {path.name}"
            )


def harness_identity(trial: Path) -> dict[str, str]:
    """Require the trial source to match the reviewed source, recording its identity."""
    source = ROOT / "tools/gltf-importer-trial/unity"
    hashes = {}
    for path in sorted(source.rglob("*")):
        if not path.is_file() or path.suffix.lower() == ".meta":
            continue
        relative = path.relative_to(source)
        digest = identity(path)["sha256"]
        if identity(trial / "Assets/Trial" / relative)["sha256"] != digest:
            raise ValueError(f"Trial harness differs from repository: {relative}")
        hashes[str(relative)] = digest
    return hashes


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--date", required=True)
    args = parser.parse_args()
    if Path(args.date).name != args.date or args.date in (".", ".."):
        raise ValueError("Artifact tag must be a filename component")
    apk = WORK / f"GeoX-GltfTrial-{args.date}.apk"
    destination = ROOT / "docs/contracts" / f"gltf-importer-android-{args.date}.json"
    ensure_new_artifacts(apk, destination)
    baseline = json.loads((WORK / "trial.json").read_text())
    trial = Path(baseline["project"])
    if not (trial / "trial-project.marker").is_file():
        raise ValueError("Expected an isolated trial project")
    harness = harness_identity(trial)
    build_identity = json.loads((trial / "build/build-identity.json").read_text())
    if build_identity["harnessSha256"] != harness:
        raise ValueError("Build-time harness identity differs from reviewed source")
    if (
        build_identity["apkSha256"]
        != identity(trial / "build/GeoX-GltfTrial.apk")["sha256"]
    ):
        raise ValueError("APK differs from build-time identity")
    build_result = (trial / "build/build-result.txt").read_text()
    if not build_result.startswith("Succeeded\nErrors: 0\n"):
        raise RuntimeError("Android build did not succeed without errors")
    shaders = [
        line
        for line in (trial / "build/included-shaders.txt").read_text().splitlines()
        if line
    ]
    required = {"glTF/PbrMetallicRoughness", "glTF/PbrSpecularGlossiness", "glTF/Unlit"}
    if not required.issubset(shaders):
        raise RuntimeError(
            "One or more required glTFast shaders were not always-included"
        )
    version = (
        (ROOT / "ProjectSettings/ProjectVersion.txt")
        .read_text()
        .splitlines()[0]
        .split(": ")[1]
    )
    android = Path(
        f"/Applications/Unity/Hub/Editor/{version}/PlaybackEngines/AndroidPlayer"
    )
    sdk_tools = android / "SDK/build-tools/36.0.0"
    environment = dict(os.environ, JAVA_HOME=str(android / "OpenJDK"))
    with (
        (trial / "build/GeoX-GltfTrial.apk").open("rb") as source,
        apk.open("xb") as target,
    ):
        shutil.copyfileobj(source, target)
    verification = {}
    for tool, flags in [
        ("apksigner", ["verify", "--verbose"]),
        ("zipalign", ["-c", "-P", "16", "-v", "4"]),
        ("aapt", ["dump", "badging"]),
    ]:
        result = subprocess.run(
            [str(sdk_tools / tool), *flags, str(apk)],
            capture_output=True,
            text=True,
            env=environment,
            check=True,
        )
        output = result.stdout
        if tool == "zipalign":
            output = output.splitlines()[-1]
        if tool == "aapt":
            output = "\n".join(
                line
                for line in output.splitlines()
                if line.startswith(
                    (
                        "package:",
                        "sdkVersion:",
                        "targetSdkVersion:",
                        "native-code:",
                        "launchable-activity:",
                        "uses-permission:",
                    )
                )
            )
        verification[tool] = {"exitCode": result.returncode, "output": output}
    with zipfile.ZipFile(apk) as archive:
        libs = [
            name
            for name in archive.namelist()
            if name.startswith("lib/") and name.endswith(".so")
        ]
        if not libs or any(not name.startswith("lib/arm64-v8a/") for name in libs):
            raise RuntimeError("APK is not ARM64-only")
        if "lib/arm64-v8a/libil2cpp.so" not in libs:
            raise RuntimeError("IL2CPP player missing")
        fixtures = json.loads(
            (ROOT / "tests/fixtures/model-inputs/generated/manifest.json").read_text()
        )
        packaged = {}
        for entry in fixtures["files"]:
            name = "assets/Fixtures/" + entry["path"] + ".bytes"
            data = archive.read(name)
            if hashlib.sha256(data).hexdigest() != entry["sha256"]:
                raise RuntimeError(f"Packaged fixture mismatch: {entry['path']}")
            packaged[entry["path"]] = entry["sha256"]
        shader_assets = [
            name
            for name in archive.namelist()
            if "glTF" in name or "gltfast" in name.lower()
        ]
        verification["native"] = {"libraries": libs}
        verification["shaderAssets"] = shader_assets
    application = {
        str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest()
        for area in ["Assets", "Packages", "ProjectSettings"]
        for p in (ROOT / area).rglob("*")
        if p.is_file()
    }
    changed = sorted(
        path
        for path in application.keys() | baseline["applicationSources"].keys()
        if application.get(path) != baseline["applicationSources"].get(path)
    )
    receipt = {
        "date": args.date,
        "bead": "geox-k1v.2.1",
        "status": "device_execution_pending",
        "productionAdoptionApproved": False,
        "questExecutionObserved": False,
        "harnessSha256": harness,
        "unity": {
            "version": version,
            "buildResult": build_result,
            "trial": str(trial),
            "includedShaders": shaders,
        },
        "apk": {
            "path": str(apk.relative_to(ROOT)),
            **identity(apk),
            "verification": verification,
            "fixtures": packaged,
        },
        "applicationIntegrity": {
            "preExistingFiles": len(baseline["applicationSources"]),
            "changed": changed,
        },
    }
    with destination.open("x") as stream:
        stream.write(json.dumps(receipt, indent=2) + "\n")
    print(
        json.dumps(
            {
                "receipt": str(destination),
                "apk": identity(apk),
                "shaders": shaders,
                "applicationFilesChanged": changed,
                "deviceExecution": "pending",
            },
            indent=2,
        )
    )


if __name__ == "__main__":
    main()
