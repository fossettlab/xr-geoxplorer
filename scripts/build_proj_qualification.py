#!/usr/bin/env python3
"""Build a pinned, offline PROJ probe for macOS or Android ARM64.

Only ignored build/proj-qualification is modified. No packages are installed
into the Unity project. Source archives are checked against sources.json.
"""

import argparse
import hashlib
import json
import os
import shutil
import subprocess
import tarfile
import urllib.request
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / "build/proj-qualification"
TOOLS = ROOT / "tools/proj-qualification"


def sources(download: bool) -> tuple[Path, Path]:
    pins = json.loads((TOOLS / "sources.json").read_text())
    archives = WORK / "downloads"
    archives.mkdir(parents=True, exist_ok=True)
    extracted = WORK / "src"
    extracted.mkdir(exist_ok=True)
    for pin in pins.values():
        archive = archives / pin["url"].rsplit("/", 1)[1]
        if not archive.exists():
            if not download:
                raise FileNotFoundError(
                    f"{archive}; use --download to fetch the pinned source"
                )
            with urllib.request.urlopen(pin["url"], timeout=30) as response:
                archive.write_bytes(response.read())
        content = archive.read_bytes()
        if hashlib.sha256(content).hexdigest() != pin["sha256"]:
            raise ValueError(f"Source hash mismatch: {archive}")
        if (
            "published_sha3_256" in pin
            and hashlib.sha3_256(content).hexdigest() != pin["published_sha3_256"]
        ):
            raise ValueError(f"Published source hash mismatch: {archive}")
        marker = extracted / (archive.name + ".sha256")
        if marker.exists() and marker.read_text() == pin["sha256"]:
            continue
        if archive.suffix == ".zip":
            with zipfile.ZipFile(archive) as bundle:
                if any(
                    n.startswith("/") or ".." in Path(n).parts
                    for n in bundle.namelist()
                ):
                    raise ValueError("Unsafe archive path")
                bundle.extractall(extracted)
        else:
            with tarfile.open(archive) as bundle:
                bundle.extractall(extracted, filter="data")
        marker.write_text(pin["sha256"])
    return (
        extracted / f"proj-{pins['proj']['version']}",
        extracted / "sqlite-amalgamation-3530400",
    )


def run(command: list[str], log: Path, environment: dict[str, str]) -> None:
    with log.open("a") as stream:
        stream.write(json.dumps(command) + "\n")
        stream.flush()
        result = subprocess.run(
            command, stdout=stream, stderr=subprocess.STDOUT, env=environment
        )
    if result.returncode:
        raise RuntimeError(f"Command failed ({result.returncode}); inspect {log}")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--target", choices=["macos", "android"], required=True)
    parser.add_argument("--download", action="store_true")
    parser.add_argument("--jobs", type=int, default=min(4, os.cpu_count() or 1))
    args = parser.parse_args()
    proj, sqlite = sources(args.download)
    build = WORK / args.target
    build.mkdir(exist_ok=True)
    log = build / "build.log"
    environment = {k: v for k, v in os.environ.items() if not k.startswith("PROJ_")}
    environment["PROJ_NETWORK"] = "OFF"
    compiler, archiver = shutil.which("clang"), shutil.which("ar")
    cmake = shutil.which("cmake")
    extra = []
    version = (
        (ROOT / "ProjectSettings/ProjectVersion.txt")
        .read_text()
        .splitlines()[0]
        .split(": ")[1]
    )
    ndk = Path(
        f"/Applications/Unity/Hub/Editor/{version}/PlaybackEngines/AndroidPlayer/NDK"
    )
    if args.target == "android":
        minimum = next(
            line.split(":")[1].strip()
            for line in (ROOT / "ProjectSettings/ProjectSettings.asset")
            .read_text()
            .splitlines()
            if line.strip().startswith("AndroidMinSdkVersion:")
        )
        host = ndk / "toolchains/llvm/prebuilt/darwin-x86_64/bin"
        compiler = str(host / f"aarch64-linux-android{minimum}-clang")
        archiver = str(host / "llvm-ar")
        extra = [
            f"-DCMAKE_TOOLCHAIN_FILE={ndk}/build/cmake/android.toolchain.cmake",
            "-DANDROID_ABI=arm64-v8a",
            f"-DANDROID_PLATFORM=android-{minimum}",
            "-DANDROID_STL=c++_static",
            "-DANDROID_SUPPORT_FLEXIBLE_PAGE_SIZES=ON",
        ]
    run(
        [
            compiler,
            "-O2",
            "-fPIC",
            "-fvisibility=hidden",
            "-DSQLITE_OMIT_LOAD_EXTENSION",
            "-c",
            str(sqlite / "sqlite3.c"),
            "-o",
            str(build / "sqlite3.o"),
        ],
        log,
        environment,
    )
    run(
        [archiver, "rcs", str(build / "libsqlite3.a"), str(build / "sqlite3.o")],
        log,
        environment,
    )
    command = [
        cmake,
        "-S",
        str(TOOLS),
        "-B",
        str(build / "cmake"),
        "-DCMAKE_BUILD_TYPE=Release",
        f"-DPROJ_SOURCE_DIR={proj}",
        f"-DSQLite3_INCLUDE_DIR={sqlite}",
        f"-DSQLite3_LIBRARY={build}/libsqlite3.a",
        f"-DEXE_SQLITE3={shutil.which('sqlite3')}",
    ] + extra
    run(command, log, environment)
    run(
        [
            cmake,
            "--build",
            str(build / "cmake"),
            "--target",
            "geox_proj_probe",
            "generate_proj_db",
            "--parallel",
            str(args.jobs),
        ],
        log,
        environment,
    )
    bundle = build / "bundle"
    bundle.mkdir(exist_ok=True)
    suffix = "so" if args.target == "android" else "dylib"
    shutil.copy2(build / f"cmake/libgeox_proj_probe.{suffix}", bundle)
    shutil.copy2(build / "cmake/proj/data/proj.db", bundle)
    shutil.copy2(proj / "COPYING", bundle / "PROJ-COPYING.txt")
    headers = {
        "nlohmann-json-LICENSE.txt": proj
        / "include/proj/internal/vendor/nlohmann/json.hpp",
        "nlohmann-wrapper-LICENSE.txt": proj
        / "include/proj/internal/include_nlohmann_json.hpp",
    }
    for name, header in headers.items():
        (bundle / name).write_text(header.read_text().split("*/", 1)[0] + "*/\n")
    sqlite_notice = (
        sqlite.joinpath("sqlite3.c")
        .read_text()
        .split("2001 September 15", 1)[1]
        .split("*/", 1)[0]
    )
    (bundle / "SQLite-NOTICE.txt").write_text(
        "/* 2001 September 15" + sqlite_notice + "*/\n"
    )
    receipt = {
        "target": args.target,
        "sources": json.loads((TOOLS / "sources.json").read_text()),
        "configure": command,
        "compiler": subprocess.check_output([compiler, "--version"], text=True),
        "ndk": (ndk / "source.properties").read_text()
        if args.target == "android"
        else None,
        "files": {
            p.name: {
                "sha256": hashlib.sha256(p.read_bytes()).hexdigest(),
                "bytes": p.stat().st_size,
            }
            for p in sorted(bundle.iterdir())
            if p.is_file()
        },
    }
    (build / "build-receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(f"Built {args.target} qualification bundle: {bundle}")


if __name__ == "__main__":
    main()
