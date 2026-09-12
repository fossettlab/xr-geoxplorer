"""Generate small, authored glTF/GLB fixtures without importing them into Unity.

Format constants follow Khronos glTF 2.0 and W3C PNG (linked in the manifest).
Geometry, colors and transforms are synthetic test inputs, not observations.
This is a fixture writer, not a runtime importer or scientific converter.
"""

import argparse
import copy
import hashlib
import json
import struct
import zlib
from dataclasses import dataclass
from pathlib import Path
from typing import Any

GLTF_SPEC = "https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html"
PNG_SPEC = "https://www.w3.org/TR/png/"
GLB_MAGIC, GLB_VERSION = 0x46546C67, 2
JSON_CHUNK, BIN_CHUNK = 0x4E4F534A, 0x004E4942
FLOAT_COMPONENT, ARRAY_BUFFER = 5126, 34962
# Authored fixture geometry: counterclockwise triangle, +Z normals, UV corners.
POSITIONS = (0, 0, 0, 1, 0, 0, 0, 1, 0)
NORMALS = (0, 0, 1) * 3
UVS = (0, 0, 1, 0, 0, 1)
PIXEL_RGBA = bytes((64, 128, 192, 255))


@dataclass(frozen=True)
class Fixture:
    name: str
    data: bytes
    expected: str


def json_bytes(value: Any) -> bytes:
    """Encode stable JSON, rejecting non-finite values."""
    return json.dumps(
        value, sort_keys=True, separators=(",", ":"), allow_nan=False
    ).encode()


def glb(document: dict[str, Any], binary: bytes) -> bytes:
    """Write GLB header and aligned JSON/BIN chunks per the cited specification."""
    encoded = json_bytes(document)
    encoded += b" " * (-len(encoded) % 4)
    padded = binary + b"\0" * (-len(binary) % 4)
    chunks = struct.pack("<II", len(encoded), JSON_CHUNK) + encoded
    if padded:
        chunks += struct.pack("<II", len(padded), BIN_CHUNK) + padded
    return struct.pack("<III", GLB_MAGIC, GLB_VERSION, 12 + len(chunks)) + chunks


def png_pixel() -> bytes:
    """Encode an authored RGBA pixel using PNG's required chunks and CRCs."""

    def chunk(kind: bytes, data: bytes) -> bytes:
        return (
            struct.pack(">I", len(data))
            + kind
            + data
            + struct.pack(">I", zlib.crc32(kind + data))
        )

    header = struct.pack(">IIBBBBB", 1, 1, 8, 6, 0, 0, 0)
    return (
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", header)
        + chunk(b"IDAT", zlib.compress(b"\0" + PIXEL_RGBA, level=9))
        + chunk(b"IEND", b"")
    )


def base_model() -> tuple[dict[str, Any], bytes]:
    """Build a synthetic triangle with normals, UVs and a simple PBR material."""
    arrays = [POSITIONS, NORMALS, UVS]
    binary, views = b"", []
    for values in arrays:
        data = struct.pack("<" + "f" * len(values), *values)
        views.append(
            {
                "buffer": 0,
                "byteOffset": len(binary),
                "byteLength": len(data),
                "target": ARRAY_BUFFER,
            }
        )
        binary += data
    document = {
        "asset": {"version": "2.0", "generator": "GeoXplorer authored test fixtures"},
        "scene": 0,
        "scenes": [{"nodes": [0]}],
        "nodes": [{"name": "Triangle", "mesh": 0}],
        "buffers": [{"byteLength": len(binary)}],
        "bufferViews": views,
        "accessors": [
            {
                "bufferView": 0,
                "componentType": FLOAT_COMPONENT,
                "count": 3,
                "type": "VEC3",
                "min": [0, 0, 0],
                "max": [1, 1, 0],
            },
            {
                "bufferView": 1,
                "componentType": FLOAT_COMPONENT,
                "count": 3,
                "type": "VEC3",
            },
            {
                "bufferView": 2,
                "componentType": FLOAT_COMPONENT,
                "count": 3,
                "type": "VEC2",
            },
        ],
        "meshes": [
            {
                "primitives": [
                    {
                        "attributes": {"POSITION": 0, "NORMAL": 1, "TEXCOORD_0": 2},
                        "material": 0,
                    }
                ]
            }
        ],
        "materials": [
            {
                "pbrMetallicRoughness": {"metallicFactor": 0, "roughnessFactor": 1},
                "doubleSided": True,
            }
        ],
    }
    return document, binary


