# Scene architecture

Issue #3 replaces the old manual `GeoXShared.unity` scene-swap workflow with one checked-in scene and per-platform prefab variants.

## Current scene inventory

`Assets/Scenes/GeoXShared.unity` is already the only enabled build scene in `ProjectSettings/EditorBuildSettings.asset`.

The platform reference scenes are preserved under `Assets/Scenes/_legacy/` for comparison during the Quest 3 modernization:

- `HoloLens.unity`
- `MobileMRTK.unity`

They are reference-only and should not be edited as active development scenes.

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

### HoloLens reference scene

- Same top-level object families as `GeoXShared`: `MixedRealityToolkit`, `Voice`, `Plane`, lights, `MixedRealityPlayspace`, and `GameUI`.
- `MixedRealityToolkit` contains repeated service-provider children in the serialized scene. This looks like accumulated prefab-instance/service state and should be reduced when the HoloLens variant is rebuilt in the Unity Editor.
- HoloLens-specific content includes `InspectionModel - HL2` and Windows/MRTK2 providers such as `WindowsMixedRealityDeviceManager`, `WindowsMixedRealityEyeGazeDataProvider`, `WindowsSpeechInputProvider`, and `WindowsDictationInputProvider`.

### MobileMRTK reference scene

- Top-level roots are `SharedPlayground`, `MixedRealityToolkit`, `MixedRealityPlayspace`, `Voice`, and `Directional Light`.
- Mobile-specific UI is organized around canvas roots such as `LobbyCanvas`, `RoomCanvas`, `InAppCanvas`, `TutorialCanvas`, and `LoaderCanvas`.
- Mobile keeps `SharedPlayground` with `TableAnchor` as a root subtree instead of the HoloLens-style `Plane` root.
- The MRTK service tree includes mobile/AR-facing systems such as `MixedRealityBoundarySystem`, `MixedRealityDiagnosticsSystem`, `MixedRealitySpatialAwarenessSystem`, and `MixedRealityTeleportSystem`.

## Target architecture

`Assets/Scenes/GeoXShared.unity` remains the single checked-in runtime scene.

Platform-specific objects move behind a prefab tree:

- `Assets/Prefabs/PlatformRoot/PlatformRoot.prefab`
- `Assets/Prefabs/PlatformRoot/PlatformRoot.Quest3.prefab`
- `Assets/Prefabs/PlatformRoot/PlatformRoot.HoloLens2.prefab`
- `Assets/Prefabs/PlatformRoot/PlatformRoot.Mobile.prefab`

The variant prefabs currently exist as the migration scaffold. The next Unity Editor pass should move the appropriate root subtrees from the reference scenes into those variants:

- Quest 3: start from the mobile/shared AR path, then replace runtime/input/anchor pieces in later OpenXR/MRTK3 tickets.
- HoloLens 2: keep the Windows/MRTK2 provider path as a best-effort secondary variant until MRTK3 validation proves otherwise.
- Mobile: preserve the existing mobile AR canvas and `SharedPlayground` path.

Use `GeoXplorer > Scene Architecture > Rebuild PlatformRoot Prefabs` in the Unity Editor to rebuild the variants from the legacy reference scenes. The helper starts Quest 3 from the mobile AR hierarchy, which is the closest existing Quest-to-be baseline before the OpenXR/MRTK3 tickets land.

## Bootstrap logic

`Assets/Scripts/PlatformBootstrapper.cs` is attached to a root object in `GeoXShared.unity`.

At runtime it resolves:

- `HoloLens2` for UWP/WSA players.
- `Quest3` for Android devices whose model/runtime identifies as Quest/Oculus/Meta.
- `Mobile` for Android and iOS fallback paths.

An inspector override is available so a developer can force a variant in the Unity Editor while verifying the migration.

## Unity Editor validation checklist

- Open `GeoXShared.unity`.
- Confirm the `PlatformBootstrapper` object has the three platform prefab references assigned.
- Run `GeoXplorer > Scene Architecture > Rebuild PlatformRoot Prefabs` or manually populate the three prefab variants with the platform-specific root subtrees from the legacy scenes.
- Enter Play Mode with each override value and confirm the expected variant instantiates.
- Build/run in Editor for mobile, HoloLens, and Quest-to-be targets. Device validation remains in later tickets.
