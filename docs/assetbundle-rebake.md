# AssetBundle Re-Bake Pipeline

This document describes the Unity 2022.3 Built-in Render Pipeline AssetBundle
scaffold for issue #6.

## Goal

XR GeoXplorer loads remote AssetBundles from Azure Storage at runtime. The
current Quest/mobile path lists blobs under the `android` and `ios` platform
containers with a `geoxplorer-<category>` prefix, then downloads the selected
blob by name. The manifest still records the historical `wsa` container for
inventory comparison, but HoloLens/WSA is no longer a #6 build target.

The new bake must therefore produce one bundle per deployed blob:

```text
<platform>/geoxplorer-<category>/<modelName>-bundle
```

The legacy NAS pipeline is documented as producing one bundle per category,
which does not match the deployed Azure layout. The Unity 2022.3 pipeline in
this repo resolves bundle names from `docs/assetbundle-metadata-manifest.json`
and the preserved source `.meta` files before running Unity's bundle build.

## Source Layout

The authoritative Unity source content is staged outside the repo in the private
`geoxplorer-source` Azure container. It contains the available Unity source
categories with `.meta` import settings preserved. Do not commit the SAS token or
copied source assets.

The historical NAS path documented in `docs/azure-storage-inventory.md` was:

```text
/mnt/nas/dev/fossett_xr_apps/GeoXAssetBundles/Assets/<Category>~/
```

The source categories for a full production match are:

```text
archeology
architecture
arthistory
bio
crystallattice
dem
drama
handsample
outcrop
```

Current #6 scope is to build what has raw Unity source, then keep the missing
raw-source cases explicit. The `bio` raw Unity source was not found in the
staged source drop, so this PR skips `bio` instead of blocking the first bake.
Bradley's 2026-06-14 direction also says not to block on missing DEM source if
the already-deployed DEM bundles still load in Unity 2022.3.

The pipeline therefore has two validation modes:

- **Strict manifest mode**: production-equivalent; every deployed manifest entry
  must have source and matching output.
- **Available-source mode**: #6 initial bake; only manifest entries that resolve
  to staged source are expected, while deployed entries without source are
  counted and reported.

For privacy/access control, this repo never reads Azure or NAS content
automatically. Copy or mount the source content into the Unity project, or pass a
Unity-visible root explicitly when running the pipeline.

The Azure source drop preserves the historical category folder names ending in
`~` (`Archeology~`, `Outcrops~`, and so on). That layout is useful for keeping
raw source out of normal project imports, but Unity does not import assets under
`~` folders as regular `AssetImporter` targets. For an actual bake, make a local
ignored importable mirror under `Assets/GeoXSource/` with category folders that
do not end in `~` (for example `archeology`, `arthistory`, `handsamples`, and
`outcrops`) and pass that mirror as `-geoXSourceRoot`.

## Editor Tool

The pipeline entry point is:

```text
Assets/Editor/GeoXAssetBundlePipeline.cs
```

Unity menu commands:

```text
GeoXplorer > AssetBundles > Assign Per-Model Bundle Names
GeoXplorer > AssetBundles > Build > Build Active Target
GeoXplorer > AssetBundles > Build > Build Android
GeoXplorer > AssetBundles > Build > Build iOS
GeoXplorer > AssetBundles > Build > Build Historical WSA
GeoXplorer > AssetBundles > Build > Build Standalone
GeoXplorer > AssetBundles > Build > Build Available Android
GeoXplorer > AssetBundles > Build > Build Available iOS
GeoXplorer > AssetBundles > Build > Build All Ticket #6 Targets
GeoXplorer > AssetBundles > Build > Build Available Ticket #6 Targets
GeoXplorer > AssetBundles > Assemble Featured Bundles
GeoXplorer > AssetBundles > Validate > Source Layout
GeoXplorer > AssetBundles > Validate > Source Coverage Against Manifest
GeoXplorer > AssetBundles > Validate > Available Source Against Manifest
GeoXplorer > AssetBundles > Validate > Initial Bake Against Manifest
GeoXplorer > AssetBundles > Validate > Staging Output Against Manifest
GeoXplorer > AssetBundles > Validate > Available Source Output Against Manifest
GeoXplorer > AssetBundles > Validate > Load Bundle From File
GeoXplorer > AssetBundles > Write Azure Upload Plan
```

