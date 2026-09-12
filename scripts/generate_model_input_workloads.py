"""Author small pressure workloads with measured sizes. Not Quest budgets.

Each workload records its generation parameters and the resulting file,
geometry and decoded-image sizes. These are desk measurements of authored
inputs. They are not device headroom, importer allocations or a production
limit. Do not commit the generated files; regenerate them when needed.
"""

from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import struct
import sys
import zlib
from pathlib import Path
from typing import Any

GENERATOR = Path(__file__).resolve().parent / "generate_model_input_fixtures.py"
SPEC = importlib.util.spec_from_file_location("generate_model_input_fixtures", GENERATOR)
FIXTURES = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = FIXTURES
SPEC.loader.exec_module(FIXTURES)

# Authored, compact pressures. Increase only with a recorded reason.
MESH_COLUMNS = 16
MESH_ROWS = 16
TEXTURE_WIDTH = 64
TEXTURE_HEIGHT = 64
HIERARCHY_DEPTH = 32


def grid_mesh() -> tuple[dict[str, Any], bytes, dict[str, int]]:
    """Build a regular XY grid with one quad per cell, split into two triangles."""
    columns, rows = MESH_COLUMNS, MESH_ROWS
    positions: list[float] = []
    normals: list[float] = []
    uvs: list[float] = []
    for y in range(rows + 1):
        for x in range(columns + 1):
            positions.extend((x, y, 0))
            normals.extend((0, 0, 1))
            uvs.extend((x / columns, y / rows))
    indices: list[int] = []
    width = columns + 1
    for y in range(rows):
        for x in range(columns):
            a = y * width + x
            b = a + 1
            c = a + width
            d = c + 1
            indices.extend((a, b, c, b, d, c))
    vertex_bytes = struct.pack("<" + "f" * len(positions), *positions)
    normal_bytes = struct.pack("<" + "f" * len(normals), *normals)
    uv_bytes = struct.pack("<" + "f" * len(uvs), *uvs)
    index_bytes = struct.pack("<" + "H" * len(indices), *indices)
    binary = vertex_bytes + normal_bytes + uv_bytes + index_bytes
    document = {
        "asset": {"version": "2.0", "generator": "GeoXplorer authored pressure workloads"},
        "scene": 0,
        "scenes": [{"nodes": [0]}],
        "nodes": [{"name": "Grid", "mesh": 0}],
        "buffers": [{"byteLength": len(binary)}],
        "bufferViews": [
            {"buffer": 0, "byteOffset": 0, "byteLength": len(vertex_bytes), "target": 34962},
            {
                "buffer": 0,
                "byteOffset": len(vertex_bytes),
                "byteLength": len(normal_bytes),
                "target": 34962,
            },
            {
                "buffer": 0,
                "byteOffset": len(vertex_bytes) + len(normal_bytes),
                "byteLength": len(uv_bytes),
                "target": 34962,
            },
            {
                "buffer": 0,
                "byteOffset": len(vertex_bytes) + len(normal_bytes) + len(uv_bytes),
                "byteLength": len(index_bytes),
                "target": 34963,
            },
        ],
        "accessors": [
            {
                "bufferView": 0,
                "componentType": 5126,
                "count": (columns + 1) * (rows + 1),
                "type": "VEC3",
                "min": [0, 0, 0],
                "max": [columns, rows, 0],
            },
            {"bufferView": 1, "componentType": 5126, "count": (columns + 1) * (rows + 1), "type": "VEC3"},
            {"bufferView": 2, "componentType": 5126, "count": (columns + 1) * (rows + 1), "type": "VEC2"},
            {"bufferView": 3, "componentType": 5123, "count": len(indices), "type": "SCALAR"},
        ],
        "meshes": [
            {
                "primitives": [
                    {
                        "attributes": {"POSITION": 0, "NORMAL": 1, "TEXCOORD_0": 2},
                        "indices": 3,
                        "material": 0,
                    }
                ]
            }
        ],
        "materials": [
            {"pbrMetallicRoughness": {"metallicFactor": 0, "roughnessFactor": 1}}
        ],
    }
    measures = {
        "vertices": (columns + 1) * (rows + 1),
        "triangles": len(indices) // 3,
        "positionBytes": len(vertex_bytes),
        "indexBytes": len(index_bytes),
    }
    return document, binary, measures


