# Legacy platform scenes

`HoloLens.unity` and `MobileMRTK.unity` are reference-only fossils for the scene-management migration in issue #3.

Do not use them as active development scenes. `Assets/Scenes/GeoXShared.unity` is the canonical scene; platform-specific differences should live in `Assets/Prefabs/PlatformRoot/` prefab variants. These legacy scenes can be deleted after the first Quest 3 ship.
