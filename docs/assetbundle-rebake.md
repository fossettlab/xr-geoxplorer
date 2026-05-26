# AssetBundle Re-Bake Pipeline

This document describes the Unity 2022.3 Built-in Render Pipeline AssetBundle
scaffold for issue #6.

## Goal

XR GeoXplorer loads remote AssetBundles from Azure Storage at runtime. The app
lists blobs under a platform container (`android`, `ios`, or `wsa`) with a
`geoxplorer-<category>` prefix, then downloads the selected blob by name.

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

Current #6 scope is to build the 8 available raw-source categories first:
everything above except `bio`. The `bio` raw Unity source was not found in the
staged source drop, so the pipeline treats `bio` as a known raw-source gap. For a
strict production-equivalent staging set, reuse the existing pre-baked `bio`
bundles or locate the missing raw source separately.

After downloading the private `geoxplorer-source` drop locally, static coverage
against the committed manifest still shows gaps beyond `bio`. The available
source maps cleanly for `architecture`, `arthistory`, `drama`, and `outcrop`,
but not for every deployed `archeology`, `crystallattice`, `dem`, or
`handsample` bundle. Do not upload to staging until these gaps are resolved or
explicitly accepted as a partial initial bake.

For privacy/access control, this repo never reads Azure or NAS content
automatically. Copy or mount the source content into the Unity project, or pass a
Unity-visible root explicitly when running the pipeline.

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
GeoXplorer > AssetBundles > Build > Build WSA
GeoXplorer > AssetBundles > Build > Build Standalone
GeoXplorer > AssetBundles > Build > Build All Ticket #6 Targets
GeoXplorer > AssetBundles > Assemble Featured Bundles
GeoXplorer > AssetBundles > Validate > Source Layout
GeoXplorer > AssetBundles > Validate > Source Coverage Against Manifest
GeoXplorer > AssetBundles > Validate > Initial Bake Against Manifest
GeoXplorer > AssetBundles > Validate > Staging Output Against Manifest
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
  -executeMethod GeoXAssetBundlePipeline.BuildAllTicketTargets \
  -geoXSourceRoot=Assets/GeoXSource/geoxplorer-source \
  -geoXOutputRoot=/path/to/staging/AssetBundles \
  -logFile /private/tmp/xr-geoxplorer-assetbundle-bake.log
```

Arguments:

```text
-geoXSourceRoot=<Unity-visible source root>
-geoXOutputRoot=<bundle output root>
-geoXMetadataManifest=<path to docs/assetbundle-metadata-manifest.json>
```

Environment variable alternatives:

```text
GEOX_BUNDLE_SOURCE_ROOT
GEOX_BUNDLE_OUTPUT_ROOT
GEOX_METADATA_MANIFEST
```

## Local Validation

Before baking, run the source-layout validation after the staged Unity source
content has been copied or mounted into the Unity project:

```bash
'/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity' \
  -batchmode \
  -quit \
  -projectPath '/Users/seanqin/Documents/Fossettlab' \
  -executeMethod GeoXAssetBundlePipeline.ValidateSourceLayout \
  -geoXSourceRoot=Assets/GeoXSource/geoxplorer-source \
  -logFile /private/tmp/xr-geoxplorer-assetbundle-source-layout.log
```

This fails if any required available-source category is missing, any required
category has no model assets, or two assets would produce the same per-model
bundle name. Missing or empty `bio` is reported as a warning because Bradley's
2026-05-25 update confirmed that only pre-baked `bio` bundles survive for now.

Then run manifest source-coverage validation before any build. This catches
missing source for deployed bundle names before spending time baking:

```bash
'/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity' \
  -batchmode \
  -quit \
  -projectPath '/Users/seanqin/Documents/Fossettlab' \
  -executeMethod GeoXAssetBundlePipeline.ValidateSourceCoverageAgainstManifest \
  -geoXSourceRoot=Assets/GeoXSource/geoxplorer-source \
  -geoXMetadataManifest=docs/assetbundle-metadata-manifest.json \
  -logFile /private/tmp/xr-geoxplorer-assetbundle-source-coverage.log
```

Static local coverage from the downloaded source drop:

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

These numbers mean a full manifest-equivalent bake is not ready yet. The tool is
still useful: it can bake matched source entries and fail fast with explicit
missing bundle names until the remaining source is provided or the acceptance
scope is narrowed.

After baking and assembling featured aliases, compare the local staging output to
the committed Azure metadata manifest before uploading. For the initial #6 bake
that intentionally excludes raw-source-missing `bio`, use:

```bash
'/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity' \
  -batchmode \
  -quit \
  -projectPath '/Users/seanqin/Documents/Fossettlab' \
  -executeMethod GeoXAssetBundlePipeline.ValidateInitialBakeOutputAgainstManifest \
  -geoXOutputRoot=/path/to/staging/AssetBundles \
  -geoXMetadataManifest=docs/assetbundle-metadata-manifest.json \
  -logFile /private/tmp/xr-geoxplorer-assetbundle-initial-manifest-check.log
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

These checks compare bundle names for `android`, `ios`, and `wsa`. They do not
require Azure credentials and intentionally ignore Unity's local `.manifest`
files because those are build artifacts, not runtime blobs in the committed
Azure inventory.

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
   each local bundle path, target blob name, content type, and custom metadata
   from the committed manifest.
4. Upload to a staging Azure container or staging storage account.
5. Apply custom blob metadata from the upload plan during upload.
6. Smoke-test one representative bundle from each category on Quest 3 first,
   then Android mobile and HoloLens 2 if available.

Materials may render magenta at this stage because URP-compatible bundles are
tracked separately in #39. For #6, the load test is about download/load success
without exceptions.

## Current Limits

This repo-side scaffold does not read NAS content, query Azure, upload to Azure,
or run hardware smoke tests by itself. Those steps require explicit access to
external storage, credentials, and devices. What it can do locally is validate
the 8 available source categories, assign per-model bundle names, build local
bundles, assemble `geoxplorer-featured` from the committed manifest, compare
staging output against the manifest with either initial-bake or strict rules, and
generate an upload plan for a later Azure staging upload.
