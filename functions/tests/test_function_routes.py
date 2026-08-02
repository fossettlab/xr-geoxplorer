"""Route-level tests for function_app handlers (#24 / #40).

Uses mocks for Azure SDK and table storage — no credentials or network needed.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path
from unittest.mock import MagicMock, patch

import azure.functions as func

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import function_app  # noqa: E402
from anchor_persistence import AnchorRecord  # noqa: E402


def _request(
    method: str,
    *,
    url: str = "/api/test",
    headers: dict | None = None,
    body: bytes | str | None = None,
    route_params: dict | None = None,
) -> func.HttpRequest:
    if isinstance(body, str):
        body = body.encode("utf-8")
    return func.HttpRequest(
        method=method,
        url=url,
        headers=headers or {},
        params={},
        route_params=route_params or {},
        body=body,
    )


@patch.dict("function_app.os.environ", {"SAS_API_KEY": "test-key"}, clear=False)
def test_sas_restricted_rejects_missing_api_key():
    req = _request("POST", body='{"bundle":"android/eastshorestructure-bundle"}')
    resp = function_app.sas_restricted(req)
    assert resp.status_code == 401


@patch.dict("function_app.os.environ", {"SAS_API_KEY": "test-key"}, clear=False)
def test_sas_restricted_rejects_disallowed_bundle():
    req = _request(
        "POST",
        headers={"X-API-Key": "test-key"},
        body='{"bundle":"android/not-on-allowlist"}',
    )
    resp = function_app.sas_restricted(req)
    assert resp.status_code == 403


@patch.dict("function_app.os.environ", {"SAS_API_KEY": "test-key"}, clear=False)
@patch("function_app._make_sas_url", return_value="https://example.com/blob?sas=1")
def test_sas_restricted_returns_url_for_allowed_bundle(mock_sas):
    req = _request(
        "POST",
        headers={"X-API-Key": "test-key"},
        body='{"bundle":"android/eastshorestructure-bundle"}',
    )
    resp = function_app.sas_restricted(req)
    assert resp.status_code == 200
    payload = json.loads(resp.get_body())
    assert payload["url"].startswith("https://")
    assert payload["ttlMinutes"] == function_app.SAS_TTL_MINUTES
    mock_sas.assert_called_once_with("android/eastshorestructure-bundle")


@patch.dict("function_app.os.environ", {"SAS_API_KEY": "test-key"}, clear=False)
def test_anchors_list_requires_api_key():
    req = _request("GET", url="/api/anchors")
    resp = function_app.anchors_list(req)
    assert resp.status_code == 401


@patch.dict("function_app.os.environ", {"SAS_API_KEY": "test-key"}, clear=False)
@patch("function_app._list_anchors")
def test_anchors_list_returns_firebase_shape(mock_list):
    mock_list.return_value = [
        AnchorRecord(
            id="a" * 32,
            name="Room A",
            identifier="asa-1",
            date_created="2026-01-01T00:00:00+00:00",
            date_expired="2026-12-31T00:00:00Z",
        )
    ]
    req = _request("GET", url="/api/anchors", headers={"X-API-Key": "test-key"})
    resp = function_app.anchors_list(req)
    assert resp.status_code == 200
    payload = json.loads(resp.get_body())
    assert payload == [
        {
            "name": "Room A",
            "identifier": "asa-1",
            "dateCreated": "2026-01-01T00:00:00+00:00",
            "dateExpired": "2026-12-31T00:00:00Z",
        }
    ]


@patch.dict("function_app.os.environ", {"SAS_API_KEY": "test-key"}, clear=False)
@patch("function_app._list_anchors", side_effect=RuntimeError("Anchor table storage is not configured"))
def test_anchors_list_returns_503_when_storage_unconfigured(mock_list):
    req = _request("GET", url="/api/anchors", headers={"X-API-Key": "test-key"})
    resp = function_app.anchors_list(req)
    assert resp.status_code == 503


@patch.dict("function_app.os.environ", {"SAS_API_KEY": "test-key"}, clear=False)
def test_anchors_create_rejects_invalid_body():
    req = _request(
        "POST",
        url="/api/anchors",
        headers={"X-API-Key": "test-key"},
        body='{"name":""}',
    )
    resp = function_app.anchors_create(req)
    assert resp.status_code == 400


@patch.dict("function_app.os.environ", {"SAS_API_KEY": "test-key"}, clear=False)
@patch("function_app._store_anchor")
def test_anchors_create_returns_201(mock_store):
    req = _request(
        "POST",
        url="/api/anchors",
        headers={"X-API-Key": "test-key"},
        body=json.dumps(
            {
                "name": "Room B",
                "identifier": "asa-2",
                "dateExpired": "2026-12-31T00:00:00Z",
            }
        ),
    )
    resp = function_app.anchors_create(req)
    assert resp.status_code == 201
    payload = json.loads(resp.get_body())
    assert payload["name"] == "Room B"
    assert payload["identifier"] == "asa-2"
    assert len(payload["id"]) == 32
    mock_store.assert_called_once()


@patch.dict("function_app.os.environ", {"SAS_API_KEY": "test-key"}, clear=False)
@patch("function_app._fetch_anchor", return_value=None)
def test_anchors_get_returns_404_when_missing(mock_fetch):
    anchor_id = "b" * 32
    req = _request(
        "GET",
        url=f"/api/anchors/{anchor_id}",
        headers={"X-API-Key": "test-key"},
        route_params={"anchor_id": anchor_id},
    )
    resp = function_app.anchors_get(req)
    assert resp.status_code == 404
    mock_fetch.assert_called_once_with(anchor_id)


@patch.dict("function_app.os.environ", {"SAS_API_KEY": "test-key"}, clear=False)
@patch("function_app._fetch_anchor")
def test_anchors_get_returns_record(mock_fetch):
    anchor_id = "c" * 32
    mock_fetch.return_value = AnchorRecord(
        id=anchor_id,
        name="Room C",
        identifier="asa-3",
        date_created="2026-02-01T00:00:00Z",
        date_expired="2026-03-01T00:00:00Z",
    )
    req = _request(
        "GET",
        url=f"/api/anchors/{anchor_id}",
        headers={"X-API-Key": "test-key"},
        route_params={"anchor_id": anchor_id},
    )
    resp = function_app.anchors_get(req)
    assert resp.status_code == 200
    payload = json.loads(resp.get_body())
    assert payload["id"] == anchor_id
    assert payload["name"] == "Room C"
