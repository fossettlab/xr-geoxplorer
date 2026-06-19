"""Pure auth / validation helpers for the SAS-issuance Function.

Kept free of Azure SDK imports so the request-gating logic (API-key check,
bundle allowlist) is unit-testable without credentials or network. The Azure
user-delegation SAS call itself lives in ``function_app.py``.
"""

from __future__ import annotations

import hmac
import json
import logging

# The deployed `restricted` container holds exactly these six blobs (two scenes ×
# three platform folders). The allowlist is enforced server-side so a caller can
# never coax a SAS for an arbitrary blob path. Override via the
# SAS_BUNDLE_ALLOWLIST app setting (a JSON array).
DEFAULT_ALLOWLIST = [
    "android/eastshorestructure-bundle",
    "android/yayamari-bundle",
    "ios/eastshorestructure-bundle",
    "ios/yayamari-bundle",
    "x86/eastshorestructure-bundle",
    "x86/yayamari-bundle",
]


def api_key_valid(provided: str | None, expected: str | None) -> bool:
    """Constant-time compare of the caller's key against the configured key.

    Returns False if either side is missing, so a mis-configured Function (no
    ``SAS_API_KEY`` set) never authorizes a request rather than failing open.
    """
    if not expected or not provided:
        return False
    return hmac.compare_digest(provided, expected)


def parse_allowlist(raw: str | None, default: list[str]) -> set[str]:
    """Parse the SAS_BUNDLE_ALLOWLIST app setting (a JSON array of blob names).

    Falls back to ``default`` when the setting is missing, not valid JSON, or
    not a JSON array.
    """
    if not raw:
        return set(default)
    try:
        parsed = json.loads(raw)
    except json.JSONDecodeError:
        logging.error("SAS_BUNDLE_ALLOWLIST is not valid JSON; using default allowlist")
        return set(default)
    if not isinstance(parsed, list):
        logging.error("SAS_BUNDLE_ALLOWLIST must be a JSON array; using default allowlist")
        return set(default)
    return set(parsed)


def bundle_allowed(bundle: str, allowlist: set[str]) -> bool:
    return bundle in allowlist
