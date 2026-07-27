# RemoteConfig (environment URL routing)

GeoXplorer loads Azure / Firebase / StraboSpot endpoints from a `RemoteConfig`
ScriptableObject instead of hardcoding URLs in C# scripts.

**This is environment routing, not authentication.** Values ship inside the
client build and can be extracted from an APK. API keys on this object are
friction only. Real gated access belongs to the SAS Function from issue #24.

## Assets

| Asset | Path | In git? |
|---|---|---|
| Dev | `Assets/Settings/Config/RemoteConfig.Dev.asset` | yes |
| Staging | `Assets/Settings/Config/RemoteConfig.Staging.asset` | yes |
| Prod (public template) | `Assets/Settings/Config/RemoteConfig.Prod.asset` | yes |
| Prod (local / CI secrets) | `Assets/Settings/Config/RemoteConfig.Prod.local.asset` | **no** — gitignored |
| Catalog (Resources) | `Assets/Resources/RemoteConfig/RemoteConfigCatalog.asset` | yes |

The catalog references Dev / Staging / Prod so player builds can
`Resources.Load` without scene wiring.

`.gitignore` pattern: `**/RemoteConfig.*.local.asset` (+ `.meta`).

## Fields

| Field | Purpose |
|---|---|
| `environmentName` | `dev` / `staging` / `prod` |
| `assetBundleBaseUrl` | Azure blob account root (no trailing slash), e.g. `https://haringerverdiag.blob.core.windows.net` |
| `thumbnailsBaseUrl` | Thumbnails container root |
| `featuredModelsConfigUrl` | Full URL to `thumbnails/FeaturedModels.txt` |
| `storageAccountName` | Account name used when metadata objects need it |
| `sasEndpointBaseUrl` | Azure Function base URL from #24 (empty until that lands) |
| `sasApiKey` | Function key friction field (empty / injected for prod) |
| `straboSpotSearchUrl` | StraboSpot datasets search endpoint |
| `firebaseAnchorsUrl` | Legacy Firebase anchors JSON endpoint |

Helpers on the asset:

- `BuildContainerListUrl(platform, prefix)`
- `BuildAssetBundleUri(platform, bundleName)`
- `BuildThumbnailUrl(relativePath)`

Runtime featured listing today still uses the Azure blob **list** API with
`prefix=featured` (see `MobileMenuManager.FetchFeatured`). The
`featuredModelsConfigUrl` field is reserved for the `FeaturedModels.txt`
config called out in `docs/azure-storage-inventory.md`.

## Selection

`RemoteConfigLoader` runs at `BeforeSceneLoad` and also from
`PlatformBootstrapper.Awake`.

| Scripting define | Asset used |
|---|---|
| `GEOX_PROD` | `RemoteConfig.Prod.local.asset` if present (Editor), else `RemoteConfig.Prod` |
| `GEOX_STAGING` | `RemoteConfig.Staging` |
| `GEOX_DEV` or none (Editor default) | `RemoteConfig.Dev` |

Set defines under **Edit → Project Settings → Player → Scripting Define Symbols**
per build target, or from CI.

## Public vs injected

**Safe to commit (public-read Azure / known endpoints):**

- asset bundle + thumbnail base URLs for the shared `haringerverdiag` account
- StraboSpot search URL
- Firebase anchors URL (legacy; replace when #24/#40 land)

**Inject at build / keep local:**

- `RemoteConfig.Prod.local.asset` is an **Editor-only** override: the loader reads
  it via `AssetDatabase` under `#if UNITY_EDITOR`, so it does **not** ship in a
  player/device build and dropping it into the project has no effect on a Quest
  build.
- To put prod-only values (e.g. the SAS key) into a **device build**, CI must
  rewrite the committed, catalog-referenced `RemoteConfig.Prod.asset` before
  building — that is the asset a player build actually loads. A build-time
  injector for this is a TODO and is not needed until the Azure SAS backend
  (#24) exists; today all three environments use the same public URLs.

## Adding a new endpoint

1. Add a `[SerializeField]` + public property on `RemoteConfig`.
2. Fill Dev / Staging / Prod assets (and Prod.local if secret).
3. Replace call sites with `RemoteConfig.Current.<Property>`.
4. Confirm `rg -n 'blob\.core\.windows\.net|firebaseio\.com|strabospot\.org' Assets/Scripts --glob '*.cs'`
   only hits nothing (values live in `.asset` YAML, not C#).

## Out of scope (issue #25)

- Hot reload / remote fetch of config
- Feature flags / A/B tests
- Non-URL gameplay settings
