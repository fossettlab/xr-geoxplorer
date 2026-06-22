# OpenXR Migration Notes

Issue #7 migrates GeoXplorer from legacy `UnityEngine.XR.WSA` app code paths to XR Plug-in Management plus OpenXR.

This PR is **Quest 3 / Android OpenXR only**. HoloLens/UWP support was intentionally dropped after the lab retired HoloLens 2 as a target ([#73](https://github.com/fossettlab/xr-geoxplorer/pull/73)).

## Packages added

- `com.unity.xr.openxr` — OpenXR loader and Meta Quest feature group
- `com.unity.xr.management` — XR Plug-in Management
- `com.unity.xr.hands` — hand tracking support used by OpenXR interaction profiles
- `com.unity.inputsystem` — required by modern XR/MRTK input wiring on Unity 2022.3

## Unity configuration

1. Open the project in **Unity 2022.3** with **Android build support** installed.
2. Run: `GeoXplorer > XR > Configure OpenXR Migration`

The menu command:

- Sets Android player settings for Quest (ARM64, IL2CPP, min SDK 24, linear color space)
- Assigns the OpenXR loader to the **Android** build target
- Enables Meta Quest OpenXR features: Quest support, Oculus/Meta touch controllers, hand tracking, and Meta Hand Tracking Aim

Re-run the command after installing Android build support if OpenXR settings were not serialized on a machine without that module.

## Build target

| Platform | Unity build target | XR runtime |
|----------|-------------------|------------|
| Quest 3  | Android (ARM64)   | OpenXR via Meta Quest feature group |
| iOS AR   | iOS               | AR Foundation (unchanged) |
| Android phone AR | Android   | AR Foundation (unchanged) |

There is **no UWP / HoloLens** build configuration in this migration.

## Manual verification still required

- Quest 3 device: app launches, `PlatformBootstrapper` selects the Quest prefab, OpenXR session starts
- Hand menu and model manipulation on device (may need follow-up input migration work in #8)
- Android AR mobile path still builds and runs
- Firebase anchor load/save behavior from #72

## Related issues

- #7 — OpenXR plugin migration (this PR, Quest-only scope)
- #8 — Input System migration (separate)
- #17 — Anchor/co-location rework (marker-based, ASA retired)