The assignment step resolves each target platform from the metadata manifest
first. It matches manifest blob names against source assets using:

1. preserved `.meta` `assetBundleName` values;
2. manifest `prefabName`;
3. manifest blob basename;
4. a small typo-tolerant fallback for known source/manifest spelling drift.

The pipeline intentionally does **not** match on manifest `modelName`: that
field is display text and can collapse different deployed bundles onto the same
source candidate, such as regular hand samples and UND hand samples.

It then clears stale bundle names under the source root and assigns the matched
entry asset to the manifest blob path, for example:

```csharp
AssetImporter.GetAtPath(sourceAsset).assetBundleName =
    "geoxplorer-outcrop/marinheadlands-bundle";
```

The source scan considers model entry assets with these extensions:

```text
.prefab
.fbx
.obj
.dae
.blend
```

Then the build commands call:

```csharp
BuildPipeline.BuildAssetBundles(
    outputPath,
    BuildAssetBundleOptions.StrictMode,
    buildTarget);
```

## Batch Mode

The script can also run from Unity batch mode. Example:

```bash
'/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity' \
  -batchmode \
  -quit \
  -projectPath '/Users/seanqin/Documents/Fossettlab' \
  -executeMethod GeoXAssetBundlePipeline.BuildAvailableTicketTargets \
  -geoXSourceRoot=Assets/GeoXSource/importable-source \
  -geoXOutputRoot=/path/to/staging/AssetBundles \
  -logFile /private/tmp/xr-geoxplorer-assetbundle-bake.log
```

Arguments:

```text
-geoXSourceRoot=<Unity-visible source root>
-geoXOutputRoot=<bundle output root>
-geoXMetadataManifest=<path to docs/assetbundle-metadata-manifest.json>
-geoXAllowPartialSource=true
-geoXBundlePath=<local bundle file to load-smoke-test>
```

Environment variable alternatives:

```text
GEOX_BUNDLE_SOURCE_ROOT
GEOX_BUNDLE_OUTPUT_ROOT
GEOX_METADATA_MANIFEST
GEOX_ALLOW_PARTIAL_SOURCE
```

By default, `Build Android`, `Build iOS`, and `Build All Ticket #6 Targets` are
strict: if the source root cannot satisfy every required bundle in the committed
manifest, the build stops before writing partial output. Use the `Build
Available ...` methods for the current #6 staged-source workflow. Those methods
log missing deployed entries as warnings, but duplicate or ambiguous source
matches still fail the build. The older `-geoXAllowPartialSource=true` argument
is still supported for batch compatibility.

## Local Validation

Before baking, run the source-layout validation after the staged Unity source
content has been copied or mounted into the Unity project:

```bash
'/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity' \
  -batchmode \
  -quit \
  -projectPath '/Users/seanqin/Documents/Fossettlab' \
  -executeMethod GeoXAssetBundlePipeline.ValidateSourceLayout \
  -geoXSourceRoot=Assets/GeoXSource/importable-source \
  -logFile /private/tmp/xr-geoxplorer-assetbundle-source-layout.log
```

This fails if any required available-source category is missing, any required
category has no model assets, or two equal-preference source assets would
produce the same per-model bundle name. Prefabs are preferred over backing mesh
files for duplicate detection. Missing or empty `bio` is reported as a warning
because Bradley's 2026-05-25 update confirmed that only pre-baked `bio` bundles
survive for now.

Then run available-source coverage validation before the initial #6 bake. This
checks that source-backed entries resolve cleanly to manifest blob names and
reports, but does not fail on, deployed blobs with no staged source:

```bash
'/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity' \
  -batchmode \
  -quit \
  -projectPath '/Users/seanqin/Documents/Fossettlab' \
  -executeMethod GeoXAssetBundlePipeline.ValidateAvailableSourceAgainstManifest \
  -geoXSourceRoot=Assets/GeoXSource/importable-source \
  -geoXMetadataManifest=docs/assetbundle-metadata-manifest.json \
  -logFile /private/tmp/xr-geoxplorer-assetbundle-available-source-coverage.log
```

