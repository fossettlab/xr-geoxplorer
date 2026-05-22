# AssetBundle Re-Bake Pipeline

This document describes the Unity 2022.3 Built-in Render Pipeline AssetBundle
scaffold for issue #6.

## Goal

XR GeoXplorer loads remote AssetBundles from Azure Storage at runtime. The app
lists blobs under a platform container (`android`, `ios`, or `wsa`) with a
`geoxplorer-<category>` prefix, then downloads the selected blob by name.

The new bake must therefore produce one bundle per model:

```text
<platform>/geoxplorer-<category>/<modelName>-bundle
```

The legacy NAS pipeline is documented as producing one bundle per category,
which does not match the deployed Azure layout. The Unity 2022.3 pipeline in
this repo assigns per-model bundle names before running Unity's bundle build.

## Source Layout

The authoritative source content is expected from the NAS path documented in
`docs/azure-storage-inventory.md`:

```text
/mnt/nas/dev/fossett_xr_apps/GeoXAssetBundles/Assets/<Category>~/
```

The source categories are:

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

For privacy/access control, the NAS is not read by this repo automatically. Copy
or mount the source content into the Unity project or pass its Unity-visible
root explicitly when running the pipeline.

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
GeoXplorer > AssetBundles > Validate > Staging Output Against Manifest
GeoXplorer > AssetBundles > Write Azure Upload Plan
```

The assignment step scans each category folder for model assets with these
extensions:

```text
.prefab
.fbx
.obj
.dae
.blend
```

For each model asset, it assigns:

```csharp
AssetImporter.GetAtPath(modelPath).assetBundleName =
    $"geoxplorer-{category}/{modelName}-bundle";
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
  -geoXSourceRoot=Assets \
  -geoXOutputRoot=/path/to/staging/AssetBundles \
  -logFile /private/tmp/xr-geoxplorer-assetbundle-bake.log
```

Arguments:

```text
-geoXSourceRoot=<Unity-visible source root>
-geoXOutputRoot=<bundle output root>
-geoXFeaturedModels=<path to FeaturedModels.txt>
-geoXMetadataManifest=<path to docs/assetbundle-metadata-manifest.json>
```

Environment variable alternatives:

```text
GEOX_BUNDLE_SOURCE_ROOT
GEOX_BUNDLE_OUTPUT_ROOT
GEOX_FEATURED_MODELS
GEOX_METADATA_MANIFEST
```

## Local Validation

Before baking, run the source-layout validation after the NAS content has been
copied or mounted into the Unity project:

```bash
'/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity' \
  -batchmode \
  -quit \
  -projectPath '/Users/seanqin/Documents/Fossettlab' \
  -executeMethod GeoXAssetBundlePipeline.ValidateSourceLayout \
  -geoXSourceRoot=Assets \
  -logFile /private/tmp/xr-geoxplorer-assetbundle-source-layout.log
```

This fails if any required category is missing, any category has no model
assets, or two assets would produce the same per-model bundle name.

After baking, compare the local staging output to the committed Azure metadata
manifest before uploading:

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

This check compares bundle names for `android`, `ios`, and `wsa`. It does not
require Azure credentials and intentionally ignores Unity's local `.manifest`
files because those are build artifacts, not runtime blobs in the committed
Azure inventory.

## Featured Assembly

`geoxplorer-featured` is not a source category. It is assembled from selected
models after the nine source categories build.

The featured step reads `FeaturedModels.txt` and copies the referenced bundles
into:

```text
<platform>/geoxplorer-featured/<category>/<modelName>-bundle
```

Accepted reference formats in `FeaturedModels.txt`:

```text
geoxplorer-dem/apollo11-bundle
dem/apollo11
apollo11
```

The shortest form must resolve to exactly one source bundle for that platform.
If multiple bundles match the same model name, use the `category/modelName`
form.

The deployed Azure inventory shows `geoxplorer-featured` bundles for Android and
iOS, but none for WSA. The new assembly step can create WSA featured aliases
once source bundles exist.

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
that the source folders are complete, assign per-model bundle names, build local
bundles, assemble `geoxplorer-featured`, compare staging output against the
manifest, and generate an upload plan for a later Azure staging upload.
