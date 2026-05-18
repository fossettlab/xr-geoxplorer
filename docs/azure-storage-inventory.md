# Azure Blob Storage inventory — `haringerverdiag`

**Snapshot date:** 2026-05-18
**Scan method:** authenticated audit of the live storage account
**Account:** `haringerverdiag.blob.core.windows.net`
**Type:** General Purpose v1 (GPv1), Standard, RA-GRS
**Region:** Central US (secondary: East US 2)
**Created:** 2018-09-26

This file is a human-readable snapshot. The machine-readable inventory is committed alongside it:

- [`azure-haringerverdiag-inventory.json`](azure-haringerverdiag-inventory.json) — full per-blob inventory with name, size, content-type, created/lastModified timestamps, access tier, and custom metadata. **This is the canonical source.**
- [`azure-haringerverdiag-inventory.csv`](azure-haringerverdiag-inventory.csv) — same data, CSV form.

Update both whenever container contents change materially. Source of truth is the Azure portal.

## At a glance

- **23 containers**
- **8,053 blobs**
- **45.76 GB total**
- Most content uploaded 2020–2021; one container has a 2022 addition (`geobase-outcrop`, 2022-05-25)
- **13 of 23 containers are publicly readable** (Container-level access)
- Account-level public read was configured at some point; no anonymous-block has been enforced

## Why this file exists

The Unity app fetches AssetBundles and thumbnails from this account at runtime. URLs are hardcoded in `Assets/Scripts/MenuManager.cs`, `Assets/Scripts/MobileMenuManager.cs`, `Assets/Scripts/FetchAssetBundle.cs`, and `Assets/Scripts/FetchSpatialMetadata.cs`. This inventory lets the modernization tickets reason about deployed content directly instead of inferring from code.

Relevant tickets:
- **#6 (Re-bake AssetBundles)** — must produce bundles matching the deployed structure, not just whatever the NAS pipeline outputs.
- **#25 (RemoteConfig ScriptableObject)** — needs to know which containers are in production use vs. legacy.
- **#24 (Replace Firebase endpoint)** — adjacent: same auth posture (public-read) audit.
- **New tickets proposed** at the bottom of this doc.

## Container inventory

| Container | Access | Blobs | Size | Earliest | Latest | Role |
|---|---|---|---|---|---|---|
| `$logs` | None | 0 | 0 | — | — | Storage Analytics; empty |
| `android` | **Container (public)** | 922 | 13.43 GB | 2019-10-01 | 2021-03-23 | **In-use** — Android AssetBundles |
| `ios` | **Container (public)** | 919 | 13.55 GB | 2020-06-01 | 2021-03-23 | **In-use** — iOS AssetBundles |
| `wsa` | **Container (public)** | 912 | 13.30 GB | 2020-01-20 | 2021-03-23 | **In-use** — HoloLens/UWP AssetBundles |
| `thumbnails` | **Container (public)** | 913 | 76.85 MB | 2020-03-30 | 2021-03-23 | **In-use** — UI thumbnails + `FeaturedModels.txt` config |
| `restricted` | **Container (public)** ⚠️ | 6 | 153 MB | 2020-04-27 | 2020-04-27 | **In-use** — 2 gated scenes, 3 platforms each |
| `crystal-viewer` | Private | 46 | 106.92 MB | 2020-05-04 | 2020-05-04 | Sibling app — `.dae` mineral source |
| `geobase-crystallattice` | Private | 48 | 109.85 MB | 2020-05-04 | 2020-05-04 | Sibling — duplicates `crystal-viewer` |
| `geobase-dem` | Private | 3 | 211.31 MB | 2020-09-07 | 2020-09-08 | Sibling — 2× `.obj`, 1× `.zip` archive |
| `geobase-handsample` | Private | 0 | 0 | — | — | Empty |
| `geobase-outcrop` | Private | 13 | 80.59 MB | 2022-05-25 | 2022-05-25 | Sibling — 1 outcrop scene, 1 `.dae` + 12 textures |
| `geoviewer-assetbundles` | **Container (public)** | 62 | 710.78 MB | 2020-05-19 | 2020-05-19 | Sibling app — cross-platform bundles |
| `geoviewer-dem` | **Container (public)** | 10 | 142.73 MB | 2020-05-19 | 2020-05-19 | Sibling app |
| `geoviewer-handsample` | **Container (public)** | 30 | 494.39 MB | 2020-05-19 | 2020-05-19 | Sibling app |
| `geoviewer-hirise` | Blob (public per-blob) | 50 | 257.22 MB | 2020-05-19 | 2020-05-19 | Sibling app — HiRISE DTMs |
| `geoviewer-outcrop` | **Container (public)** | 27 | 626 MB | 2020-05-19 | 2020-05-19 | Sibling app |
| `handsample-assetbundles` | **Container (public)** | 60 | 445 MB | 2020-05-19 | 2020-05-19 | Legacy / non-prefixed bundles |
| `outcrop-dem-assetbundles` | **Container (public)** | 70 | 719 MB | 2020-05-19 | 2020-05-19 | Legacy / non-prefixed bundles |
| `production-crystallattice` | **Container (public)** | 48 | 109.85 MB | 2020-05-19 | 2020-05-19 | Raw source `.dae` (duplicate of `geobase-crystallattice`, but public) |
| `production-dem` | **Container (public)** | 285 | 383.34 MB | 2020-05-19 | 2020-09-08 | Raw `.obj/.mtl/.jpg` sources |
| `production-handsample` | **Container (public)** | 117 | 1.45 GB | 2020-05-19 | 2020-09-08 | Raw `.obj/.mtl` + textures |
| `production-outcrop` | **Container (public)** | 128 | 2.24 GB | 2020-05-19 | 2022-05-25 | Raw `.obj/.mtl` + textures |
| `tiledmodel` | Private | 1,633 | 200.78 MB | 2020-03-06 | 2020-03-09 | Cesium 3D Tiles (`.b3dm`) — non-Unity, web streaming |
| `tiledmodel2` | Private | 1,751 | 226.04 MB | 2020-03-09 | 2020-03-10 | Cesium 3D Tiles, second dataset |

