#!/usr/bin/env python3
"""Build the provider-free C# spatial conformance command-line reader.

The build uses the pinned Unity installation's Roslyn compiler, .NET runtime,
and JSON.NET assembly, matching ``scripts/test_spatial_headless.py`` without
loading Unity or resolving any asset locator.

From the repository root::

    python3 scripts/build_spatial_conformance.py

The script writes ``build/spatial-conformance/csharp/command.json`` with an
``argv`` array containing the absolute ``dotnet`` executable and CLI assembly.
Send one request JSON object to that command's stdin. For example, after
building, a caller can load that array and execute it with ``subprocess.run``
while passing ``{"document": ..., "operationId": ..., "point": [...]}``.
"""

import argparse
import json
import shutil
import subprocess
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]


def unity_resources_default() -> Path:
    """Return the resources directory for the project's pinned Unity version."""

    version_file = ROOT / "ProjectSettings/ProjectVersion.txt"
    version = version_file.read_text().splitlines()[0].split(": ", 1)[1]
    return Path(f"/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/Resources")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--unity-resources", type=Path, default=unity_resources_default())
    args = parser.parse_args()

    resources = args.unity_resources.resolve()
    scripting = resources / "Scripting"
    runtime = scripting / "NetCoreRuntime"
    framework_versions = sorted(
        (runtime / "shared/Microsoft.NETCore.App").iterdir(),
        key=lambda path: tuple(int(part) for part in path.name.split(".")),
    )
    framework = framework_versions[-1]
    compiler = scripting / "DotNetSdkRoslyn/csc.dll"
    json_library = scripting / "Managed/Newtonsoft.Json.dll"

    output = ROOT / "build/spatial-conformance/csharp"
    output.mkdir(parents=True, exist_ok=True)
    program = output / "SpatialConformance.dll"
    core = sorted((ROOT / "Assets/Scripts/Spatial/Core").glob("*.cs"))
    source = ROOT / "tools/spatial-conformance/csharp/Program.cs"

    lines = [
        "-nologo",
        "-target:exe",
        "-langversion:9",
        "-nostdlib+",
        f'-out:"{program}"',
    ]
    lines += [f'-reference:"{path}"' for path in sorted(framework.glob("*.dll"))]
    lines.append(f'-reference:"{json_library}"')
    lines += [f'"{path}"' for path in core + [source]]
    response = output / "compile.rsp"
    response.write_text("\n".join(lines) + "\n")

    subprocess.run(
        [str(runtime / "dotnet"), str(compiler), f"@{response}"],
        check=True,
        cwd=ROOT,
    )
    shutil.copy2(json_library, output / json_library.name)
    (output / "SpatialConformance.runtimeconfig.json").write_text(
        json.dumps(
            {
                "runtimeOptions": {
                    "tfm": "net8.0",
                    "framework": {
                        "name": "Microsoft.NETCore.App",
                        "version": framework.name,
                    },
                }
            },
            indent=2,
        )
        + "\n"
    )

    command = [str((runtime / "dotnet").resolve()), str(program.resolve())]
    command_file = output / "command.json"
    command_file.write_text(json.dumps({"argv": command}, indent=2) + "\n")
    print("Executable command:")
    print(" ".join(command))
    print(f"Runner configuration: {command_file}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