Use `ValidateSourceCoverageAgainstManifest` only when you intentionally want the
strict production-equivalent gate.

Static local coverage from the downloaded source drop. Android and iOS are the
current ticket targets; WSA is shown only as historical deployed inventory.
These are manifest matches, not raw file counts:

| Category | Android | iOS | WSA | Notes |
|---|---:|---:|---:|---|
| `architecture` | 1 / 1 | 1 / 1 | 1 / 1 | source maps cleanly |
| `arthistory` | 14 / 14 | 14 / 14 | 14 / 16 | WSA has two extra deployed entries not mapped from source |
| `drama` | 8 / 8 | 8 / 8 | 7 / 11 | WSA includes four extra deployed entries not mapped from `Drama~` |
| `outcrop` | 40 / 40 | 40 / 40 | 40 / 40 | source maps cleanly, including spelling drift such as `Harland` / `Hartland` |
| `archeology` | 2 / 6 | 2 / 6 | 2 / 6 | only `Cromeleque` and `SkaraBrae` map locally |
| `crystallattice` | 10 / 69 | 10 / 68 | 10 / 68 | source drop has 11 prefab roots, not the deployed mineral catalog |
| `dem` | 5 / 702 | 5 / 700 | 5 / 700 | source drop has a small set of DEM prefabs, not the deployed DTEEC catalog |
| `handsample` | 30 / 53 | 30 / 53 | 30 / 53 | UND samples are still not represented as individual source prefabs |
| `bio` | 0 / 17 | 0 / 17 | 0 / 17 | known raw-source gap |

Per-category source counts from Bradley's private `geoxplorer-source` drop are:

| Source folder | Model-like files | Prefabs | Notes |
|---|---:|---:|---|
| `Archeology~` | 4 | 2 | imported as `archeology` |
| `Architecture~` | 2 | 1 | imported as `architecture` |
| `ArtHistory~` | 28 | 14 | imported as `arthistory` |
| `CrystalLattice~` | 22 | 11 | imported as `crystallattice` |
| `DEM~` | 10 | 5 | imported as `dem`; deployed DEM bundles should be reused if they load |
| `Drama~` | 18 | 8 | imported as `drama` |
| `HandSamples~` | 60 | 30 | imported as `handsamples` |
| `Outcrops~` | 84 | 42 | imported as `outcrops` |

The raw source folders do end in `~`, so Bradley's concern about Unity hiding
those folders is real. The ignored importable mirror removes the trailing `~`
and preserves the same counts, so the remaining missing deployed entries are not
caused by mirror loss.

To bake only the source entries that are currently available, run the explicit
available-source build method:

```bash
'/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity' \
  -batchmode \
  -quit \
  -projectPath '/Users/seanqin/Documents/Fossettlab' \
  -executeMethod GeoXAssetBundlePipeline.BuildAvailableAndroid \
  -geoXSourceRoot=Assets/GeoXSource/importable-source \
  -geoXOutputRoot=/path/to/staging/AssetBundles \
  -logFile /private/tmp/xr-geoxplorer-assetbundle-android-available-bake.log
```

Do not treat an available-source bake as production-equivalent. It is useful for
validating the fresh Unity 2022 pipeline and staging representative bundles. The
strict manifest comparison should remain the acceptance gate for a later complete
cutover.

After baking and assembling featured aliases, compare the local staging output to
the source-backed subset of the committed Azure metadata manifest before
uploading. The validator checks the active build target when Unity is opened or
started with `-buildTarget Android` / `-buildTarget iOS`; otherwise it checks
the ticket's Android and iOS targets:

```bash
'/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity' \
  -batchmode \
  -quit \
  -projectPath '/Users/seanqin/Documents/Fossettlab' \
  -executeMethod GeoXAssetBundlePipeline.ValidateAvailableSourceOutputAgainstManifest \
  -geoXSourceRoot=Assets/GeoXSource/importable-source \
  -geoXOutputRoot=/path/to/staging/AssetBundles \
  -geoXMetadataManifest=docs/assetbundle-metadata-manifest.json \
  -logFile /private/tmp/xr-geoxplorer-assetbundle-available-output-check.log
```

