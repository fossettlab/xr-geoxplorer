# Auth backend — SAS issuance Function

The Unity app loads AssetBundles directly from Azure Blob Storage. Public containers
are fetched anonymously; a **private** blob cannot be. This Function issues a
short-lived, signed URL (a SAS) so the client can read one private bundle without any
storage account key living in the app.

Code: [`functions/`](../functions/). Runtime: Python (Azure Functions v2 model).

## What this does and does not secure

The client API key is shipped inside the Unity APK and is **extractable** — anyone who
unpacks the build can read it. The `X-API-Key` check is therefore **friction, not
per-user authorization**. The real, meaningful win is removing *anonymous* listing and
direct download of private content. True user-level access control would need device
attestation or a user identity (Entra ID, etc.) and is out of scope.

## Endpoint contract

```
POST /api/sas/restricted
Header:  X-API-Key: <key>
Body:    {"bundle": "android/eastshorestructure-bundle"}

200  {"url": "https://haringerverdiag.blob.core.windows.net/restricted/<bundle>?<sas>",
      "ttlMinutes": 15}
401  invalid / missing X-API-Key
400  body not JSON, or missing "bundle"
403  "bundle" not on the server-side allowlist
500  SAS generation failed (detail logged server-side)
```

The client then issues a plain `GET` against the returned `url`.

### Unity client (EditMode-testable)

[`RestrictedBundleSasClient.cs`](../Assets/Scripts/Config/RestrictedBundleSasClient.cs)
builds the POST URL, attaches `X-API-Key` from `RemoteConfig.Current.SasApiKey`, and
parses the JSON response. Example:

```csharp
StartCoroutine(RestrictedBundleSasClient.RequestSasUrl(
    "android/eastshorestructure-bundle",
    (url, ttlMinutes) => StartCoroutine(DownloadFromSignedUrl(url)),
    error => Debug.LogError(error)));
```

Smoke test on device or Editor: configure `sasEndpointBaseUrl` + `sasApiKey` on the
active RemoteConfig asset, call the client, then `UnityWebRequest.Get` the returned URL.

## User-delegation SAS flow

1. The Function App runs with a **system-assigned managed identity**.
2. That identity is granted **Storage Blob Data Reader** on the `restricted` container only.
3. Per request, the Function obtains a *user delegation key* (`DefaultAzureCredential`,
   no account key) and signs a SAS scoped to: **one named blob**, **read only**,
   **TTL ≤ 15 min**. The request cannot widen any of these — the TTL is capped in code
   and the blob must be on the allowlist.
4. **No storage account key** is ever in the app, the Function settings, or this repo.

## Configuration (Function App application settings)

| Setting | Purpose |
|---|---|
| `STORAGE_ACCOUNT_NAME` | `haringerverdiag` |
| `RESTRICTED_CONTAINER` | `restricted` |
| `SAS_TTL_MINUTES` | SAS lifetime, capped at 15 in code |
| `SAS_API_KEY` | the client key; compared constant-time against `X-API-Key` |
| `SAS_BUNDLE_ALLOWLIST` | optional JSON array overriding the default 6-blob allowlist |

`SAS_API_KEY` is never committed. Locally, copy `local.settings.json.example` to
`local.settings.json` (gitignored) and set a value. The client receives the key via
the #25 RemoteConfig (`sasApiKey` / `sasEndpointBaseUrl`), not hardcoded.

## Status

**Code scaffolded; not yet provisioned live.** Python Function + unit tests live under
[`functions/`](../functions/). Unity client helper:
[`RestrictedBundleSasClient.cs`](../Assets/Scripts/Config/RestrictedBundleSasClient.cs)
reads `sasEndpointBaseUrl` / `sasApiKey` from RemoteConfig (#25) and POSTs to this
endpoint. The app does not yet call it from download paths — wire-up lands with #37
when restricted bundles are fetched again.

The endpoint has no production consumer until #25 values are set in the deployed
RemoteConfig assets and #37 reconnects the restricted scene downloads. Provision live
Azure infra when those tickets are ready, so the Function is not run unused.

## Provisioning (when ready)

Storage account `haringerverdiag` lives in the **EPS – Fossett Lab for Virtual Planetary
Exploration** subscription (`3638bb5a-7ca5-45f2-a4a8-46cf562bd53e`), resource group
**SharingServer**, region **centralus**. Provision in that subscription:

```bash
SUB=3638bb5a-7ca5-45f2-a4a8-46cf562bd53e
RG=SharingServer
LOC=centralus
ACCT=haringerverdiag
APP=geoxplorer-sas            # Function App name (must be globally unique; adjust if taken)

az account set --subscription "$SUB"

# A dedicated storage account for the Function runtime (separate from the data account)
az storage account create \
  --name geoxsasfuncstore --resource-group "$RG" --location "$LOC" --sku Standard_LRS

az functionapp create \
  --name "$APP" --resource-group "$RG" \
  --storage-account geoxsasfuncstore \
  --consumption-plan-location "$LOC" \
  --runtime python --runtime-version 3.11 --functions-version 4 \
  --os-type Linux --assign-identity '[system]'

# Grant the Function's managed identity read on the restricted container only
PRINCIPAL=$(az functionapp identity show -n "$APP" -g "$RG" --query principalId -o tsv)
SCOPE=$(az storage account show -n "$ACCT" -g "$RG" --query id -o tsv)/blobServices/default/containers/restricted
az role assignment create \
  --assignee "$PRINCIPAL" \
  --role "Storage Blob Data Reader" \
  --scope "$SCOPE"

# App settings (set SAS_API_KEY to a strong random value, kept out of source)
az functionapp config appsettings set -n "$APP" -g "$RG" --settings \
  STORAGE_ACCOUNT_NAME="$ACCT" RESTRICTED_CONTAINER=restricted SAS_TTL_MINUTES=15

# Deploy from functions/
cd functions && func azure functionapp publish "$APP"
```

After deploy, record the final Function App name + URL here, then smoke-test: request a
SAS with the API key and `GET` the bundle from the signed URL.

## Local development

```bash
cd functions
python -m venv .venv && source .venv/bin/activate
pip install -r requirements.txt
cp local.settings.json.example local.settings.json   # set SAS_API_KEY
func start                                            # needs Azure Functions Core Tools
```

Run the unit tests (no Azure SDK / credentials needed — pure gating logic):

```bash
cd functions && python -m pytest tests/
```