def texture_model(document: dict[str, Any], image: dict[str, Any]) -> dict[str, Any]:
    """Return a copy referencing the supplied image description."""
    result = copy.deepcopy(document)
    result["images"] = [image]
    result["textures"] = [{"source": 0}]
    result["materials"][0]["pbrMetallicRoughness"]["baseColorTexture"] = {"index": 0}
    return result


def fixtures() -> list[Fixture]:
    """Construct the correctness and deliberately adverse fixture set."""
    document, binary = base_model()
    pixel = png_pixel()
    basic = glb(document, binary)
    hierarchy = copy.deepcopy(document)
    hierarchy["nodes"] = [
        {"name": "Offset parent", "children": [1], "translation": [5, 2, -3]},
        {
            "name": "Rotated stretched child",
            "mesh": 0,
            "rotation": [0, 0, 1, 0],
            "scale": [1, 2, 3],
        },
    ]
    embedded = texture_model(
        document, {"bufferView": len(document["bufferViews"]), "mimeType": "image/png"}
    )
    embedded["bufferViews"].append(
        {"buffer": 0, "byteOffset": len(binary), "byteLength": len(pixel)}
    )
    embedded["buffers"][0]["byteLength"] = len(binary) + len(pixel)
    external = texture_model(document, {"uri": "pixel.png"})
    missing = texture_model(document, {"uri": "missing.png"})
    unknown = copy.deepcopy(document)
    extension = "GEOX_fixture_unknown"
    unknown.update(
        extensionsUsed=[extension],
        extensionsRequired=[extension],
        extensions={extension: {}},
    )
    multifile = copy.deepcopy(external)
    multifile["buffers"][0]["uri"] = "geometry.bin"
    return [
        Fixture("triangle.glb", basic, "valid; check triangle, normal and material"),
        Fixture(
            "hierarchy.glb",
            glb(hierarchy, binary),
            "valid; preserve authored parent/child transforms",
        ),
        Fixture("textured.glb", glb(embedded, binary + pixel), "valid; embedded image"),
        Fixture(
            "external-image.glb",
            glb(external, binary),
            "valid format; initial self-contained-only route must reject external image",
        ),
        Fixture(
            "missing-image.glb",
            glb(missing, binary),
            "reject external image initially; later report missing companion",
        ),
        Fixture(
            "unsupported-required.glb",
            glb(unknown, binary),
            "reject unsupported required extension",
        ),
        Fixture("truncated.glb", basic[:-7], "reject length/chunk truncation"),
        Fixture("bad-magic.glb", b"FAIL" + basic[4:], "reject bad container magic"),
        Fixture(
            "multifile.gltf",
            json_bytes(multifile),
            "valid when explicit companion resolution is enabled",
        ),
        Fixture("geometry.bin", binary, "companion for multifile.gltf"),
        Fixture("pixel.png", pixel, "authored companion image"),
    ]


def main() -> None:
    """Write fixtures and hashes into a new directory; never overwrite inputs."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--output",
        type=Path,
        default=Path(__file__).resolve().parents[1]
        / "tests/fixtures/model-inputs/generated",
    )
    args = parser.parse_args()
    output = args.output.resolve()
    if output.exists():
        parser.error(f"Output already exists; choose a new directory: {output}")
    records = fixtures()
    output.mkdir(parents=True)
    inventory = []
    for fixture in records:
        (output / fixture.name).write_bytes(fixture.data)
        inventory.append(
            {
                "path": fixture.name,
                "bytes": len(fixture.data),
                "sha256": hashlib.sha256(fixture.data).hexdigest(),
                "expected": fixture.expected,
            }
        )
    manifest = {
        "source": "scripts/generate_model_input_fixtures.py; synthetic, project-authored",
        "license": "No project redistribution license declared; no third-party model data included",
        "format_sources": [GLTF_SPEC, PNG_SPEC],
        "zlib_runtime": zlib.ZLIB_RUNTIME_VERSION,
        "status": "generated fixtures; no Unity/Quest acceptance implied",
        "files": inventory,
    }
    (output / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    print(f"Wrote {len(records)} fixture files and manifest to {output}")


if __name__ == "__main__":
    main()
