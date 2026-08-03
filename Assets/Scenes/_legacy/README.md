# Legacy platform scenes

`MobileMRTK.unity` is a reference-only fossil for the scene-management migration in issue #3.

Do not use it as an active development scene. `Assets/Scenes/GeoXShared.unity` is the canonical scene; platform-specific differences should live in `Assets/Prefabs/PlatformRoot/` prefab variants. This legacy scene can be deleted after the first Quest 3 ship.

The former `HoloLens.unity` reference scene was removed when HoloLens 2 was dropped as a target (2026-06-06).
