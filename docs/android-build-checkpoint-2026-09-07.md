# Android build checkpoint

The OpenXR/input/GameActivity milestone is complete. OpenXR qualification
(`geox-k1v.6`) and Android compatibility repair (`geox-k1v.5`) have software
evidence. **Quest hardware acceptance remains open.**

## Result

| Check | Observed result |
|---|---|
| Environment | Unity 6000.4.4f1, OpenXR 1.17.0, Input System 1.19.0 |
| Final Edit Mode suite | 51 passed, 0 failed, 0 skipped |
| Android build | Succeeded; 0 errors, 14 warnings |
| Player | ARM64 IL2CPP, Vulkan, ASTC; GeoXShared scene |
| Packaged launcher | UnityPlayerGameActivity, BaseUnityGameActivityTheme, native library `game` |
| Native libraries | ARM64 only; `libgame.so` and `libil2cpp.so` present |
| APK integrity | Android SDK apksigner verification passed, v2 signature |
| Repeat repair | Manifest and prefabs byte-identical; PlayerSettings values unchanged despite dictionary ordering |
| Standalone C# lint | Unavailable: `dotnet` command not installed |

The [complete receipt](android-build-checkpoint-2026-09-07.json) includes test
cases, build diagnostics, source identity and APK verification. The warnings
are the preserved optional OpenXR latency recommendation, Pipeline being absent
from the player, and compressed app-icon warnings. The first full compilation
also emitted legacy API deprecation warnings; the incremental rebuild does not
erase that maintenance work or qualify the planned rig/UI migration.

APK: `build/GeoXplorer-openxr-1.17.0-2026-09-07.apk` (81,455,624 bytes).

SHA-256: `85315d46e3fc344d219c39c5599167fd1a504c9cf111efaabc47a670430226c8`

## Corrections established by the build

The first build produced an APK but reported a missing GameActivity CMake setup
entry. It was not accepted as the final checkpoint. The custom Gradle template
now includes `**DEFAULT_CONFIG_SETUP**` and `../shared/common.gradle`, matching
the installed Unity template. The Android validator checks both requirements.
Existing Azure/anchor dependency content and unrelated Gradle edits are retained.

The generic Pipeline build command also reset the Android texture subtarget to
Generic. `GeoXEditor.CommandLineBuild.BuildAndroid` now supplies ASTC explicitly,
writes `build/GeoXplorer.build.json`, and treats reported build errors as failure
even if Unity's result says Succeeded. Unity documents the explicit target and
subtarget relationship in its [BuildPlayerOptions API](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/BuildPlayerOptions-subtarget.html).

## Source and reproduction

The build includes the uncommitted scientific-scene/input work and existing
Android edits on `codex/scientific-scene-foundation`, based on
`a87c7d73b9e892355b46fe5478eba9520876aba2`. No commit, push or publication occurred.
Full hashes for 7,780 Assets/Packages/ProjectSettings files are saved beside the
APK in `build/GeoXplorer-openxr-1.17.0-2026-09-07.source.json`; its digest and the
integration-file hashes are also recorded in the portable receipt.

The isolated build inputs match the integrated files. After building, Unity's
XR Management processors remove the two temporary XR `preloadedAssets` entries
from trial PlayerSettings; their package code adds current target settings at
build preprocessing and cleans them after the build. This observed post-build
difference is documented, and the original integration settings are preserved.

Use the official Unity CLI with the automated Editor. Run `run_tests` in editor
mode and poll `test_status` through completion. For the APK, invoke the project's
`GeoXEditor.CommandLineBuild.BuildAndroid` entry point; it uses the committed
scene path and explicit ASTC subtarget. This execution scheduled that method
through the registered `eval` command after compilation and validation. The
method exits the Editor after writing its build receipt. Keep launch credentials
out of transcripts and portable logs.

## Next acceptance

`geox-3kj` uses this identified APK for current Quest launch, stereo and tracking
observations. `geox-fzr` follows with measured performance. Follow
[the hardware checklist](hw-smoke-test-checklist.md) and
[updated deployment instructions](quest3-build-and-deploy.md). Historical July
headset observations do not qualify this APK. Independent portable-scene work
can continue through `geox-k1v.1.5`; runtime integration retains its baseline gate.