def checker_png(width: int, height: int) -> bytes:
    """Encode an authored RGBA checker so decoded bytes are known exactly."""

    def chunk(kind: bytes, data: bytes) -> bytes:
        return (
            struct.pack(">I", len(data))
            + kind
            + data
            + struct.pack(">I", zlib.crc32(kind + data))
        )

    raw = bytearray()
    for y in range(height):
        raw.append(0)
        for x in range(width):
            on = (x // 8 + y // 8) % 2 == 0
            raw.extend((255, 0, 0, 255) if on else (0, 0, 255, 255))
    header = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    return (
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", header)
        + chunk(b"IDAT", zlib.compress(bytes(raw), level=9))
        + chunk(b"IEND", b"")
    )


def workloads() -> list[dict[str, Any]]:
    """Return named authored workloads and their measured sizes."""
    document, binary, mesh = grid_mesh()
    mesh_glb = FIXTURES.glb(document, binary)
    pixel = checker_png(TEXTURE_WIDTH, TEXTURE_HEIGHT)
    textured = FIXTURES.texture_model(
        document,
        {"bufferView": len(document["bufferViews"]), "mimeType": "image/png"},
    )
    textured["bufferViews"].append(
        {"buffer": 0, "byteOffset": len(binary), "byteLength": len(pixel)}
    )
    textured["buffers"][0]["byteLength"] = len(binary) + len(pixel)
    texture_glb = FIXTURES.glb(textured, binary + pixel)
    hierarchy = {
        **document,
        "nodes": [
            {"name": f"Node {index}", "children": [index + 1]}
            if index < HIERARCHY_DEPTH - 1
            else {"name": f"Node {index}", "mesh": 0}
            for index in range(HIERARCHY_DEPTH)
        ],
    }
    hierarchy["scenes"] = [{"nodes": [0]}]
    hierarchy_glb = FIXTURES.glb(hierarchy, binary)
    return [
        {
            "path": "mesh-pressure.glb",
            "data": mesh_glb,
            "parameters": {"columns": MESH_COLUMNS, "rows": MESH_ROWS},
            "measures": {
                **mesh,
                "fileBytes": len(mesh_glb),
                "decodedPositionBytes": mesh["positionBytes"],
            },
            "expected": "authored grid; measure file and vertex sizes, not Quest FPS",
        },
        {
            "path": "texture-pressure.glb",
            "data": texture_glb,
            "parameters": {"width": TEXTURE_WIDTH, "height": TEXTURE_HEIGHT},
            "measures": {
                "fileBytes": len(texture_glb),
                "pngBytes": len(pixel),
                "decodedRgbaBytes": TEXTURE_WIDTH * TEXTURE_HEIGHT * 4,
                "expansionRatio": (TEXTURE_WIDTH * TEXTURE_HEIGHT * 4) / len(pixel),
            },
            "expected": "authored 64x64 checker; decoded RGBA is 4 * width * height",
        },
        {
            "path": "hierarchy-pressure.glb",
            "data": hierarchy_glb,
            "parameters": {"nodes": HIERARCHY_DEPTH},
            "measures": {
                "fileBytes": len(hierarchy_glb),
                "nodes": HIERARCHY_DEPTH,
                "vertices": mesh["vertices"],
            },
            "expected": "authored node chain; count nodes, do not treat as a scene graph budget",
        },
    ]


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--output",
        type=Path,
        default=Path(__file__).resolve().parents[1]
        / "tests/fixtures/model-inputs/workloads",
    )
    args = parser.parse_args()
    output = args.output.resolve()
    if output.exists():
        parser.error(f"Output already exists; choose a new directory: {output}")
    output.mkdir(parents=True)
    records = []
    for item in workloads():
        (output / item["path"]).write_bytes(item["data"])
        records.append(
            {
                "path": item["path"],
                "bytes": len(item["data"]),
                "sha256": hashlib.sha256(item["data"]).hexdigest(),
                "parameters": item["parameters"],
                "measures": item["measures"],
                "expected": item["expected"],
            }
        )
    manifest = {
        "source": "scripts/generate_model_input_workloads.py; synthetic, project-authored",
        "license": "No project redistribution license declared; no third-party model data included",
        "status": "desk pressure measurements; not Quest budgets or importer allocations",
        "files": records,
    }
    (output / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    print(f"Wrote {len(records)} workload files to {output}")


if __name__ == "__main__":
    main()
