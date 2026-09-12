"""Shared authored reader cases; no geodetic provider or scientific tolerances."""

import copy
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FIXTURES = ROOT / "tests/fixtures/spatial"


def cases() -> list[dict]:
    """Return identical raw requests and expectations for both independent readers."""
    result = []
    inventory = json.loads((FIXTURES / "cases.json").read_text())
    for item in inventory["valid"]:
        document = json.loads((FIXTURES / item["file"]).read_text())
        request = {
            "document": document,
            "operationId": "declared-placement",
            "point": [2, 3, 4],
        }
        result.append(
            {
                "id": item["file"],
                "request": json.dumps(request),
                "document": document,
                "status": "valid",
                "available": item["available"],
                "reason": item.get("reason"),
                "point": item.get("expected"),
            }
        )
    base = json.loads((FIXTURES / "affine.json").read_text())

    def add(
        name: str, document: dict, status: str = "valid", **options: object
    ) -> None:
        request = {"document": document, **options}
        result.append(
            {
                "id": name,
                "request": json.dumps(request),
                "document": document,
                "status": status,
            }
        )

    inverse = {
        "document": base,
        "operationId": "declared-placement",
        "point": [14, 26, 46],
        "inverse": True,
    }
    result.append(
        {
            "id": "affine-inverse",
            "request": json.dumps(inverse),
            "document": base,
            "status": "valid",
            "available": True,
            "reason": None,
            "point": [2, 3, 4],
        }
    )
    optional = copy.deepcopy(base)
    optional["extensions"] = {
        "example:inert": {
            "version": 1,
            "required": False,
            "payload": {
                "nested": [
                    None,
                    True,
                    {"__proto__": "inert", "integer": 123456789012345678901234567890},
                ]
            },
        }
    }
    add("optional-extension-preserved", optional)
    add("unresolved-assets-preserved", base)

    for name, options, expected in [
        ("layer-forward", {"layerId": "model-layer", "point": [2, 3, 4]}, [14, 26, 46]),
        (
            "layer-inverse",
            {"layerId": "model-layer", "point": [14, 26, 46], "inverse": True},
            [2, 3, 4],
        ),
        (
            "representation-identity",
            {
                "layerId": "model-layer",
                "representationId": "model-representation",
                "point": [2, 3, 4],
            },
            [2, 3, 4],
        ),
    ]:
        add(name, base, **options)
        result[-1].update(available=True, point=expected, reason=None)
    for name, options in [
        ("layer-needs-point", {"layerId": "model-layer"}),
        (
            "multiple-selectors",
            {
                "layerId": "model-layer",
                "operationId": "declared-placement",
                "point": [0, 0, 0],
            },
        ),
        ("representation-needs-layer", {"representationId": "model-representation"}),
        ("point-needs-selector", {"point": [0, 0, 0]}),
        (
            "boolean-point-rejected",
            {"operationId": "declared-placement", "point": [True, 0, 0]},
        ),
        ("inverse-type-rejected", {"operationId": "declared-placement", "inverse": 1}),
    ]:
        add(name, base, "invalid", **options)

    huge = copy.deepcopy(base)
    huge["metadata"]["arbitraryPrecisionInteger"] = 10**320
    add("integer-beyond-binary64-range", huge)
    scalar_definition = copy.deepcopy(base)
    scalar_definition["frames"][0]["definition"]["value"] = 2
    add("scalar-definition-rejected", scalar_definition, "invalid")

    mutations = {
        "unknown-core-field": lambda d: d.update(renderer="not scientific state"),
        "missing-core-field": lambda d: d.pop("revision"),
        "duplicate-frame-id": lambda d: d["frames"].append(
            copy.deepcopy(d["frames"][0])
        ),
        "dangling-scene-frame": lambda d: d.update(sceneFrameId="missing"),
        "dangling-asset": lambda d: d["layers"][0]["sourceAssetIds"].append("missing"),
        "dangling-registration": lambda d: d["layers"][0].update(
            activeRegistrationId="missing"
        ),
        "history-cycle": lambda d: d["registrations"][0].update(
            supersedes="declared-solution"
        ),
        "singular-map": lambda d: d["operations"][0]["payload"].update(linear=[0] * 9),
        "invalid-endpoints": lambda d: d["operations"][0].update(
            targetFrameId="original-model"
        ),
        "same-frame-nonidentity": lambda d: d["operations"][0].update(
            sourceFrameId="local-study"
        ),
        "unsafe-locator": lambda d: d["assets"][0].update(locators=["../private.glb"]),
        "query-locator": lambda d: d["assets"][0].update(
            locators=["https://example.invalid/a.glb?token=fixture"]
        ),
        "invalid-evidence-status": lambda d: d["operations"][0]["evidence"][
            "statements"
        ][0].update(status="verified"),
        "floating-schema-version": lambda d: d.update(schemaVersion=2.0),
        "zero-operation-version": lambda d: d["operations"][0].update(version=0),
    }
    for name, mutate in mutations.items():
        document = copy.deepcopy(base)
        mutate(document)
        add(name, document, "invalid")
    encoded = json.dumps({"document": base})
    malformed = {
        "duplicate-json-key": encoded.replace(
            '"schemaVersion": 2', '"schemaVersion": 2, "schemaVersion": 2'
        ),
        "escaped-duplicate-key": encoded.replace(
            '"schemaVersion": 2', '"schemaVersion": 2, "schema\\u0056ersion": 2'
        ),
        "trailing-comma": encoded[:-1] + ",}",
        "nonfinite-number": encoded.replace("9007199254740993", "1e999"),
        "comment": "/* not JSON */" + encoded,
        "trailing-document": encoded + "{}",
    }
    for name, request in malformed.items():
        result.append({"id": name, "request": request, "status": "invalid"})
    return result
