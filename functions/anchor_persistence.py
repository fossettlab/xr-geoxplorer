"""Anchor record validation and serialization for #40 Phase B.

Pure helpers (no Azure SDK) so POST/GET gating logic is unit-testable without
credentials. Table Storage I/O lives in function_app.py behind ANCHOR_TABLE_NAME.
"""

from __future__ import annotations

import re
import uuid
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
from typing import Any

ANCHOR_ID_PATTERN = re.compile(r"^[a-f0-9]{32}$")


@dataclass(frozen=True)
class AnchorRecord:
    id: str
    name: str
    identifier: str
    date_created: str
    date_expired: str

    def to_table_entity(self) -> dict[str, Any]:
        return {
            "PartitionKey": "anchor",
            "RowKey": self.id,
            "name": self.name,
            "identifier": self.identifier,
            "date_created": self.date_created,
            "date_expired": self.date_expired,
        }

    @staticmethod
    def from_table_entity(entity: dict[str, Any]) -> AnchorRecord:
        return AnchorRecord(
            id=entity["RowKey"],
            name=entity["name"],
            identifier=entity["identifier"],
            date_created=entity["date_created"],
            date_expired=entity["date_expired"],
        )


def new_anchor_id() -> str:
    return uuid.uuid4().hex


def normalize_anchor_id(raw: str | None) -> str | None:
    if not raw:
        return None
    cleaned = raw.strip().lower()
    return cleaned if ANCHOR_ID_PATTERN.match(cleaned) else None


def parse_create_body(body: dict[str, Any] | None) -> tuple[AnchorRecord | None, str | None]:
    """Return (record, error_message)."""
    if not body or not isinstance(body, dict):
        return None, "Body must be a JSON object"

    name = body.get("name")
    identifier = body.get("identifier")
    date_expired = body.get("dateExpired") or body.get("date_expired")

    if not isinstance(name, str) or not name.strip():
        return None, "Missing or invalid 'name'"
    if not isinstance(identifier, str) or not identifier.strip():
        return None, "Missing or invalid 'identifier'"
    if not isinstance(date_expired, str) or not date_expired.strip():
        return None, "Missing or invalid 'dateExpired'"

    now = datetime.now(timezone.utc).isoformat()
    record = AnchorRecord(
        id=new_anchor_id(),
        name=name.strip(),
        identifier=identifier.strip(),
        date_created=now,
        date_expired=date_expired.strip(),
    )
    return record, None


def record_to_json(record: AnchorRecord) -> dict[str, Any]:
    return asdict(record)


def record_to_firebase_json(record: AnchorRecord) -> dict[str, Any]:
    """Firebase Realtime Database shape for drop-in GET list parity (#40)."""
    return {
        "name": record.name,
        "identifier": record.identifier,
        "dateCreated": record.date_created,
        "dateExpired": record.date_expired,
    }


def records_to_firebase_list_json(records: list[AnchorRecord]) -> list[dict[str, Any]]:
    return [record_to_firebase_json(record) for record in records]
