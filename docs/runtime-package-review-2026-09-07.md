# Runtime package review and qualification instructions

Engineering choices reviewed on 2026-09-07. Trials have not run. No package,
prefab, runtime code or settings are changed by this review. Empirical beads
`geox-k1v.6` and `.2.1` stay open. This packet supplies their next bounded work
after the user's requested pause.

## OpenXR: qualify the supported collision fix

Use exact `com.unity.xr.openxr` **1.17.0** as the trial candidate, with current
Unity **6000.4.4f1** and Input System **1.19.0** as the starting environment.
Current manifest has OpenXR **1.16.1**. The
[prior test receipt](execution-tests-2026-09-07.json) records 50 passing tests
and the failing actual-registry gamepad test. Preserve that test; an isolated
InputTestFixture resets the registry and cannot demonstrate the repair.

Unity documents a rename from XR DPad to XRDPad that addresses the layout
collision, plus related binding changes. It also changes timing/foveation
defaults and moves the foveated API choice into a feature group. These are
reasons to inspect effective settings, not only serialized fields.
[Official OpenXR changelog](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.17/changelog/CHANGELOG.html).

Decision: test the supported release before any package patch or global layout
override. Do not downgrade modernization. Preserve the current effective timing
and rendering behavior for the first comparison: serialized predicted time is
false and the installed foveation enum is Legacy. If the candidate cannot preserve
the effective configuration, return a concrete diff for review before adoption.
Keep URP as its separate, already planned migration.

Trial steps, in one isolated checkout with one Editor owner:

1. Copy the current uncommitted repair/tests/planning into the trial snapshot and
   record exactly what was copied. A worktree from HEAD alone lacks them. Preserve
   the integration checkout and its existing dirty Android/settings files.
2. Capture manifest/lock, enabled XR features, build target, effective timing and
   foveation API, rendering mode, actions/bindings and compiler state.
3. Install only the candidate through the registered Unity package workflow.
   Record exact resolved dependencies and all generated settings changes. Migrate
   actual `<DPad>` paths only if found; do not rewrite the ordinary gamepad control.
4. Wait for compilation, inspect Console, and run the existing complete Edit Mode
   suite with the actual registry. Require the formerly failing gamepad test to pass
   along with all other cases, with no masking or unexplained skips.
5. Record a go/no-go packet. A pass permits integration of the inspected dependency
   and XR-settings changes, followed by the prepared Android repair `.5`. Build and
   device evidence belongs to the subsequent baseline task; it is still required.

Reopen high-effort review if dependency resolution demands a broader upgrade,
effective feature/timing behavior changes, tests still fail, or a vendor patch
appears necessary. Build-only success cannot establish stereo, tracking, keyboard,
hand/controller interaction or performance. Keep `geox-3kj`/`geox-fzr` open until
their current-build device evidence exists. No unrelated new OpenXR feature is
enabled just because it becomes available.

## Runtime GLB: one candidate, explicit failure behavior

**Follow-up, 2026-09-09:** the [dependency review and corrected Editor
trial](gltf-importer-trial.md) accept continued isolated qualification with
Unity's built-in Collections 6.4.0. The earlier trial instructions below remain
the adoption criteria; an Editor correctness pass does not complete them.

Start the isolated importer trial with exact **Unity glTFast 6.20.0**. The official
release raises the minimum Unity version to 6.0 and updates Burst/Collections.
This matches the project generation but does not establish compatibility with
its exact dependencies or Android target. Capture the candidate's actual registry
package manifest and resolved lock; do not use the development branch or infer
the release's exact dependency graph from that branch.
[Official release notes](https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@6.20/changelog/CHANGELOG.html).

glTFast exposes buffer loading, custom loading/material hooks, diagnostics and
explicit disposal. Its success boolean can include partially loaded scenes.
Destroy instantiated scenes before disposing their shared import resources.
[Official runtime-loading guide](https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@6.20/manual/ImportRuntime.html).

Decision: qualify this candidate first; evaluate
[Khronos UnityGLTF](https://github.com/KhronosGroup/UnityGLTF) only if a documented
requirement fails. Do not ship both importers or run an unbounded feature contest.
Record exact package/license notices and transitive licenses from the resolved
trial artifacts before adoption. A package's name does not authorize all of its
network, shader or decoder defaults.

| Trial question | Required evidence / adoption rule |
|---|---|
| Exact environment | Unity/package/dependency versions, platform/graphics backend, license files, compilation and Android ARM64 IL2CPP build. Current built-in rendering and planned URP are separately identified configurations. |
| Resource control | GeoXplorer owns HTTPS fetching and redirect policy. Preflight GLB structure and reject external references for the first route; configure a rejecting companion provider as a backstop. Demonstrate no hidden fetch bypass. Buffer loading alone is not a guarantee. |
| Correctness | Official fixture validation plus actual material, texture, hierarchy, transform, handedness and bounds observations. Record diagnostics as well as the boolean result. Do not publish a partial scene as complete when a required texture/primitive fails. |
| Cancellation | Cancel during fetch, decode and instantiation; distinguish requested cancellation from final cleanup. An operation ID prevents stale completion from replacing a newer scene. |
| Ownership | Destroy instances, then dispose importer resources; prove load/unload/retry recovery without orphaned objects or surviving resources. Shared instances require one explicit lifetime owner. |
| Resource envelope | Measure download buffers, decoded texture/geometry allocations and peak CPU/GPU memory on an identified Quest build. Record provisional settings and workloads. No invented universal file-size budget. |
| Shader/build compatibility | Confirm runtime shader inclusion/material generation in the actual player. An Editor preview does not prove Android or the later URP configuration. |
| Scientific boundary | Preserve source identity and the representation-to-source mapping; imported GameObjects never become canonical coordinates. A mesh collider is not automatically an analytical surface. |

The [input policy](model-input-policy.md) and existing fixtures remain the single
policy/test source. Plain loopback transport does not qualify HTTPS or headset
access. Block production opening if resource budgets are unconfigured. Report a
specific unsupported input with recovery, rather than silently coercing it or
increasing limits until it loads. Broader inputs still follow scientific-scene
acceptance; no importer package determines the framework roadmap.

Package trial, fixture work and portable domain work may overlap in isolated
ownership lanes. Adoption into `Packages/`, settings or prefabs is serialized.
Unexpected dependency changes, unbounded fetches, unrecoverable allocations or
unresolved scientific basis conversion return to review before adapter work.
