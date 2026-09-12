"""Validate review artifact shape; this is not the production scene validator.

Run with an existing Python environment containing jsonschema. Paths resolve
relative to this file. The assertions below are authored contract probes, not
scientific observations or an implementation of the future semantic reader.
"""

import copy
import json
from pathlib import Path

from jsonschema import Draft202012Validator


def main() -> None:
    """Check the schema, authored example and review counterexamples."""
    root = Path(__file__).resolve().parent
    schema = json.loads((root / "scene-v2.review.schema.json").read_text())
    example = json.loads((root / "scene-v2.review.example.json").read_text())
    Draft202012Validator.check_schema(schema)
    validator = Draft202012Validator(schema)
    validator.validate(example)

    malformed = []
    runtime_leak = copy.deepcopy(example)
    runtime_leak["display"] = {}
    malformed.append(runtime_leak)
    wrong_matrix = copy.deepcopy(example)
    wrong_matrix["operations"][0]["payload"]["linear"].pop()
    malformed.append(wrong_matrix)
    wrong_version = copy.deepcopy(example)
    wrong_version["schemaVersion"] = 1
    malformed.append(wrong_version)
    empty_id = copy.deepcopy(example)
    empty_id["id"] = " "
    malformed.append(empty_id)
    wrong_definition = copy.deepcopy(example)
    wrong_definition["frames"][0]["definition"] = {"encoding": "wkt2", "value": {}}
    malformed.append(wrong_definition)
    for document in malformed:
        assert list(validator.iter_errors(document)), "Malformed shape was accepted"

    future = copy.deepcopy(example)
    future["operations"][0].update(kind="future-operation", version=2, payload={})
    validator.validate(future)  # Must preserve; cannot evaluate as identity.
    unregistered = copy.deepcopy(example)
    unregistered["layers"][0]["activeRegistrationId"] = None
    unregistered["registrations"] = []
    unregistered["operations"] = []
    validator.validate(unregistered)

    # Deliberately document the schema's limits. The future semantic reader must
    # reject these even though their JSON shapes are valid.
    dangling = copy.deepcopy(example)
    dangling["sceneFrameId"] = "missing-frame"
    validator.validate(dangling)
    singular = copy.deepcopy(example)
    singular["operations"][0]["payload"]["linear"] = [0] * 9
    validator.validate(singular)
    print(
        "Review schema and authored example validated; counterexamples behaved as specified."
    )
    print(
        "Semantic readers, migration, provider evaluation and runtime remain unimplemented."
    )


if __name__ == "__main__":
    main()
