# Scene architecture

Issue #3 replaces the old manual `GeoXShared.unity` scene-swap workflow with one checked-in scene and per-platform prefab variants.

## Current scene inventory

`Assets/Scenes/GeoXShared.unity` is already the only enabled build scene in `ProjectSettings/EditorBuildSettings.asset`.

The mobile reference scene is preserved under `Assets/Scenes/_legacy/` for comparison during the Quest 3 modernization:

- `MobileMRTK.unity`

It is reference-only and should not be edited as an active development scene.

## Root object comparison

### GeoXShared

- `GameUI`
  - `LobbyUI`
  - `RoomUI`
  - `InAppUI`
  - `MenuUI`
  - `InspectionModel - HL2`
- `Voice`
- `Directional Light`
- `Directional Light (1)`
- `MixedRealityToolkit`
  - `DefaultRaycastProvider`
  - `FocusProvider`
  - `HandJointService`
  - `InputPlaybackService`
  - `InputRecordingService`
  - `InputSimulationService`
  - `MixedRealityCameraSystem`
  - `MixedRealityInputSystem`
  - `UnityJoystickManager`
  - `UnityTouchDeviceManager`
  - `WindowsDictationInputProvider`
  - `WindowsMixedRealityDeviceManager`
  - `WindowsMixedRealityEyeGazeDataProvider`
  - `WindowsSpeechInputProvider`
- `MixedRealityPlayspace`
  - `Main Camera`
- `Plane`

### MobileMRTK reference scene

- Top-level roots are `SharedPlayground`, `MixedRealityToolkit`, `MixedRealityPlayspace`, `Voice`, and `Directional Light`.
- Mobile-specific UI is organized around canvas roots such as `LobbyCanvas`, `RoomCanvas`, `InAppCanvas`, `TutorialCanvas`, and `LoaderCanvas`.
- Mobile keeps `SharedPlayground` with `TableAnchor` as a root subtree instead of a HoloLens-style `Plane` root.
- The MRTK service tree includes mobile/AR-facing systems such as `MixedRealityBoundarySystem`, `MixedRealityDiagnosticsSystem`, `MixedRealitySpatialAwarenessSystem`, and `MixedRealityTeleportSystem`.

## Target architecture

`Assets/Scenes/GeoXShared.unity` remains the single checked-in runtime scene.

Platform-specific objects move behind a prefab tree:

- `Assets/Prefabs/PlatformRoot/PlatformRoot.prefab`
- `Assets/Prefabs/PlatformRoot/PlatformRoot.Quest3.prefab`
- `Assets/Prefabs/PlatformRoot/PlatformRoot.Mobile.prefab`

The variant prefabs currently exist as the migration scaffold. They copy the authored scene roots from the reference scenes, but intentionally omit the generated MRTK service roots such as `MixedRealityInputSystem` and `DefaultRaycastProvider`. Those generated MRTK objects register services in edit mode when serialized inside prefab assets, which dirties `GeoXShared.unity` before Play. The rebuild helper also disables nested Photon `ApplyDontDestroyOnLoad` flags so generated prefabs do not warn when instantiated under `PlatformBootstrapper`. Later OpenXR/MRTK3 tickets own the final runtime rig.

The next Unity Editor pass should move the appropriate non-MRTK root subtrees from the reference scenes into those variants:

- Quest 3: start from the mobile/shared AR path, then replace runtime/input/anchor pieces in later OpenXR/MRTK3 tickets.
- Mobile: preserve the existing mobile AR canvas and `SharedPlayground` path.

Use `GeoXplorer > Scene Architecture > Rebuild PlatformRoot Prefabs` in the Unity Editor to rebuild the variants from the legacy reference scenes. The helper starts Quest 3 from the mobile AR hierarchy, which is the closest existing Quest-to-be baseline before the OpenXR/MRTK3 tickets land.

## Bootstrap logic

`Assets/Scripts/PlatformBootstrapper.cs` is attached to a root object in `GeoXShared.unity`.

At runtime it resolves:

- `Quest3` for Android devices whose model/runtime identifies as Quest/Oculus/Meta.
- `Mobile` for Android and iOS fallback paths, and for Editor play mode by default.

An inspector override is available so a developer can force a variant in the Unity Editor while verifying the migration.

## Unity Editor validation checklist

- Open `GeoXShared.unity`.
- Confirm the `PlatformBootstrapper` object has the Quest3 and Mobile prefab references assigned.
- Run `GeoXplorer > Scene Architecture > Rebuild PlatformRoot Prefabs` or manually populate the prefab variants with the platform-specific root subtrees from the legacy scenes.
- Enter Play Mode with each override value and confirm the expected variant instantiates.
- Build/run in Editor for mobile and Quest targets. Device validation remains in later tickets.
