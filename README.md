# xr-geoxplorer

Canonical unified Meta Quest 3 + iOS/Android geoscience explorer.

## Status
**Modernization in progress.** Real source activity through
September 2020; project-file regeneration October 2023 (no code changes);
modernization to Unity 2022.3 / Meta Quest 3 began April 2026. See the
modernization epic at
[issue #1](https://github.com/fossettlab/xr-geoxplorer/issues/1) and the
contractor handoff at [`HANDOFF.md`](HANDOFF.md).

## Build
Requires Unity **2019.4.8f1**. Uses MRTK 2.x, Photon PUN, Azure Spatial
Anchors SDK. For modern revival: Unity 2022.3 LTS or Unity 6 + MRTK 3
or OpenXR migration.

The `legacy-2019.4` tag preserves the last known-buildable Unity 2019.4.8f1 state.

## Platform
Meta Quest 3 (primary) + iOS + Android. HoloLens 2 was dropped as a target on
2026-06-06 (EOL'd; lab units being sold). Cross-device shared experiences
originally used Azure Spatial Anchors, which Microsoft retired 2024-11-20; the
modern path is Meta spatial anchors (Quest) plus marker-based alignment for
phone↔headset co-location (see issue #17).

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
Fossett Laboratory, Washington University in St. Louis.