For a strict production-equivalent staging output, including pre-baked `bio`
bundles, use:

```bash
'/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity' \
  -batchmode \
  -quit \
  -projectPath '/Users/seanqin/Documents/Fossettlab' \
  -executeMethod GeoXAssetBundlePipeline.ValidateStagingOutputAgainstManifest \
  -geoXOutputRoot=/path/to/staging/AssetBundles \
  -geoXMetadataManifest=docs/assetbundle-metadata-manifest.json \
  -logFile /private/tmp/xr-geoxplorer-assetbundle-manifest-check.log
```

The default validation checks compare bundle names for the current #6 ticket
targets, `android` and `ios`. WSA can still be inspected manually from the
historical manifest data, but it is not included in `Build All Ticket #6
Targets`. These checks do not require Azure credentials and intentionally ignore
Unity's local `.manifest` files because those are build artifacts, not runtime
blobs in the committed Azure inventory.

## Featured Assembly

`geoxplorer-featured` is not a source category. It is assembled from selected
models after the source categories build.

The featured step is driven from `docs/assetbundle-metadata-manifest.json`, not
from `FeaturedModels.txt`, because the checked-in text file may drift from what
is actually deployed. For each manifest entry like:

```text
geoxplorer-featured/<category>/<modelName>-bundle
```

the pipeline copies:

```text
<platform>/geoxplorer-<category>/<modelName>-bundle
```

to:

```text
<platform>/geoxplorer-featured/<category>/<modelName>-bundle
```

The deployed Azure inventory shows `geoxplorer-featured` bundles for Android and
iOS, but none for WSA, so manifest-driven assembly creates Android/iOS featured
aliases unless the manifest changes.

## Metadata Manifest

The acceptance gate compares staging output against deployed Azure metadata.
The committed manifest is:

```text
docs/assetbundle-metadata-manifest.json
```

It was generated from:

```text
docs/azure-haringerverdiag-inventory.json
```

The manifest includes every deployed `geoxplorer-*` blob from the `android`,
`ios`, and `wsa` containers with:

```text
name
size
contentType
metadata
```

The metadata object preserves every observed custom blob metadata field, such as
`author`, `description`, `isAssetBundle`, `latitude`, `longitude`, `prefabName`,
`modelName`, `planetaryBody`, `geoDescription`, and `mineralGroup`.

Regenerate the manifest from a refreshed inventory with:

```bash
ruby -rjson -e '
source = "docs/azure-haringerverdiag-inventory.json"
data = JSON.parse(File.read(source))
platforms = {}
%w[android ios wsa].each do |platform|
  platforms[platform] = data.fetch(platform)
    .select { |blob| blob.fetch("name", "").start_with?("geoxplorer-") }
    .sort_by { |blob| blob.fetch("name") }
    .map do |blob|
      {
        "name" => blob.fetch("name"),
        "size" => blob.fetch("size"),
        "contentType" => blob.fetch("contentType"),
        "metadata" => (blob["meta"] || {}).sort.to_h
      }
    end
end
manifest = {
  "schemaVersion" => 1,
  "sourceInventory" => source,
  "storageAccount" => "haringerverdiag.blob.core.windows.net",
  "containers" => platforms,
  "metadataFieldsObserved" =>
    platforms.values.flatten.flat_map { |blob| blob["metadata"].keys }.uniq.sort
}
File.write("docs/assetbundle-metadata-manifest.json",
  JSON.pretty_generate(manifest) + "\n")
'
```

## Upload Procedure

Do not upload to production for this ticket. Production cutover requires separate
project-lead approval.

Recommended staging procedure:

1. Build bundles to a local staging output root.
2. Compare the staging blob paths against `docs/assetbundle-metadata-manifest.json`.
3. Generate an upload plan with `GeoXplorer > AssetBundles > Write Azure Upload Plan`.
   The plan is written to `<outputRoot>/azure-upload-plan.json` and contains
   each local bundle path, platform, production-shaped target container/blob
   name, single-container staging blob name, content type, and custom metadata
   from the committed manifest.
