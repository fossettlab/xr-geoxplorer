#!/usr/bin/env python3
"""Generate independent reference fixtures and invoke the pinned desktop probe.

No application source, package, setting or user dataset is modified. The generated
fixtures are also the input to the Android IL2CPP qualification player.
"""

import ctypes
import hashlib
import json
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / "build/proj-qualification"
# A floating-point regression margin, not a physical accuracy statement.
ROUNDING_MARGIN = 64 * sys.float_info.epsilon
LIFECYCLE_ITERATIONS = 100  # Bounded ownership smoke test; not a leak proof.


def fixtures() -> dict:
    """Return sourced Earth vectors and analytic spherical projection cases."""
    cases = [
        {
            "id": "earth-grs80-cartesian",
            "source": "EPSG:4937",
            "target": "EPSG:4936",
            "input": [45.3935192042, 17.7562015132, 133.12],
            "expected": [4272922.1553, 1368283.0597, 4518261.3501],
            "axisExpectation": ["north", "east", "up"],
            "domain": [-16.1, 33.26, 38.01, 84.73],
            "tolerance": 0.0001,
            "inverseTolerance": ROUNDING_MARGIN * 6378137,
            "outcome": "available",
            "reference": {
                "url": "https://proj.org/en/stable/operations/conversions/cart.html",
                "basis": "Published GRS80 result rounded to 4 decimal metres; tolerance is one published decimal unit. GRS80 semi-major axis 6378137 m sets inverse arithmetic margin.",
                "epochInPublishedExample": 2017.8,
                "epochUse": "No dynamic transformation: geographic and geocentric forms of the same ETRS89 frame. The epoch in the published cart example is a passthrough ordinate.",
                "axisUse": "Published longitude/latitude reordered explicitly to the declared EPSG latitude/longitude/height axes.",
            },
        },
        {
            "id": "earth-utm32",
            "source": "EPSG:4258",
            "target": "EPSG:25832",
            "input": [56, 12],
            "expected": [687071.44, 6210141.33],
            "axisExpectation": ["north", "east"],
            "domain": [6, 0, 12, 84],
            "tolerance": 0.01,
            "inverseTolerance": ROUNDING_MARGIN * 180,
            "outcome": "available",
            "reference": {
                "url": "https://proj.org/en/stable/operations/projections/utm.html",
                "basis": "Published zone 32 GRS80 example rounded to 2 decimal metres; one published decimal unit. EPSG:4258 and EPSG:25832 explicitly use GRS80.",
            },
        },
    ]
    for body, code, radius, source in [
        (
            "moon",
            301,
            1737400,
            "https://psdi.astrogeology.usgs.gov/moon/standards/data_examples/",
        ),
        ("mars", 499, 3396190, "https://voparis-vespa-crs.obspm.fr/web/mars.html"),
    ]:
        for latitude, longitude in [(0, 90), (30, 60)]:
            expected = [
                radius * math.radians(longitude),
                radius * math.radians(latitude),
            ]
            cases.append(
                {
                    "id": f"{body}-spherical-eqc-{latitude}-{longitude}",
                    "source": f"IAU_2015:{code}00",
                    "target": f"IAU_2015:{code}10",
                    "input": [latitude, longitude],
                    "expected": expected,
                    "axisExpectation": ["north", "east"],
                    "domain": [-180, -90, 180, 90],
                    "tolerance": ROUNDING_MARGIN * max(radius, *expected),
                    "inverseTolerance": ROUNDING_MARGIN * 180,
                    "outcome": "available",
                    "reference": {
                        "definition": source,
                        "radiusMetres": radius,
                        "formula": "https://proj.org/en/stable/operations/projections/eqc.html",
                        "basis": "Authored points; independent analytic x=R*longitude_radians, y=R*latitude_radians for spherical equidistant cylindrical projection with zero central meridian, origin and standard parallel. Binary64 margin 64*epsilon*coordinate scale; no height or physical accuracy claim.",
                    },
                }
            )

    def reject(identifier: str, outcome: str, **changes) -> None:
        test = dict(cases[0], id=identifier, outcome=outcome)
        test.update(changes)
        cases.append(test)

    reject(
        "wrong-axis-contract", "axis_mismatch", axisExpectation=["east", "north", "up"]
    )
    reject("missing-height", "height_required", source="EPSG:4258", input=[45, 17])
    reject("missing-ordinate", "missing_or_extra_coordinate", input=[45, 17])
    reject("null-height", "missing_or_non_numeric_coordinate", input=[45, 17, None])
    reject("outside-domain", "outside_declared_domain", input=[91, 17, 133.12])
    reject("different-body", "body_mismatch", target="IAU_2015:30110")
    reject("unknown-crs", "crs_unavailable", source="GEOX_QUALIFICATION:unavailable")
    # EPSG:7912 is the dynamic ITRF2014 geographic 3D CRS. This trial rejects
    # dynamic frames before operation discovery, even for a same-CRS request.
    reject(
        "dynamic-frame",
        "dynamic_frame_unqualified",
        source="EPSG:7912",
        target="EPSG:7912",
    )
    cases.append(dict(cases[0], id="valid-after-failures"))
    return {"cases": cases, "lifecycleIterations": LIFECYCLE_ITERATIONS}


def main() -> None:
    WORK.mkdir(parents=True, exist_ok=True)
    fixture_path = WORK / "fixtures.json"
    fixture_path.write_text(json.dumps(fixtures(), indent=2, allow_nan=False) + "\n")
    bundle = WORK / "macos/bundle"
    library = bundle / "libgeox_proj_probe.dylib"
    # Deliberately hostile ambient values: the explicit context must override both.
    os.environ["PROJ_DATA"] = str(WORK / "absent-ambient-database")
    os.environ["PROJ_NETWORK"] = "ON"
    probe = ctypes.CDLL(str(library)).geox_proj_probe_run
    probe.argtypes = [ctypes.c_char_p] * 3
    probe.restype = ctypes.c_int
    checks = []
    for name, directory, expected in [
        ("desktop", bundle, 0),
        ("missing-database", WORK / "absent-bundle", 1),
        ("desktop-after-failure", bundle, 0),
    ]:
        report = WORK / f"{name}-report.json"
        result = probe(
            os.fsencode(directory), os.fsencode(fixture_path), os.fsencode(report)
        )
        evidence = json.loads(report.read_text())
        passed = result == expected
        if name == "missing-database":
            passed &= evidence.get("error") == "database_unavailable"
        else:
            passed &= evidence.get("success") is True
        checks.append(
            {
                "id": name,
                "exitCode": result,
                "passed": passed,
                "report": str(report.relative_to(ROOT)),
            }
        )
        print(
            json.dumps(
                {
                    "id": name,
                    "passed": passed,
                    "error": evidence.get("error"),
                    "cases": [
                        {
                            "id": r["id"],
                            "outcome": r.get("outcome"),
                            "passed": r["passed"],
                        }
                        for r in evidence.get("results", [])
                    ],
                }
            )
        )
    receipt = {
        "success": all(c["passed"] for c in checks),
        "checks": checks,
        "ambientNetwork": "ON",
        "ambientData": "deliberately absent",
        "files": {
            str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest()
            for p in [fixture_path, library, bundle / "proj.db"]
        },
    }
    (WORK / "desktop-receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    raise SystemExit(0 if receipt["success"] else 1)


if __name__ == "__main__":
    main()
