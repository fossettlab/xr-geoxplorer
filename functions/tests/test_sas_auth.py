"""Unit tests for the pure gating logic (no Azure SDK / network required)."""

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from sas_auth import (  # noqa: E402
    DEFAULT_ALLOWLIST,
    api_key_valid,
    bundle_allowed,
    parse_allowlist,
)


def test_api_key_valid_match():
    assert api_key_valid("s3cret", "s3cret") is True


def test_api_key_valid_mismatch():
    assert api_key_valid("s3cret", "wrong") is False


def test_api_key_valid_missing_expected_does_not_fail_open():
    # A Function with no configured key must reject, not authorize.
    assert api_key_valid("anything", None) is False
    assert api_key_valid("anything", "") is False


def test_api_key_valid_missing_provided():
    assert api_key_valid(None, "s3cret") is False
    assert api_key_valid("", "s3cret") is False


def test_parse_allowlist_default_when_unset():
    assert parse_allowlist(None, DEFAULT_ALLOWLIST) == set(DEFAULT_ALLOWLIST)


def test_parse_allowlist_custom_json():
    assert parse_allowlist('["a/b", "c/d"]', DEFAULT_ALLOWLIST) == {"a/b", "c/d"}


def test_parse_allowlist_invalid_json_falls_back():
    assert parse_allowlist("{not json", DEFAULT_ALLOWLIST) == set(DEFAULT_ALLOWLIST)


def test_parse_allowlist_non_array_falls_back():
    assert parse_allowlist('{"a": 1}', DEFAULT_ALLOWLIST) == set(DEFAULT_ALLOWLIST)


def test_bundle_allowed():
    allowlist = {"android/eastshorestructure-bundle"}
    assert bundle_allowed("android/eastshorestructure-bundle", allowlist) is True
    assert bundle_allowed("android/../secrets", allowlist) is False
    assert bundle_allowed("android/yayamari-bundle", allowlist) is False