4. Upload to a staging Azure container or staging storage account.
5. Apply custom blob metadata from the upload plan during upload.
6. Smoke-test one representative bundle from each category on Quest 3 first,
   then Android/iOS mobile if available.

If staging uses Bradley's single `staging-assetbundles` container, upload each
bundle to the plan's `stagingBlobName` so platform folders stay distinct, for
example `android/geoxplorer-outcrop/marinheadlands-bundle`. If staging mirrors
production's three-container shape, use `targetContainer` plus `targetBlobName`.

Materials may render magenta at this stage because URP-compatible bundles are
tracked separately in #39. For #6, the load test is about download/load success
without exceptions.

## Existing DEM Bundle Smoke Test

Bradley asked whether an already-deployed DEM bundle still loads in Unity
2022.3. If it does, missing DEM raw source should not block #6 because the live
DEM bundles can continue to serve runtime content.

Download one public deployed DEM bundle to a temporary local path, then run:

```bash
'/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity' \
  -batchmode \
  -quit \
  -buildTarget Android \
  -projectPath '/Users/seanqin/Documents/Fossettlab' \
  -executeMethod GeoXAssetBundlePipeline.ValidateBundleLoadFromFile \
  -geoXBundlePath=/private/tmp/xr-geoxplorer-dem-smoke/apollo11-bundle \
  -logFile /private/tmp/xr-geoxplorer-dem-bundle-smoke.log
```

The verifier fails if the bundle cannot be loaded or if it contains no assets or
scenes.

2026-06-14 result: the public deployed Android bundle
`android/geoxplorer-dem/apollo11-bundle` loaded successfully in Unity 2022.3
with 281 assets and 0 scenes. That supports Bradley's plan to keep using
existing deployed DEM bundles where raw DEM source is missing.

## 2026-06-14 Validation Status

The private Azure `geoxplorer-source` SAS was used locally to download the
source drop, then mirrored under an ignored Unity-visible source root without
trailing `~` category folder names. The source-layout validation passed against
that mirror and found 228 model assets across the available categories, with
`bio` still treated as the known raw-source gap.

The branch is rebased on top of the ASA editor guard from PR #86. That guard
keeps the Android ASA bridge out of Unity Editor compilation with:

```csharp
#if UNITY_ANDROID && !UNITY_EDITOR
```

Strict manifest coverage still fails against the downloaded source, which is
expected because the source drop does not contain every deployed `archeology`,
`crystallattice`, `dem`, `handsample`, or `bio` entry. The available-source
validation and build paths now make that a reported scope limit instead of a
pipeline blocker.

Android available-source validation and bake results on 2026-06-14:

- Source-backed coverage: 110 Android bundles matched source, 783 deployed
  Android bundles have no staged source, and 17 `bio` bundles are the known
  optional raw-source gap.
- Android bake: Unity built 111 Android bundle artifacts to local staging.
- Featured assembly: 6 source-backed `geoxplorer-featured` aliases were created;
  featured aliases whose backing bundles lack source were skipped with warnings.
- Android output validation: 116 source-backed Android manifest bundle names
  matched local staging output.
- Upload plan: `Write Azure Upload Plan` generated a local
  `azure-upload-plan.json` for the staged Android output. No Azure upload was
  performed.

iOS source assignment reached the same available-source count, but the local
Unity install could not build iOS AssetBundles because the `iOSSupport` module is
not installed. The installed playback engine list only includes `AndroidPlayer`.
Run `Build Available iOS` on a Unity 2022.3 install with iOS Build Support to
complete the iOS bake.

No bundles should be uploaded to Azure staging until the available-source output
validation passes locally and Sean/Bradley explicitly approve the staging
upload.

## Current Limits

This repo-side scaffold does not read NAS content, query Azure, upload to Azure,
or run hardware smoke tests by itself. Those steps require explicit access to
external storage, credentials, and devices. What it can do locally is validate
the available source categories, assign per-model bundle names, build local
bundles, assemble `geoxplorer-featured` from the committed manifest, compare
staging output against either the available-source subset or the strict full
manifest, load-smoke-test an existing bundle file, and generate an upload plan
for a later Azure staging upload.
