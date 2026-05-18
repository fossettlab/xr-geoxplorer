# xr-geoxplorer

Canonical unified HoloLens-2 + iOS/Android geoscience explorer.

## Status
**Archived; modernization in progress.** Real source activity through
September 2020; project-file regeneration October 2023 (no code changes);
modernization to Unity 2022.3 / Meta Quest 3 began April 2026. See the
modernization epic at
[issue #1](https://github.com/fossettlab/xr-geoxplorer/issues/1) and the
contractor handoff at [`HANDOFF.md`](HANDOFF.md).

## Build
Requires Unity **2019.4.8f1**. Uses MRTK 2.x, Photon PUN, Azure Spatial
Anchors SDK. For modern revival: Unity 2022.3 LTS or Unity 6 + MRTK 3
or OpenXR migration.

## Platform
Multi-platform: HoloLens 2 (UWP) + iOS + Android (via AR Foundation and
Azure Spatial Anchors for cross-device shared experiences).

## Related repos
- `xr-geoxplorer-mobile` — mobile-only head (App Store / Play Store source)
- `xr-geoxplorer-se` — shared-experience HoloLens variant
- `xr-geoxplorer-v1` — 2018 HoloLens-1 era archive (has original git history)
- `xr-geoxplorer-assets` — shared AssetBundle build pipeline (never
  pushed to GitHub; source lives on the Fossett Lab NAS at
  `/mnt/nas/dev/fossett_xr_apps/GeoXAssetBundles/`; see
  [`docs/azure-storage-inventory.md`](docs/azure-storage-inventory.md)
  for the Azure side of the asset story).

## Origin
Fossett Laboratory, Washington University in St. Louis. Imported from the
lab NAS on 2026-04-21.
