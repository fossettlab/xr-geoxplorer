# OpenXR qualification and Android repair

The isolated OpenXR 1.17.0 trial passes the complete Edit Mode suite: **51 passed,
0 failed, 0 skipped**. This includes the actual project-registry gamepad test that
failed with 1.16.1, all four input-bridge tests, spatial tests and existing tests.
The [machine-readable receipt](openxr-qualification-2026-09-07.json) records every
case, resolved dependency change and effective feature/settings comparison.

## Qualification decision

Adopt OpenXR **1.17.0** with Unity **6000.4.4f1** and Input System **1.19.0**.
Only OpenXR changed in the resolved package graph. No global layout override,
vendor patch or other package upgrade was needed. No project `<DPad>` binding
required migration.

Effective Android and Standalone settings remain predicted-time disabled,
Legacy foveation and SinglePassInstanced rendering. Android retains Depth16Bit;
Standalone retains no depth submission. All existing feature enablement is
unchanged. Newly serialized API Layers, Debug Utils and Android Mouse features
are disabled. The obsolete Meta feature foveation field is removed by the
package; effective foveation is verified through OpenXRSettings.

This passes the engineering gate in `geox-k1v.6` and permits the reviewed Android
repair in `.5`. Android build and Quest runtime implications remain distinct:
Editor evidence does not establish stereo, tracking, controller/hand interaction,
passthrough, keyboard behavior or performance on a headset.

## Applied repair

The narrow registered `GeoXplorer/XR/Apply Reviewed Android Compatibility` command
sets the new Input System backend and GameActivity together, writes the matching
manifest, and adds one LegacyUiInputBridge to each Quest and mobile platform
prefab. The main checkout receives the two intended PlayerSettings changes,
manifest, minimal prefab component additions, package files and XR migration.
Unity's unrelated full-prefab reserialization is excluded. The original dirty
checkout was hash-checked against the trial snapshot before integrating changes.

Post-restart tests, repeat-application checks, manifest validation and the final
Android APK build now pass. Build verification also identified the missing
GameActivity Gradle setup entry and the need for an explicit ASTC build subtarget;
both are corrected. See the [final build checkpoint](android-build-checkpoint-2026-09-07.md).
Quest acceptance (`geox-3kj`) and performance (`geox-fzr`) remain open.

## Reproduction and evidence boundary

Use the official Unity CLI and the registered Pipeline commands. On an automated
Editor with this project loaded, run the complete `run_tests` command in editor
mode and poll `test_status` to completion. The trial uses the current uncommitted
source snapshot, recorded under `/private/tmp/geox-openxr-117-9ua8dxr3` with
`trial-source-manifest.json`; it is not a clean-commit or release claim.

The approved licensing-client restart resolved the startup blocker. Unity launch
output must be suppressed before reaching a transcript because the CLI injects
session credentials. Raw launch logs are not portable evidence or publication
artifacts. No commit, push or public issue update was performed in this execution.
