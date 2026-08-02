# Azure Function provisioning runbook (#24 / #37 Phase B)

**Audience:** project lead with Azure subscription access. The cloud agent cannot
run these steps — they require live credentials and portal/CLI access.

**Goal:** deploy the Python Function App in [`functions/`](../functions/) so the
Unity app can request restricted-bundle SAS URLs (#24) and (optionally) persist
anchor records (#40) instead of the public Firebase endpoint.

## Prerequisites

- Subscription: `3638bb5a-7ca5-45f2-a4a8-46cf562bd53e` (see
  [`docs/azure-cleanup-plan.md`](azure-cleanup-plan.md))
- Resource group: `SharingServer` (existing)
- Storage account: `haringerverdiag` (existing)
- A unique Function App name (example: `geoxplorer-sas`)

## 1. Create Function App (Python 3.11, Linux)

```bash
FUNC_NAME=geoxplorer-sas
RG=SharingServer
LOCATION=centralus

az functionapp create \
  --resource-group "$RG" \
  --consumption-plan-location "$LOCATION" \
  --runtime python \
  --runtime-version 3.11 \
  --functions-version 4 \
  --name "$FUNC_NAME" \
  --storage-account haringerverdiag \
  --os-type Linux
```

Enable **system-assigned managed identity**:

```bash
az functionapp identity assign -g "$RG" -n "$FUNC_NAME"
```

Note the `principalId` from the output — needed for RBAC in step 3.

## 2. Application settings

Set via portal (Configuration → Application settings) or CLI:

| Setting | Example | Purpose |
|---|---|---|
| `SAS_API_KEY` | *(generate 32+ char secret)* | Client `X-API-Key` gate (#24) |
| `STORAGE_ACCOUNT_NAME` | `haringerverdiag` | Blob account for SAS signing |
| `RESTRICTED_CONTAINER` | `restricted` | Private container name |
| `SAS_BUNDLE_ALLOWLIST` | `["android/eastshorestructure-bundle","android/yayamari-bundle"]` | Server-side bundle gate |
| `SAS_TTL_MINUTES` | `15` | Capped at 15 in code |
| `ANCHOR_TABLE_NAME` | `geoxanchors` | Table for #40 anchor records |
| `ANCHOR_TABLE_CONNECTION` | *(storage connection string)* | Table Storage (or reuse `AzureWebJobsStorage`) |

Copy the same `SAS_API_KEY` into the active RemoteConfig asset
(`sasApiKey` field) when testing on device.

## 3. RBAC — managed identity on storage

Grant the Function identity **Storage Blob Data Reader** on the **restricted**
container only (not the whole account):

```bash
PRINCIPAL_ID=<from step 1>
SCOPE="/subscriptions/3638bb5a-7ca5-45f2-a4a8-46cf562bd53e/resourceGroups/SharingServer/providers/Microsoft.Storage/storageAccounts/haringerverdiag/blobServices/default/containers/restricted"

az role assignment create \
  --assignee-object-id "$PRINCIPAL_ID" \
  --assignee-principal-type ServicePrincipal \
  --role "Storage Blob Data Reader" \
  --scope "$SCOPE"
```

For anchor Table Storage, grant **Storage Table Data Contributor** on the
Function's backing storage account (or a dedicated tables account).

## 4. Privatize the restricted container (#37 Phase B)

Current state: `restricted` is **public container access** (~153 MB gated lab
content). After SAS path is verified:

1. Azure Portal → Storage account → Containers → `restricted` → Change access
   level to **Private**.
2. Confirm anonymous GET of a blob URL returns 403/404.
3. Confirm `POST /api/sas/restricted` with valid key + allow-listed bundle
   returns a working read SAS (see smoke tests below).

See [`docs/restricted-container-audit.md`](restricted-container-audit.md) for
why the app does not load these bundles today — privatization is still
recommended for storage hygiene.

## 5. Deploy function code

From repo root (requires [Azure Functions Core Tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)):

```bash
cd functions
func azure functionapp publish geoxplorer-sas
```

Endpoints after deploy:

- `POST https://<FUNC_NAME>.azurewebsites.net/api/sas/restricted`
- `GET/POST https://<FUNC_NAME>.azurewebsites.net/api/anchors`

## 6. Smoke tests

**Local** (no Azure credentials for SAS mint; anchor routes return 503 without table):

```bash
cd functions && func start
# separate terminal:
./scripts/test_sas_function.sh
./scripts/test_anchor_function.sh
```

**Deployed** (full SAS path):

```bash
export SAS_ENDPOINT=https://geoxplorer-sas.azurewebsites.net/api/sas/restricted
export SAS_API_KEY=<your-key>
./scripts/test_sas_function.sh
```

**Unity:** set `sasEndpointBaseUrl` on RemoteConfig to
`https://<FUNC_NAME>.azurewebsites.net` and run
`RestrictedBundleSasClient.RequestSasUrl` from a test scene.

## 7. Wire Unity RemoteConfig

On `RemoteConfig.Dev` / `Staging` / `Prod`:

| Field | Value |
|---|---|
| `sasEndpointBaseUrl` | `https://<FUNC_NAME>.azurewebsites.net` |
| `sasApiKey` | same as `SAS_API_KEY` |

Anchor backend (#40) uses the same base URL. Firebase URL (`firebaseAnchorsUrl`)
stays until #17 + #23 land and `FirebaseExchanger` is swapped for
`AnchorBackendClient`.

## Related docs

- [`docs/auth-backend.md`](auth-backend.md) — endpoint contracts
- [`docs/firebase-anchor-audit.md`](firebase-anchor-audit.md) — migration context
- [`docs/restricted-container-audit.md`](restricted-container-audit.md) — Phase A findings
- [`AGENTS.md`](../AGENTS.md) — local dev without Azure credentials