## Source content for the AssetBundle re-bake (#6)

The `production-*` containers hold raw source assets (`.obj` / `.mtl` / `.dae` / textures) that the modernization re-bake can pull from. They cover **4 of the 9 source categories**:

| Category | Azure `production-*` | NAS-only |
|---|---|---|
| `archeology` | — | **NAS only** |
| `architecture` | — | **NAS only** |
| `arthistory` | — | **NAS only** |
| `bio` | — | **NAS only** |
| `crystallattice` | `production-crystallattice` (48 `.dae`) | |
| `dem` | `production-dem` (285 blobs) | |
| `drama` | — | **NAS only** |
| `handsample` | `production-handsample` (117 blobs) | |
| `outcrop` | `production-outcrop` (128 blobs) | |

**Source-content access path for the contractor dev:** the NAS at `/mnt/nas/dev/fossett_xr_apps/GeoXAssetBundles/Assets/<Category>~/` has all 9 source categories. The 4 categories with Azure mirrors are a redundant backup. Practical recommendation: do a one-time rsync of all 9 categories from NAS to a working location at the start of #6 — single source of truth, covers everything. Fall back to `production-*` containers only if NAS access is delayed.

## What's actually used by the xr-geoxplorer runtime

The Unity scripts hit exactly five containers:

1. **`android`** — list call with `?prefix=geoxplorer-<category>`, then per-blob download
2. **`ios`** — same pattern, different platform
3. **`wsa`** — same pattern, HoloLens/UWP
4. **`thumbnails`** — direct PNG fetch by `geoxplorer-<category>/<modelName>.png`
5. **`restricted`** — referenced by the gated-content logic (need to confirm in code grep)

Everything else (`crystal-viewer`, `geobase-*`, `geoviewer-*`, `handsample-assetbundles`, `outcrop-dem-assetbundles`, `production-*`, `tiledmodel*`) appears to belong to sibling Fossett Lab projects (crystal-viewer, geoviewer, sharing-server backed by `production-*`, and a Cesium web viewer). They live in the same storage account for cost / org reasons.

**The xr-geoxplorer modernization should not touch the sibling containers.** They have their own owners and lifecycles.

## Deployed content (xr-geoxplorer)

Per-platform breakdown of what's in `android` / `ios` / `wsa`:

| Category | Android | iOS | WSA |
|---|---|---|---|
| `geoxplorer-archeology` | 6 / 94 MB | 6 / 100 MB | 6 / 96 MB |
| `geoxplorer-architecture` | 1 / 21 MB | 1 / 22 MB | 1 / 21 MB |
| `geoxplorer-arthistory` | 14 / 108 MB | 14 / 124 MB | 16 / 118 MB |
| `geoxplorer-bio` | 17 / 24 MB | 17 / 24 MB | 17 / 24 MB |
| `geoxplorer-crystallattice` | 69 / 100 MB | 68 / 95 MB | 68 / 95 MB |
| `geoxplorer-dem` | 702 / 11.42 GB | 700 / 11.38 GB | 700 / 11.40 GB |
| `geoxplorer-drama` | 8 / 111 MB | 8 / 186 MB | 11 / 161 MB |
| `geoxplorer-featured` | 12 / 169 MB | 12 / 168 MB | **0 / 0 — missing** ⚠️ |
| `geoxplorer-handsample` | 53 / 598 MB | 53 / 652 MB | 53 / 615 MB |
| `geoxplorer-outcrop` | 40 / 783 MB | 40 / 790 MB | 40 / 784 MB |
| **Total** | **922 / 13.43 GB** | **919 / 13.55 GB** | **912 / 13.30 GB** |

DEM (digital elevation model) is by far the largest category — ~85% of total bundle volume. Featured count differs between platforms: iOS and Android have 12 featured bundles, WSA has none. Drama category sizes differ noticeably (Android 111 MB, iOS 186 MB, WSA 161 MB) — platform-specific content variants exist.

