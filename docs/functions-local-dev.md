# Azure Functions local development (with Azurite)

Run the auth backend (`functions/`) locally for anchor API smoke tests. SAS mint
still fails without Azure managed identity — that is expected. Anchor
persistence works fully with **Azurite** Table Storage.

## Quick start

```bash
# Terminal 1 — Azurite (Table endpoint on :10002)
./scripts/start_azurite.sh

# Terminal 2 — Function host
./scripts/run_functions_local.sh

# Terminal 3 — smoke tests
./scripts/test_anchor_function.sh
./scripts/test_sas_function.sh
```

## Azurite connection strings

Well-known dev storage account (Microsoft docs):

| Service | Endpoint |
|---|---|
| Blob | `http://127.0.0.1:10000/devstoreaccount1` |
| Queue | `http://127.0.0.1:10001/devstoreaccount1` |
| Table | `http://127.0.0.1:10002/devstoreaccount1` |

Account key (same for all local Azurite):

```text
Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==
```

Set in `functions/local.settings.json` (gitignored):

```json
{
  "Values": {
    "SAS_API_KEY": "local-dev-key",
    "ANCHOR_TABLE_CONNECTION": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;"
  }
}
```

Copy from [`functions/local.settings.json.example`](../functions/local.settings.json.example).

## What works locally

| Endpoint | With Azurite | Without Azurite |
|---|---|---|
| `GET /api/anchors` | 200 + JSON array | 503 |
| `POST /api/anchors` | 201 + record | 503 |
| `GET /api/anchors/{id}` | 200 / 404 | 503 |
| `POST /api/sas/restricted` (auth gates) | 401/403 work | same |
| `POST /api/sas/restricted` (mint SAS) | 500 (~60–90s) | 500 — no managed identity |

## Prerequisites

- Python 3.11+ with venv (`pip install -r functions/requirements.txt pytest`)
- Azure Functions Core Tools v4 (`func`) — on cloud VM: `~/.npm-global/bin/func`
- Azurite — `npm install -g azurite` (or use `./scripts/start_azurite.sh` which installs on first run)

## Unit tests (no Azurite)

```bash
cd functions && python -m pytest tests/ -q
```

26 tests including mocked route handlers — runs in CI.

## Related

- [`docs/auth-backend.md`](auth-backend.md) — endpoint contracts
- [`docs/azure-function-provisioning.md`](azure-function-provisioning.md) — production deploy
- [`AGENTS.md`](../AGENTS.md) — cloud VM notes
