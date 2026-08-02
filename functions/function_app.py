"""Azure Function: SAS issuance (#24) and anchor persistence (#40 Phase B).

See docs/auth-backend.md and docs/firebase-anchor-audit.md.
"""

from __future__ import annotations

import datetime as dt
import json
import logging
import os
from urllib.parse import quote

import azure.functions as func
from azure.identity import DefaultAzureCredential
from azure.storage.blob import BlobSasPermissions, BlobServiceClient, generate_blob_sas

from anchor_persistence import (
    AnchorRecord,
    normalize_anchor_id,
    parse_create_body,
    record_to_json,
)
from sas_auth import DEFAULT_ALLOWLIST, api_key_valid, bundle_allowed, parse_allowlist

# Function App application settings (see local.settings.json.example).
STORAGE_ACCOUNT = os.environ.get("STORAGE_ACCOUNT_NAME", "haringerverdiag")
RESTRICTED_CONTAINER = os.environ.get("RESTRICTED_CONTAINER", "restricted")
API_KEY_SETTING = "SAS_API_KEY"
ANCHOR_TABLE_NAME = os.environ.get("ANCHOR_TABLE_NAME", "geoxanchors")
# Hard cap the SAS lifetime; a request can never widen it.
TTL_CAP_MINUTES = 15
SAS_TTL_MINUTES = min(int(os.environ.get("SAS_TTL_MINUTES", "15")), TTL_CAP_MINUTES)


def _make_sas_url(bundle: str) -> str:
    """Mint a read-only, single-blob, TTL-bounded user-delegation SAS URL.

    Uses the Function's managed identity (via ``DefaultAzureCredential``) to obtain a
    user delegation key — no storage account key is involved.
    """
    account_url = f"https://{STORAGE_ACCOUNT}.blob.core.windows.net"
    service = BlobServiceClient(account_url, credential=DefaultAzureCredential())

    now = dt.datetime.now(dt.timezone.utc)
    expiry = now + dt.timedelta(minutes=SAS_TTL_MINUTES)
    delegation_key = service.get_user_delegation_key(key_start_time=now, key_expiry_time=expiry)

    sas = generate_blob_sas(
        account_name=STORAGE_ACCOUNT,
        container_name=RESTRICTED_CONTAINER,
        blob_name=bundle,
        user_delegation_key=delegation_key,
        permission=BlobSasPermissions(read=True),
        expiry=expiry,
        start=now,
    )
    return f"{account_url}/{RESTRICTED_CONTAINER}/{quote(bundle)}?{sas}"


def _table_client():
    """Return a Table client when ANCHOR_TABLE_CONNECTION or AzureWebJobsStorage is set."""
    connection = os.environ.get("ANCHOR_TABLE_CONNECTION") or os.environ.get("AzureWebJobsStorage")
    if not connection:
        return None
    try:
        from azure.data.tables import TableServiceClient
    except ImportError:
        logging.error("azure-data-tables package is not installed")
        return None
    service = TableServiceClient.from_connection_string(connection)
    try:
        service.create_table_if_not_exists(ANCHOR_TABLE_NAME)
    except Exception:  # noqa: BLE001
        logging.exception("Failed to ensure anchor table exists")
    return service.get_table_client(ANCHOR_TABLE_NAME)


def _store_anchor(record: AnchorRecord) -> None:
    client = _table_client()
    if client is None:
        raise RuntimeError("Anchor table storage is not configured")
    client.create_entity(record.to_table_entity())


def _fetch_anchor(anchor_id: str) -> AnchorRecord | None:
    client = _table_client()
    if client is None:
        raise RuntimeError("Anchor table storage is not configured")
    try:
        entity = client.get_entity(partition_key="anchor", row_key=anchor_id)
        return AnchorRecord.from_table_entity(entity)
    except Exception:  # noqa: BLE001
        return None


# ANONYMOUS at the platform layer; the X-API-Key header is the (intentionally modest)
# gate, checked in code so the client uses the contract documented in auth-backend.md.
app = func.FunctionApp(http_auth_level=func.AuthLevel.ANONYMOUS)


@app.route(route="sas/restricted", methods=["POST"])
def sas_restricted(req: func.HttpRequest) -> func.HttpResponse:
    if not api_key_valid(req.headers.get("X-API-Key"), os.environ.get(API_KEY_SETTING)):
        return func.HttpResponse("Unauthorized", status_code=401)

    try:
        body = req.get_json()
    except ValueError:
        return func.HttpResponse("Body must be JSON with a 'bundle' field", status_code=400)

    bundle = (body or {}).get("bundle")
    if not isinstance(bundle, str) or not bundle:
        return func.HttpResponse("Missing 'bundle'", status_code=400)

    if not bundle_allowed(bundle, parse_allowlist(os.environ.get("SAS_BUNDLE_ALLOWLIST"), DEFAULT_ALLOWLIST)):
        return func.HttpResponse("Bundle not allowed", status_code=403)

    try:
        url = _make_sas_url(bundle)
    except Exception:  # noqa: BLE001 - return a generic 500, log the detail server-side
        logging.exception("Failed to mint SAS for %s", bundle)
        return func.HttpResponse("Failed to issue SAS", status_code=500)

    return func.HttpResponse(
        json.dumps({"url": url, "ttlMinutes": SAS_TTL_MINUTES}),
        status_code=200,
        mimetype="application/json",
    )


@app.route(route="anchors", methods=["POST"])
def anchors_create(req: func.HttpRequest) -> func.HttpResponse:
    if not api_key_valid(req.headers.get("X-API-Key"), os.environ.get(API_KEY_SETTING)):
        return func.HttpResponse("Unauthorized", status_code=401)

    try:
        body = req.get_json()
    except ValueError:
        return func.HttpResponse("Body must be JSON", status_code=400)

    record, error = parse_create_body(body)
    if error:
        return func.HttpResponse(error, status_code=400)

    try:
        _store_anchor(record)
    except RuntimeError:
        return func.HttpResponse("Anchor storage not configured", status_code=503)
    except Exception:  # noqa: BLE001
        logging.exception("Failed to store anchor %s", record.name)
        return func.HttpResponse("Failed to store anchor", status_code=500)

    return func.HttpResponse(
        json.dumps(record_to_json(record)),
        status_code=201,
        mimetype="application/json",
    )


@app.route(route="anchors/{anchor_id}", methods=["GET"])
def anchors_get(req: func.HttpRequest) -> func.HttpResponse:
    if not api_key_valid(req.headers.get("X-API-Key"), os.environ.get(API_KEY_SETTING)):
        return func.HttpResponse("Unauthorized", status_code=401)

    anchor_id = normalize_anchor_id(req.route_params.get("anchor_id"))
    if not anchor_id:
        return func.HttpResponse("Invalid anchor id", status_code=400)

    try:
        record = _fetch_anchor(anchor_id)
    except RuntimeError:
        return func.HttpResponse("Anchor storage not configured", status_code=503)
    except Exception:  # noqa: BLE001
        logging.exception("Failed to fetch anchor %s", anchor_id)
        return func.HttpResponse("Failed to fetch anchor", status_code=500)

    if record is None:
        return func.HttpResponse("Not found", status_code=404)

    return func.HttpResponse(
        json.dumps(record_to_json(record)),
        status_code=200,
        mimetype="application/json",
    )
