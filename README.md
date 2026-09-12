# xr-geoxplorer

A geospatial and scientific spatial framework for XR. Quest first; iOS follows.

## Status
**Modernization in progress.** Real source activity through
September 2020; project-file regeneration October 2023 (no code changes);
modernization to Meta Quest 3 began April 2026. **`main` migrated to Unity 6**
(**6000.4.4f1**, August 2026 — see PR #162). See the modernization epic at
[issue #1](https://github.com/fossettlab/xr-geoxplorer/issues/1) and the
contractor handoff at [`HANDOFF.md`](HANDOFF.md).

**Product direction:** GeoXplorer owns scientific scene meaning: reference
systems, registration, layers, source identity, provenance and uncertainty where
known. Renderers and mesh/point/tile/splat representations are interchangeable
through qualified adapters. A saved scene should preserve its spatial meaning
across Unity/OpenXR, a future Three.js/WebXR backend, or another conforming runtime.
Earth, Moon, Mars and local/unknown frames are explicit. Easy opening remains a
core requirement; Azure catalogs and trusted bundles remain optional sources.

**Implementation:** the local Cartesian `GeoX.Spatial` core and invariant tests
exist. The portable interchange contract, qualified geodesy, registration tools,
runtime GLB and additional renderers are still implementation work. Retain URP,
MRTK3/XRI, collaboration, voice, anchors and passthrough. Start with the current
Quest baseline, strengthen the scientific core, then expand adapters. The dataset
dominates a quiet contextual spatial interface.

Read the [short pitch deck](docs/geoxplorer-pitch-deck.md),
[framework review](docs/framework-direction-review-2026-09-07.md),
[architecture](docs/open-3d-plan.md), [UI principles](docs/ui-design-principles.md),
and [execution plan](docs/beads-execution-plan.md).

## Build

Requires Unity **6000.4.4f1** (see [`ProjectSettings/ProjectVersion.txt`](ProjectSettings/ProjectVersion.txt)
and [`CONTRIBUTING.md`](CONTRIBUTING.md)). The active target is Meta Quest 3 on OpenXR.
MRTK 2.x, Photon PUN, and Azure Spatial Anchors SDK remain in the tree during
modernization (MRTK 3 migration is planned — #14).

**Agent / MCP workflow:** launch the Editor in automated mode — see [`AGENTS.md`](AGENTS.md).

**Quest 3 on-device:** follow [`docs/quest-session-1-runbook.md`](docs/quest-session-1-runbook.md)
(first light + perf baseline) and [`docs/quest3-build-and-deploy.md`](docs/quest3-build-and-deploy.md).

**Headless compile / Android build** (Mac or Linux with Unity Hub installed):

```bash
chmod +x scripts/unity.sh
./scripts/unity.sh compile
./scripts/unity.sh build-android
```

The `legacy-2019.4` tag preserves the last known-buildable Unity 2019.4.8f1 state.

## Platform
Meta Quest 3 first; iOS follows the core Quest experience. Existing Android
mobile code remains during modernization. HoloLens 2 was dropped as a target on
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
