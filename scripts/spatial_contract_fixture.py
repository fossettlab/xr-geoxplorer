"""Print authored fixtures for the reviewed scientific scene contract.

These synthetic vectors test a proposed algebraic invariant. They are not
scientific observations, production coordinate conversion, or Unity validation.
Run with Python's standard library; output is deterministic and contains no
random values or external inputs.
"""

import json
import struct

# Authored test vectors. Each registered source reaches the same scene point.
SOURCE_A = (2, 3, 4)
SOURCE_B = (12, 23, 34)
REGISTRATION_A = (0, 0, 0)
REGISTRATION_B = (-10, -20, -30)
EXPECTED_SCENE_POINT = (2, 3, 4)
DISPLAY_TRANSLATION = (10, 20, 30)
DISPLAY_SCALE = 2


def add(a: tuple[float, ...], b: tuple[float, ...]) -> tuple[float, ...]:
    """Add corresponding components of authored three-dimensional vectors."""
    if len(a) != 3 or len(b) != 3:
        raise ValueError("Fixture vectors must contain exactly three components")
    return tuple(x + y for x, y in zip(a, b))


def boundary_examples() -> dict:
    """Check explicit synthetic basis, unit and floating-origin examples."""
    # C undoes a hypothetical loader X reflection. R adds a source-frame offset.
    # U=2 is an authored fictitious unit factor, not a real unit definition.
    vertex = (-2, 3, 4)
    source = (-vertex[0], vertex[1], vertex[2])
    scene = add(source, (1, 0, 0))
    local = add(scene, (-1, -1, -1))
    converted = tuple(2 * value for value in local)
    bridged = (converted[0], converted[2], converted[1])
    displayed = add(tuple(2 * value for value in bridged), (10, 20, 30))
    if source != SOURCE_A or displayed != (18, 32, 38):
        raise AssertionError("Basis/unit composition changed the authored result")
    unposed = tuple((a - b) / 2 for a, b in zip(displayed, (10, 20, 30)))
    unbridged = (unposed[0], unposed[2], unposed[1])
    recovered_scene = add(tuple(value / 2 for value in unbridged), (1, 1, 1))
    recovered_source = add(recovered_scene, (-1, 0, 0))
    if recovered_source != SOURCE_A:
        raise AssertionError("Inverse composition did not recover the source")

    # 2**40 and 2**-3 are exact in binary64, but binary32 cannot retain this
    # offset at that origin. This demonstrates ordering, not device accuracy.
    origin = float(2**40)
    offset = 2**-3
    coordinate = origin + offset
    local_float = struct.unpack("f", struct.pack("f", coordinate - origin))[0]
    rounded_world = struct.unpack("f", struct.pack("f", coordinate))[0]
    if local_float != offset or rounded_world - origin != 0:
        raise AssertionError("Floating-origin example differs from expected loss")
    return {
        "representation_vertex": vertex,
        "source_position": source,
        "scene_position": scene,
        "fictitious_metres_per_scene_unit": 2,
        "display_position": displayed,
        "recovered_source": recovered_source,
        "large_origin": origin,
        "small_offset": offset,
        "subtract_before_float": local_float,
        "float_before_subtract": rounded_world - origin,
    }


def main() -> None:
    """Check the authored display cases and print their reviewable values."""
    registered_a = add(SOURCE_A, REGISTRATION_A)
    registered_b = add(SOURCE_B, REGISTRATION_B)
    if registered_a != EXPECTED_SCENE_POINT or registered_b != EXPECTED_SCENE_POINT:
        raise AssertionError("Authored registration fixture does not coincide")

    x, y, z = registered_a
    # Explicit quarter-turn about Z in the fixture's right-handed XY plane.
    rotated = (-y, x, z)
    scaled = tuple(DISPLAY_SCALE * value for value in registered_a)
    combined = add(
        tuple(DISPLAY_SCALE * value for value in rotated), DISPLAY_TRANSLATION
    )
    cases = {
        "identity": registered_a,
        "translate": add(registered_a, DISPLAY_TRANSLATION),
        "rotate_about_z": rotated,
        "uniform_scale": scaled,
        "rotate_scale_translate": combined,
        "reset": registered_a,
    }
    # Independently authored expected integer answers, not numeric tolerances.
    expected = {
        "identity": (2, 3, 4),
        "translate": (12, 23, 34),
        "rotate_about_z": (-3, 2, 4),
        "uniform_scale": (4, 6, 8),
        "rotate_scale_translate": (4, 24, 38),
        "reset": (2, 3, 4),
    }
    if cases != expected:
        raise AssertionError("Display fixture differs from authored expected answers")
    print(
        json.dumps(
            {
                "status": "synthetic contract fixtures; not runtime validation",
                "source_a": SOURCE_A,
                "source_b": SOURCE_B,
                "registration_a_translation": REGISTRATION_A,
                "registration_b_translation": REGISTRATION_B,
                "scene_point": registered_a,
                "display_translation": DISPLAY_TRANSLATION,
                "display_scale": DISPLAY_SCALE,
                "expected_display_points": cases,
                "reviewed_boundary_examples": boundary_examples(),
            },
            indent=2,
        )
    )


if __name__ == "__main__":
    main()
