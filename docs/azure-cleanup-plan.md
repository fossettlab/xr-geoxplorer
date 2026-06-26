# Azure cleanup plan — haringerverdiag

Goal: get the project's Azure footprint into a clean, right-sized, deadline-safe
state. The **only** thing the app needs Azure for is asset/content delivery for
the eventual xr-geoxplorer app, which is served from the `haringerverdiag`
storage account. Everything else is housekeeping.

## Scope / constants

- Subscription: `3638bb5a-7ca5-45f2-a4a8-46cf562bd53e` (EPS – Fossett Lab for Virtual Planetary Exploration)
- Resource group (keep): `SharingServer`
- Storage account: `haringerverdiag`

## Current state (verified 2026-06-25)

- `kind: Storage` — General Purpose **v1** (created 2018-09-26)
- `sku: Standard_RAGRS` — Read-Access Geo-Redundant (the most redundant tier)
- `accessTier: null` — v1 does not support access tiers
- Five resource groups; only `SharingServer` holds a resource (`haringerverdiag`).
  The other four are empty.

Note on magnitude: ~55 GB at rest is a small dollar amount; this work is about a
clean, right-sized, deadline-safe state rather than large savings. The app's data
is reproducible (AssetBundles bake from source on the NAS), which is why reduced
redundancy is acceptable.

## Plane / tooling note

Management-plane `az` calls (account config, RG deletes, lifecycle policy) work
normally. Data-plane blob operations (listing, set-tier, delete) hit the
Homebrew-python pyexpat bug in the az-bundled CLI, so they run via
`doppler run --project mac --config dev -- uv run --with azure-storage-blob python <script>`
(connection string from doppler env).

## Phase 0 — Snapshot + inventory (read-only, first)

- Save current config: `az storage account show ... -o json > azure-snapshot-pre.json`
- Refresh blob inventory (container / name / size / last-modified) into
  `docs/azure-storage-inventory.md` via the data-plane workaround above.
- Confirm nothing references the RA-GRS secondary endpoint
  (`haringerverdiag-secondary.blob.core.windows.net`); the app's `FetchAssetBundle`
  and the #24 SAS function both use the primary endpoint.

## Phase 1 — RA-GRS to LRS (reversible)

```
az storage account update -n haringerverdiag -g SharingServer \
  --subscription 3638bb5a-7ca5-45f2-a4a8-46cf562bd53e --sku Standard_LRS
```

- Online, no data movement. Reversible (back to `Standard_RAGRS`).
- Verify: `sku == Standard_LRS`.

## Phase 2 — GPv1 to GPv2 (one-way)

```
az storage account update -n haringerverdiag -g SharingServer \
  --subscription 3638bb5a-7ca5-45f2-a4a8-46cf562bd53e --upgrade-to-storagev2
```

- Avoids the Oct-2026 forced v1 migration; unlocks access tiers for Phase 3.
- Irreversible, but strictly better; HTTP blob fetch + user-delegation SAS are
  unaffected.
- Verify: `kind == StorageV2`; spot-check a bundle URL resolves and the #24 SAS
  path still issues.

## Phase 3 — Stale-data pass (deferred until after the #6/#82 re-bake)

- Classify from the Phase-0 inventory. Active bundles stay Hot; **never Archive
  active bundles** (hours-long rehydration breaks runtime delivery).
- Delete candidates (NAS holds canonical source): pre-2022 `bio` pre-baked
  bundles, superseded bundle versions, the privatized/orphaned `restricted`
  scenes, and the `geoxplorer-source` drop if redundant with the NAS source.
- Cool candidates: bundles kept but rarely read.
- Mechanism: bulk delete/tier via the SDK (data-plane workaround), or a
  lifecycle-management policy (management-plane) keyed on last-access tracking.
- Timing: defer the bulk until the re-bake settles, since much current blob data
  is about to be replaced.

## Phase 4 — Empty resource-group cleanup ($0, tidiness)

Decision (2026-06-25): the app's only Azure dependency is `haringerverdiag` in
`SharingServer`, so all four empty RGs are removed; a future backend (#40)
creates its own properly-named RG when built.

```
az group delete -n FossetLabResourceGroup   --subscription 3638bb5a-7ca5-45f2-a4a8-46cf562bd53e --yes
az group delete -n myResourceGroup          --subscription 3638bb5a-7ca5-45f2-a4a8-46cf562bd53e --yes
az group delete -n FossettLabSharingService --subscription 3638bb5a-7ca5-45f2-a4a8-46cf562bd53e --yes
az group delete -n GeoXplorerBackend        --subscription 3638bb5a-7ca5-45f2-a4a8-46cf562bd53e --yes
```

- Re-verify each is empty immediately before deleting. Empty RGs cost nothing;
  this is hygiene, not savings.

## Execution grouping

- **Batch A** — Phases 1, 2, 4: quick, management-plane, low-risk (GPv2 the only
  one-way change). Verify after each.
- **Phase 3** — separate, gated on the refreshed inventory and the re-bake.
