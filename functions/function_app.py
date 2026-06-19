"""Azure Function: issue short-lived user-delegation SAS URLs for restricted bundles.

A Unity client cannot fetch a private blob anonymously. ``POST /sas/restricted``
mints a read-only, single-blob, <=15-minute *user-delegation* SAS so the client can
GET one allow-listed bundle — with no storage account key in the app or in the
Function's settings. See docs/auth-backend.md for the security model and its limits
(notably: the client API key is extractable from the APK, so this is friction, not
per-user authorization).
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

from sas_auth import DEFAULT_ALLOWLIST, api_key_valid, bundle_allowed, parse_allowlist

# Function App application settings (see local.settings.json.example).
STORAGE_ACCOUNT = os.environ.get("STORAGE_ACCOUNT_NAME", "haringerverdiag")
RESTRICTED_CONTAINER = os.environ.get("RESTRICTED_CONTAINER", "restricted")
API_KEY_SETTING = "SAS_API_KEY"
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
