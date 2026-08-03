# Restricted container audit (#37 Phase A)

**Status:** Phase A complete (2026-08-02). Phase B (privatize container + wire SAS
downloads) waits on project-lead sign-off and live Function provisioning.

## Question

Does the runtime treat `haringerverdiag/restricted` differently from public platform
containers (`android`, `ios`, `wsa`)?

## Finding: restricted bundles are orphaned in current code

A full-repo search for `restricted`, `eastshorestructure`, and `yayamari` in app
scripts, scenes, and prefabs finds **no runtime references**.

| Search target | Result |
|---|---|
| `Assets/Scripts/**/*.cs` | Only `RestrictedBundleSasClient` (#24 scaffold) and vendor comments |
| `Assets/Scenes/**` | No matches |
| Menu / download flows | List `geoxplorer-*` prefixes under **platform** containers only |

[`FetchAssetBundle.cs`](../Assets/Scripts/FetchAssetBundle.cs) builds download URLs via
`RemoteConfig.BuildAssetBundleUri(platform, bundleName)` — always
`https://…/android/<bundle>` or `…/ios/<bundle>`. The `containerName` field passed
through Photon instantiation is **ignored for the URL** (see line 74 comment).

Therefore the six blobs in the public `restricted` container are **not loaded by the
modernization codebase**. They were likely used by an older gated-scene flow that did
not survive the Quest-first refactor.

## Deployed blobs (reference)

| Blob | Size (approx) |
|---|---|
| `android/eastshorestructure-bundle` | 25 MB |
| `android/yayamari-bundle` | 21 MB |
| `ios/eastshorestructure-bundle` | 28 MB |
| `ios/yayamari-bundle` | 30 MB |
| `x86/eastshorestructure-bundle` | 25 MB |
| `x86/yayamari-bundle` | 24 MB |

Blob metadata includes `"restricted": "true"`. Platforms are `android`, `ios`, and
`x86` (legacy UWP) — **no `wsa/` copies**. HoloLens is dropped; Quest uses `android/`.

## Phase A conclusion

- There is **no app-side permission check** today — because the app does not fetch
  these bundles at all.
- Storage-level public access on a container named `restricted` is still a **real
  security finding** (anonymous download of ~153 MB of lab content).
- **Recommend Phase B:** privatize the container and keep SAS infrastructure ready
  for when/if gated scenes return. No Quest load test is required until a product
  decision revives these scenes.

## Phase B prep (already on branch)

| Component | Location |
|---|---|
| SAS Function allowlist | [`functions/sas_auth.py`](../functions/sas_auth.py) |
| Unity SAS client | [`RestrictedBundleSasClient.cs`](../Assets/Scripts/Config/RestrictedBundleSasClient.cs) |
| Local smoke script | [`scripts/test_sas_function.sh`](../scripts/test_sas_function.sh) |

When scenes are revived, `FetchAssetBundle` (or a successor) should:

1. Detect restricted bundle paths (allowlist keys like `android/eastshorestructure-bundle`).
2. Call `RestrictedBundleSasClient.RequestSasUrl`.
3. `UnityWebRequestAssetBundle.GetAssetBundle(signedUrl)`.

## wsa / HoloLens note

Do **not** re-bake `wsa/` restricted bundles unless product direction changes.
Issue #6 / #39 scope is Quest (`android`) + mobile (`ios`) only.

## Related

- [#37](https://github.com/fossettlab/xr-geoxplorer/issues/37)
- [`docs/azure-storage-inventory.md`](azure-storage-inventory.md)
- [`docs/auth-backend.md`](auth-backend.md)
