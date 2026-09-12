# Isolated glTFast Android checkpoint — 2026-09-11

Bead `geox-k1v.2.1` now has an isolated ARM64 IL2CPP APK with explicit glTFast
shader inclusion. **Quest execution is still pending.** This is not production
importer adoption and does not change GeoXplorer packages or settings.

Machine-readable receipt: [contracts/gltf-importer-android-2026-09-11.json](contracts/gltf-importer-android-2026-09-11.json).

## What passed

| Check | Evidence |
|---|---|
| Isolated project | `/private/tmp/geox-gltf-trial-8wfjn2_a`, marker present |
| Unity | 6000.4.4f1 batch build via `GeoX.GltfTrial.ImporterBuild.BuildAndroid` |
| Android build | Succeeded; 0 errors; 1 expected warning that Pipeline is absent from the player |
| Player | ARM64 IL2CPP, Vulkan, GameActivity, package `edu.wustl.fossett.geoxgltftrial` |
| Shaders always included | `glTF/PbrMetallicRoughness`, `glTF/PbrSpecularGlossiness`, `glTF/Unlit` |
| Packaged fixtures | All generated model-input fixture bytes match their manifest hashes |
| APK | Signature, 16 KB alignment and ARM64-only native libraries verified |
| Application integrity | All pre-existing Assets/Packages/ProjectSettings hashes unchanged |

APK: `build/gltf-importer-trial/GeoX-GltfTrial-2026-09-11.apk`  
SHA-256: `b8fa19cbc6deef0f2a9c2b43679844300678d0216922bc66375cf29a8cccf4cb`

The player copies packaged fixtures and runs the existing diagnostic, rejection
and pre-work cancellation cases, then writes
`android-importer-receipt.json` under persistent data. That receipt does not
exist until the APK is launched on a device.

## Limits

- No Quest was connected. Do not close `.2.1` from this build alone.
- Built-in RP shaders only. Identify URP separately when #13 is qualified.
- Player lifetime, native-job interruption and memory pressure remain device work.
- Production `Packages/manifest.json` is unchanged. Do not adopt glTFast from this APK.