The `thumbnails` container is 1 PNG per model, matching the bundle catalog with one consolidation: thumbnails are flat (`<category>/<modelName>.png`), not split per platform.

## Top three findings that change the modernization plan

### 1. Bundle structure mismatch — NAS pipeline vs deployed bundles ⚠️

**The NAS `GeoXAssetBundles` pipeline outputs ONE bundle per category** (e.g., a single `archeology` bundle containing all archeology models). **The deployed bundles on Azure are ONE bundle per MODEL** (e.g., `geoxplorer-archeology/cromeleque-bundle`, `skarabrae-bundle`, etc.).

The runtime fetch code in `MenuManager.cs:105` confirms it expects per-model bundles:

```csharp
string url = "https://<account>.blob.core.windows.net/" + platformType +
             "?restype=container&comp=list&include=metadata&prefix=geoxplorer-" + indexType;
```

It calls Azure's blob-list API with the category prefix, then iterates the returned blob names. This only works with the per-model layout.

**Implication:** the NAS pipeline (Unity 2017-era, single bundle per category) cannot produce bundles that match what's deployed. Either:

- A newer version of the pipeline exists on another machine (Bill or another lab member may know).
- The deployed bundles were built by a different/manual process not preserved on the NAS.
- The pipeline was modified between NAS and deployment but the modifications weren't pushed back.

**Action:** ticket #6 (re-bake) needs scope expansion. The dev can't just open the NAS project and hit "build" — there's a structural transformation involved. Best path: build a small Editor script that produces per-model bundles, using the existing source content in `Assets/<Category>~/` as input.

### 2. Public access on the `restricted` container ⚠️

The container literally named `restricted` has **Container-level public access** — anyone with the URL can download the `eastshorestructure-bundle` and `yayamari-bundle` files for all three platforms. The "restriction" is purely in app-side logic; storage-level access control isn't enforcing anything.

If those scenes are meant to be access-controlled (the name implies they are), this is a finding. If "restricted" just meant "labeled as such for app-side gating," it's working as intended.

**Action:** confirm intent with whoever set this up (Bill?). If true restriction is needed, that's its own follow-up ticket — likely "move to private container, switch fetch to SAS tokens issued by the new Azure Function in #24."

### 3. The runtime depends on `thumbnails/FeaturedModels.txt`

A 476-byte plain-text config file at the root of the `thumbnails` container drives the "featured" section of the app. This was not visible in the codebase grep because it's loaded at runtime — there's no path constant for it in source.

**Action:** include `FeaturedModels.txt` in #25 (RemoteConfig) scope so any URL move covers it. Back it up before any container rewrites.

## Other notes (informational, no ticket changes)

- **WSA missing `geoxplorer-featured/`.** 12 featured bundles exist for Android and iOS but were never built for HoloLens. If the featured section ever shipped on HL2 it would have been empty. Likely an old known issue, not new modernization scope.
- **One `.zip` archive** in `geobase-dem/Mt Katahdin/katahdin.zip` (20.74 MB). Metadata flags it as `isAssetBundle: false`. Probably belongs to the geobase sibling project, not us.
- **Duplicates across sibling-app containers.** `crystal-viewer` ≈ `geobase-crystallattice` ≈ `production-crystallattice` all hold the same 46-48 `.dae` mineral files. Probably intentional (different consumers, different access tiers).
- **GPv1 account type is legacy.** Microsoft has been recommending migration to GPv2 for years for tiering + cost reasons. Out of scope here; flag for the lab to consider separately.
- **49 GB total** — fits in vanilla Azure pricing tiers cheaply (~$1-2/mo at hot tier). No cost lever to pull during modernization.

## Operational metadata observed on blobs

The `android` / `ios` / `wsa` AssetBundles carry rich custom metadata fields on every blob:

- `author`
- `description`
- `isAssetBundle` (true)
- `latitude`, `longitude`
- `prefabName`
- `modelName`
- `planetaryBody` (Earth, Moon, Mars, etc. — observed on DEM and some outcrop bundles)
- `geoDescription` (longer free-text description)
- `mineralGroup` (on crystal/mineral bundles)

This metadata is set per-blob, not just at the container level, and is queryable via `?include=metadata` in the list-blob call — which is exactly what the existing `FetchSpatialMetadata.cs` is doing. The new RemoteConfig + any re-bake pipeline must preserve these fields.

## Proposed new / updated tickets (for discussion, not yet filed)

Pulled out so the existing 34-ticket set can stay coherent without thrashing:

- **Update #6** (re-bake AssetBundles): add the per-model-vs-per-category structural transformation. Bump size estimate from L → XL.
- **Update #25** (RemoteConfig): include `FeaturedModels.txt` in scope; document that the runtime uses only 5 of 23 containers; explicitly note sibling-app containers as out-of-scope.
- **New ticket (P2):** "Audit `restricted` container access posture; switch to private + SAS if true gating is intended." Belongs in Phase 4 alongside #24.
- **New ticket (P3, optional):** "Migrate storage account from GPv1 to GPv2 for tiering / cost." Out of modernization scope but worth noting.
