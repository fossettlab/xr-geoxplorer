#!/usr/bin/env python3
"""Regenerate synthetic portable-scene fixtures; no observations or accuracy claims.

The accepted review example supplies the base document. All changes below are
authored boundary cases, not selected geodetic methods or measured constants.
"""

import copy
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "tests/fixtures/spatial"


def write(name: str, data: dict) -> None:
    (DEST / name).write_text(json.dumps(data, indent=2, allow_nan=False) + "\n")


def main() -> None:
    DEST.mkdir(parents=True, exist_ok=True)
    base = json.loads((ROOT / "docs/contracts/scene-v2.review.example.json").read_text())
    base["id"] = "synthetic-portable-scene"
    base["metadata"] = {"purpose": "Synthetic conformance fixture; no observation or accuracy claim",
                        "largeInteger": 9007199254740993,
                        "beyondInt64": 123456789012345678901234567890,
                        "timestampText": "2026-09-07T00:00:00Z"}
    base["operations"][0]["payload"] = {"linear": [-1, 2, 0, 0, 2, 0, 0, 0, 4], "translation": [10, 20, 30]}
    # Hand arithmetic for p=(2,3,4): (-2+6+10, 6+20, 16+30)=(14,26,46).
    write("affine.json", base)
    cases = [{"file": "affine.json", "available": True, "point": [2, 3, 4], "expected": [14, 26, 46]}]
    unavailable = {
        "unknown-operation": "unsupported_operation_kind_or_version",
        "unknown-version": "unsupported_operation_kind_or_version",
        "required-extension": "unsupported_required_extension",
        "body-conflict": "known_body_conflict",
        "wkt2": "crs_interpretation_required",
        "projjson": "crs_interpretation_required",
        "geodetic": "qualified_geodetic_provider_required",
        "geographic": "geographic_frame_requires_qualified_provider",
    }
    for name, reason in unavailable.items():
        data = copy.deepcopy(base)
        op = data["operations"][0]
        if name == "unknown-operation":
            op["kind"] = "future-adapter-map"
            op["payload"] = {"inert": "do not execute", "integer": 9007199254740993}
        elif name == "unknown-version":
            op["version"] = 123456789012345678901234567890
            op["payload"] = {}
        elif name == "required-extension":
            data["extensions"] = {"example:future": {"version": 1, "required": True, "payload": {"future": None}}}
        elif name == "body-conflict":
            data["frames"][0]["body"] = "Mars"
            data["frames"][1]["body"] = "Moon"
        elif name == "wkt2":
            data["frames"][0]["definition"] = {"encoding": "wkt2", "value": 'ENGCRS["Synthetic unqualified fixture"]'}
        elif name == "projjson":
            data["frames"][0]["definition"] = {"encoding": "projjson", "value": {"type": "EngineeringCRS", "name": "Synthetic unqualified fixture"}}
        elif name == "geographic":
            data["frames"][0]["kind"] = "geographic"
            data["frames"][0]["cartesian"] = None
        elif name == "geodetic":
            op["kind"] = "geodetic"
            op["payload"] = {"definition": {"encoding": "opaque", "value": "unqualified fixture definition"},
                             "provider": {"name": "synthetic-unavailable-provider", "version": "fixture"},
                             "database": {}, "resources": [{"id": "unresolved-grid"}],
                             "domain": {}, "inverseAvailable": False}
        write(f"{name}.json", data)
        cases.append({"file": f"{name}.json", "available": False, "reason": reason})
    write("cases.json", {"valid": cases})

    # Schema 1 matrix, display basis, pose and origin are authored migration cases.
    identity = {"Linear": [1, 0, 0, 0, 1, 0, 0, 0, 1], "Translation": {"X": 0, "Y": 0, "Z": 0}}
    frame = {"Id": "scene-frame", "Body": "Mars", "Kind": "Cartesian",
             "AxisConvention": "X-right,Y-forward,Z-up", "MetresPerUnit": 2,
             "Definition": {"source": "Synthetic migration fixture", "originalCRSText": "uninterpreted text"}}
    display = {"Origin": {"X": 1, "Y": 1, "Z": 1},
               "AxisBridge": {"Linear": [1, 0, 0, 0, 0, 1, 0, 1, 0], "Translation": {"X": 0, "Y": 0, "Z": 0}},
               "Pose": {"Linear": identity["Linear"], "Translation": {"X": 10, "Y": 20, "Z": 30}}, "Scale": 2}
    layers = []
    for index in range(2):
        source = copy.deepcopy(frame)
        source["Id"] = f"source-frame-{index}"
        layers.append({"Id": f"layer-{index}", "Name": f"Synthetic layer {index}", "SourceFrame": source,
                       "Registration": {"SourceFrameId": source["Id"], "TargetFrameId": frame["Id"],
                                        "SourceToScene": {"Linear": [-1, 2, 0, 0, 2, 0, 0, 0, 4],
                                                          "Translation": {"X": 10, "Y": 20, "Z": 30}},
                                        "Provenance": {"method": "authored fixture"},
                                        "ValidationEvidence": {"passed": True, "accuracy": "not an accepted error estimate"}},
                       "Visible": index == 0,
                       "Assets": [{"Id": "reused-asset-id", "AccessReferenceId": f"private-fixture-key-{index}",
                                   "ContentHash": "unknown-algorithm-value", "Format": "fixture-only",
                                   "Metadata": {"geox:schema1": "existing user metadata", "largeInteger": 9007199254740993}}],
                       "Metadata": {"purpose": "synthetic"}, "Provenance": {"origin": "uninterpreted legacy"}})
    write("schema1.json", {"SchemaVersion": 1, "Id": "synthetic-schema1-scene", "Frame": frame, "Layers": layers,
                            "Display": display, "ResetDisplay": {"Origin": identity["Translation"], "AxisBridge": identity,
                                                                   "Pose": identity, "Scale": 1},
                            "Metadata": copy.deepcopy(base["metadata"])})


if __name__ == "__main__":
    main()
