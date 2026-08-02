"""Unit tests for anchor record validation (#40 Phase B scaffold)."""

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from anchor_persistence import (  # noqa: E402
    normalize_anchor_id,
    new_anchor_id,
    parse_create_body,
    record_to_json,
)


def test_new_anchor_id_is_32_hex_chars():
    anchor_id = new_anchor_id()
    assert len(anchor_id) == 32
    assert normalize_anchor_id(anchor_id) == anchor_id


def test_normalize_anchor_id_rejects_invalid():
    assert normalize_anchor_id("not-valid") is None
    assert normalize_anchor_id("") is None


def test_parse_create_body_valid():
    record, error = parse_create_body(
        {
            "name": "Room A",
            "identifier": "asa-cloud-id-123",
            "dateExpired": "2026-12-31T00:00:00Z",
        }
    )
    assert error is None
    assert record.name == "Room A"
    assert record.identifier == "asa-cloud-id-123"
    assert record.date_expired == "2026-12-31T00:00:00Z"
    assert normalize_anchor_id(record.id) == record.id


def test_parse_create_body_missing_name():
    record, error = parse_create_body({"identifier": "x", "dateExpired": "2026-01-01"})
    assert record is None
    assert "name" in error


def test_record_to_json_roundtrip_fields():
    record, _ = parse_create_body(
        {"name": "n", "identifier": "i", "dateExpired": "2026-01-01T00:00:00Z"}
    )
    payload = record_to_json(record)
    assert payload["name"] == "n"
    assert payload["identifier"] == "i"
    assert "id" in payload
