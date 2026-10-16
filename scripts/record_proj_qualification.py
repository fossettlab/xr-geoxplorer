#!/usr/bin/env python3
"""Verify the isolated APK and record local qualification evidence, without acceptance.

Run after the native builds, desktop probe, Unity Edit Mode test and Android build.
Device execution stays explicitly pending until separately observed on a Quest.
"""

import argparse
import hashlib
import json
import os
import shutil
import subprocess
import tempfile
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / "build/proj-qualification"


def identity(path: Path) -> dict:
    return {
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        "bytes": path.stat().st_size,
    }


def read(path: Path) -> dict:
    return json.loads(path.read_text())


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--date", required=True, help="Checkpoint date, YYYY-MM-DD")
    parser.add_argument(
        "--snapshot",
        action="store_true",
        help="Create a new starting application hash map before working",
    )
    args = parser.parse_args()
    # Also prevents a date argument from becoming an output path.
    from datetime import date

    date.fromisoformat(args.date)
    if args.snapshot:
        WORK.mkdir(parents=True, exist_ok=True)
        snapshot = {
            str(p.relative_to(ROOT)): identity(p)["sha256"]
            for area in ["Assets", "Packages", "ProjectSettings"]
            for p in sorted((ROOT / area).rglob("*"))
            if p.is_file()
        }
        path = WORK / f"source-start-{args.date}.json"
        with path.open("x") as output:
            output.write(json.dumps(snapshot, indent=2) + "\n")
        print(path)
        return
    trial_record = read(WORK / "unity-trial.json")
    trial = Path(trial_record["project"])
    build_result = (trial / "build/build-result.txt").read_text()
    if not build_result.startswith("Succeeded\nErrors: 0\n"):
        raise RuntimeError("Android build did not succeed without errors")
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
    llvm = android / "NDK/toolchains/llvm/prebuilt/darwin-x86_64/bin"
    environment = dict(os.environ, JAVA_HOME=str(android / "OpenJDK"))
    apk = WORK / f"GeoX-ProjQualification-{args.date}.apk"
    shutil.copy2(trial / "build/GeoX-ProjQualification.apk", apk)
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
    with zipfile.ZipFile(apk) as archive, tempfile.TemporaryDirectory() as temp:
        libs = [
            name
            for name in archive.namelist()
            if name.startswith("lib/") and name.endswith(".so")
        ]
        if not libs or any(not name.startswith("lib/arm64-v8a/") for name in libs):
            raise RuntimeError("APK is not ARM64-only")
        if "lib/arm64-v8a/libil2cpp.so" not in libs:
            raise RuntimeError("IL2CPP player missing")
        resources = {}
        for source in (WORK / "android/bundle").iterdir():
            if source.suffix == ".so":
                continue
            packaged = archive.read("assets/ProjQualification/" + source.name)
            if packaged != source.read_bytes():
                raise RuntimeError(f"Packaged resource mismatch: {source.name}")
            resources[source.name] = identity(source)
        if (
            archive.read("assets/ProjQualification/fixtures.json")
            != (WORK / "fixtures.json").read_bytes()
        ):
            raise RuntimeError("Packaged fixture mismatch")
        native = Path(temp) / "libgeox_proj_probe.so"
        native.write_bytes(archive.read("lib/arm64-v8a/libgeox_proj_probe.so"))
        elf = subprocess.check_output(
            [str(llvm / "llvm-readelf"), "-h", "-l", "-d", str(native)], text=True
        )
        symbols = subprocess.check_output(
            [str(llvm / "llvm-readelf"), "--dyn-syms", str(native)], text=True
        )
        defined = [
            line.split()[-1]
            for line in symbols.splitlines()
            if "DEFAULT" in line and " UND " not in line
        ]
        if defined != ["geox_proj_probe_run"]:
            raise RuntimeError("Unexpected public native ABI")
        if "AArch64" not in elf or any(
            "0x4000" not in line
            for line in elf.splitlines()
            if line.strip().startswith("LOAD")
        ):
            raise RuntimeError("Unexpected native architecture/page alignment")
        verification["native"] = {
            **identity(native),
            "definedExports": defined,
            "elf": elf,
            "libraries": libs,
        }
    before = read(WORK / f"source-start-{args.date}.json")
    changed = [
        name
        for name, digest in before.items()
        if not (ROOT / name).is_file() or identity(ROOT / name)["sha256"] != digest
    ]
    if changed:
        raise RuntimeError(f"Pre-existing application files changed: {changed}")
    for name, digest in trial_record["files"].items():
        if name.startswith("Assets/") and identity(trial / name)["sha256"] != digest:
            raise RuntimeError(f"Trial source changed since preparation: {name}")
    tests = read(WORK / "unity-editor-tests.json")["data"]["result"]
    tests = json.loads(tests) if isinstance(tests, str) else tests
    console = read(WORK / "unity-editor-console.json")["data"]["result"]
    if (
        tests["status"] != "completed"
        or tests["summary"]["failed"]
        or tests["summary"]["passed"] < 1
    ):
        raise RuntimeError("Unity test did not pass")
    if console["entries"] or console["dropped"]:
        raise RuntimeError("Unity error capture is not clean")
    report = read(WORK / "desktop-report.json")
    if not report["success"] or not read(WORK / "desktop-receipt.json")["success"]:
        raise RuntimeError("Desktop qualification did not pass")
    receipt = {
        "date": args.date,
        "bead": "geox-k1v.7.1",
        "status": "device_execution_pending",
        "productionAdoptionApproved": False,
        "questExecutionObserved": False,
        "nativeBuilds": {
            target: read(WORK / target / "build-receipt.json")
            for target in ["macos", "android"]
        },
        "desktop": read(WORK / "desktop-receipt.json"),
        "database": report["database"],
        "unity": {
            "version": version,
            "tests": tests,
            "consoleBeforeBuild": console,
            "buildResult": build_result,
            "trial": str(trial),
        },
        "apk": {
            "path": str(apk.relative_to(ROOT)),
            **identity(apk),
            "verification": verification,
            "resources": resources,
            "fixtures": identity(WORK / "fixtures.json"),
        },
        "applicationIntegrity": {"preExistingFiles": len(before), "changed": changed},
        "sources": {
            str(p.relative_to(ROOT)): identity(p)
            for p in sorted((ROOT / "tools/proj-qualification").rglob("*"))
            if p.is_file()
        },
        "scripts": {
            str(p.relative_to(ROOT)): identity(p)
            for p in sorted((ROOT / "scripts").glob("*proj*.py"))
        },
    }
    destination = ROOT / "docs/contracts" / f"proj-qualification-{args.date}.json"
    destination.write_text(json.dumps(receipt, indent=2) + "\n")
    reference = {"fixtures": read(WORK / "fixtures.json"), "desktopResults": report}
    destination.with_name(f"proj-reference-results-{args.date}.json").write_text(
        json.dumps(reference, indent=2) + "\n"
    )
    print(
        json.dumps(
            {
                "receipt": str(destination),
                "apk": identity(apk),
                "applicationFilesUnchanged": len(before),
                "deviceExecution": "pending",
            },
            indent=2,
        )
    )


if __name__ == "__main__":
    main()
