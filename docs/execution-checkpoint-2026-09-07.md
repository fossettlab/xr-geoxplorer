# Mechanical execution checkpoint — 2026-09-07

## Implemented

`geox-k1v.1.2` is closed. The engine-independent `GeoX.Spatial` assembly implements
scene/layer identity, explicit frame and body metadata, double-precision affine
registration, display transforms, reset, and strict versioned JSON persistence.
Earth, Moon, Mars, custom/local and unknown frames are representable; geographic
reprojection remains outside this first Cartesian implementation. Unknown units
remain unknown. Rendering and live scene integration are subsequent beads.

The domain suite has 28 passing cases, including registration invariants,
save/reload, unknown metadata, nonuniform/sheared/reflected affine transforms,
frame mismatch rejection, and subtraction of large origins before float conversion.

`geox-k1v.5` contains a prepared Android repair: new Input System, GameActivity,
and a temporary BaseInput bridge for retained MRTK/uGUI modules. The bridge is
scheduled for deletion with the modern UI migration. Its four isolated tests
pass, including actual StandaloneInputModule processing. Testing caught and
fixed its missing handling of Horizontal/Vertical button queries.

The narrow configurator command has **not been applied**. Prefabs, activity
manifest and Player Settings have not been changed by this repair. Existing
dirty settings were snapshotted before integration at
`/private/tmp/geox-android-before-20260907T044203Z`, with hashes in `manifest.json`.
No package upgrade, commit, push, APK or Quest acceptance is claimed.

## Verification

Unity 6000.4.4f1 compilation completed without errors. The final Edit Mode run
contains **51 tests: 50 passed, 1 failed, 0 skipped**. Passing results comprise
28 spatial tests, 4 input bridge tests and 18 existing tests. The failing test
is `OpenXrGamepadCompatibilityTests.ProjectLayoutsPermitGamepadCreation`:

```text
Cannot instantiate device layout 'Dpad' as child of '/Gamepad'; devices must be added at root
```

This test deliberately uses the project's actual Input System registry.
Isolated InputTestFixture tests reset that registry and cannot establish OpenXR
compatibility. Keep both kinds of tests. The portable receipt is
[execution-tests-2026-09-07.json](execution-tests-2026-09-07.json).
`git -c core.whitespace=cr-at-eol diff --check` passes with existing C# CRLF
conventions recognized. No claim is made for unavailable dotnet lint or hardware.

## Consequential review: supported OpenXR repair

New bead `geox-k1v.6` precedes Android repair `geox-k1v.5`, which precedes the
Quest baseline `geox-3kj`. Installed OpenXR 1.16.1 registers the XR device layout
DPad, colliding with Input System's Dpad control. Its Editor registration path
registers available interaction layouts, so merely disabling the feature is
not a sufficient repair. No vendor patch or global layout override was added.

Unity's [OpenXR 1.17 changelog](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.17/changelog/CHANGELOG.html)
documents renaming that device to XRDPad and fixing gamepad creation. It also
changes predicted-time and foveated-rendering defaults. This makes adoption a
package qualification decision, rather than a mechanical version bump.

Recommendation: qualify the supported 1.17.0 release in isolation, preserving
current explicit timing/rendering settings for the initial comparison. Current
serialized OpenXR settings have `m_useOpenXRPredictedTime: 0` and
`m_foveatedRenderingApi: 0` (Legacy in installed package source). Inspect the
upgrade diff rather than assuming those values survive migration. A search of
Assets C#, inputactions, assets, prefabs and scenes found no literal `<DPad>` or
`<XRDPad>` bindings; still inspect migrated/generated bindings in the trial.

Review and execution order:

1. Review the proposed OpenXR 1.17.0 trial and preservation of current defaults.
2. In an isolated checkout, capture manifest/lock and XR settings before and
   after installation; examine dependency changes and layout bindings.
3. Require all 51 regression tests to pass with the actual package registry.
4. Apply the narrow Android repair, inspect prefab/manifest/settings diffs,
   restart the Editor as required, and compile/test again.
5. Produce an Android build, then separately validate boot, stereo, tracking,
   UI input and performance on Quest. Keep hardware beads open until observed.
6. Continue the existing URP → rig/UI → registered scene integration order.
   Importer trials, fixtures and design preparation retain their independent lanes.

The requested consequential-review stop applies here. The core implementation
is complete; the overall mechanical-execution objective is not complete.
