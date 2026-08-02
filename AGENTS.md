# xr-geoxplorer

Guidance for coding agents and contributors working in this repo.

## Commit messages

This repo has student/collaborator contributors. Commit messages here
target them — high-level, plain English:

- Subject ≤72 chars saying what changed, from a repo user's point of view.
- Body: 2–4 sentences on what changed and why it matters to someone using
  the repo. No internal infrastructure jargon (run ids, hostnames,
  pipeline internals, phase numbers).
- Mechanism detail belongs in the PR description or code comments, not
  the commit message.

## Cursor Cloud specific instructions

This repo is mainly a **Unity 2022.3.62f2 Meta Quest 3 app** (see
`README.md` / `CONTRIBUTING.md` / `HANDOFF.md`). The Unity app cannot be
built or run in the cloud VM — it needs a Unity license and XR
hardware/GUI, and is only built in CI via GameCI (`.github/workflows/
android-build.yml`, `unity-tests.yml`) when Unity secrets are set. What
*is* runnable in the cloud VM is the supporting tooling below.

The startup update script provisions a Python venv at `/workspace/.venv`
(Azure Functions deps + `pytest`) and restores the `yield-lint` .NET
project. `.NET 8 SDK`, Azure Functions Core Tools v4 (`func`), and
`python3-venv` are baked into the VM image, not the update script.
`func` lives at `~/.npm-global/bin` and is added to PATH in `~/.bashrc`.

- **C# lint** (`tools/yield-lint`, the `C# Lint` CI gate): scans `Assets/`
  for Unity-specific C# mistakes. Run: `dotnet run --project tools/yield-lint -- Assets`.
  The `CS9057` analyzer-version warnings during build are harmless.
- **Azure Functions auth backend** (`functions/`): the `sas/restricted`
  SAS-issuing HTTP function. Unit tests (no Azure/network needed):
  `cd functions && /workspace/.venv/bin/python -m pytest`. To run the host,
  create `functions/local.settings.json` from `local.settings.json.example`
  (it is git-ignored; set any `SAS_API_KEY`), then from `functions/` with the
  venv active run `func start`. Endpoint: `POST http://localhost:7071/api/sas/restricted`.
  The `AzureWebJobsStorage` "Unhealthy" log line is expected and does not
  block the HTTP function. Requests with a valid key + allow-listed bundle
  reach the real Azure user-delegation SAS call and return HTTP 500
  ("Failed to issue SAS") in the VM because there are no Azure
  managed-identity credentials — this is the expected local terminus, and
  `DefaultAzureCredential` makes that one path take ~60–90s to fail.
- **AssetBundle scripts** (`scripts/`): plain Python CLIs. Run with the venv
  python. `build_source_mapping.py <index>` **overwrites the tracked
  `docs/assetbundle-source-mapping.csv`** — `git checkout` it afterward if you
  only ran it to experiment. `upload_assetbundles_to_azure.py --dry-run`
  validates an upload plan without touching Azure.
  `compare_bundle_manifest.py --platform android --build-dir AssetBundles/android`
  compares a local bake folder to `docs/assetbundle-metadata-manifest.json`
  (add `--allow-missing` for partial #6 bakes).
  `compare_manifest_to_inventory.py` cross-checks the manifest against
  `docs/azure-haringerverdiag-inventory.csv` (no Azure/network needed).
  `test_sas_function.sh` and `test_anchor_function.sh` exercise local Function
  endpoints (needs `func start` in `functions/`).
- **Functions CI** (`.github/workflows/functions-tests.yml`): runs
  `python -m pytest functions/tests/` on every PR — no Azure credentials needed.
- **PR merge order** for open agent branches: see
  [`docs/pr-merge-guide.md`](docs/pr-merge-guide.md).
