#!/usr/bin/env python3
"""Compare independent provider-free C# and JavaScript readers on authored cases.

Run scripts/build_spatial_conformance.py first. Results are semantic conformance,
not provider qualification, scientific accuracy or renderer acceptance.
"""

import argparse
import hashlib
import json
import subprocess
from pathlib import Path

from spatial_conformance_cases import ROOT, cases


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    commands = {
        "csharp": json.loads(
            (ROOT / "build/spatial-conformance/csharp/command.json").read_text()
        )["argv"],
        "javascript": ["node", str(ROOT / "tools/spatial-conformance/js/cli.mjs")],
    }
    results = []
    for case in cases():
        for reader, command in commands.items():
            proc = subprocess.run(
                command,
                input=case["request"],
                text=True,
                capture_output=True,
                cwd=ROOT,
                check=False,
            )
            problems = []
            try:
                actual = json.loads(proc.stdout)
                if actual.get("status") != case["status"]:
                    problems.append("status differs from authored expectation")
                if case["status"] == "valid":
                    if actual.get("document") != case["document"]:
                        problems.append("semantic roundtrip changed source document")
                    for field in ["available", "reason", "point"]:
                        if field in case and actual.get(field) != case[field]:
                            problems.append(
                                f"{field} differs from authored expectation"
                            )
                if proc.returncode != 0:
                    problems.append("reader exited unsuccessfully")
            except (ValueError, TypeError) as error:
                actual = {"stdout": proc.stdout, "stderr": proc.stderr}
                problems.append(str(error))
            results.append(
                {
                    "case": case["id"],
                    "reader": reader,
                    "passed": not problems,
                    "problems": problems,
                    "actual": actual,
                    "requestSha256": hashlib.sha256(
                        case["request"].encode()
                    ).hexdigest(),
                }
            )
    files = [
        p
        for area in [
            "tools/spatial-conformance",
            "Assets/Scripts/Spatial/Core",
            "tests/fixtures/spatial",
        ]
        for p in (ROOT / area).rglob("*")
        if p.is_file()
    ]
    files += [
        Path(__file__).resolve(),
        ROOT / "scripts/spatial_conformance_cases.py",
        ROOT / "scripts/build_spatial_conformance.py",
    ]
    receipt = {
        "scope": "Provider-free semantic conformance only",
        "commands": commands,
        "sources": {
            str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest()
            for p in sorted(files)
        },
        "passed": sum(r["passed"] for r in results),
        "total": len(results),
        "results": results,
    }
    with args.output.open("x") as stream:
        stream.write(json.dumps(receipt, indent=2, allow_nan=False) + "\n")
    for result in results:
        if not result["passed"]:
            print(result["reader"], result["case"], result["problems"])
    print(
        f"Provider-free conformance: {receipt['passed']}/{receipt['total']} passed; {args.output}"
    )
    return 0 if all(r["passed"] for r in results) else 1


if __name__ == "__main__":
    raise SystemExit(main())
