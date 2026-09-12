#!/usr/bin/env python3
"""Compile the actual GeoX.Spatial sources and NUnit tests without a Unity Editor.

Uses the Roslyn compiler, .NET runtime, JSON.NET and NUnit shipped with the
project's pinned Unity installation. No restore, license, Editor or network is used.
"""

import argparse
import json
import shutil
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    version = (ROOT / "ProjectSettings/ProjectVersion.txt").read_text().splitlines()[0].split(": ")[1]
    parser.add_argument("--unity-resources", type=Path, default=Path(
        f"/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/Resources"))
    args = parser.parse_args()
    resources = args.unity_resources.resolve()
    scripting = resources / "Scripting"
    runtime = scripting / "NetCoreRuntime"
    versions = sorted((runtime / "shared/Microsoft.NETCore.App").iterdir(),
                      key=lambda p: tuple(int(x) for x in p.name.split(".")))
    framework = versions[-1]
    compiler = scripting / "DotNetSdkRoslyn/csc.dll"
    libraries = [scripting / "Managed/Newtonsoft.Json.dll",
                 resources / "PackageManager/BuiltInPackages/com.unity.ext.nunit/net40/unity-custom/nunit.framework.dll"]
    output = ROOT / "build/spatial-headless"
    output.mkdir(parents=True, exist_ok=True)
    program = output / "SpatialTests.dll"
    core = sorted((ROOT / "Assets/Scripts/Spatial/Core").glob("*.cs"))
    tests = sorted((ROOT / "Assets/Tests/EditMode/Spatial").glob("*.cs"))
    # Only BCL, JSON.NET and NUnit references: a Unity reference will fail compilation.
    lines = ["-nologo", "-target:exe", "-langversion:9", "-nostdlib+", f'-out:"{program}"']
    lines += [f'-reference:"{p}"' for p in sorted(framework.glob("*.dll")) + libraries]
    lines += [f'"{p}"' for p in core + tests + [ROOT / "tools/spatial-headless/Program.cs"]]
    response = output / "compile.rsp"
    response.write_text("\n".join(lines) + "\n")
    subprocess.run([str(runtime / "dotnet"), str(compiler), f"@{response}"], check=True, cwd=ROOT)
    for library in libraries:
        shutil.copy2(library, output / library.name)
    (output / "SpatialTests.runtimeconfig.json").write_text(json.dumps({"runtimeOptions": {
        "tfm": "net8.0", "framework": {"name": "Microsoft.NETCore.App", "version": framework.name}}}))
    receipt = output / "results.xml"
    result = subprocess.run([str(runtime / "dotnet"), str(program), str(receipt)], cwd=ROOT)
    print(f"NUnit evidence: {receipt}")
    return result.returncode


if __name__ == "__main__":
    raise SystemExit(main())
