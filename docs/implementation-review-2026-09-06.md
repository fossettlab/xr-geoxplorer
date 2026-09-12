# Scientific scene and Android integration review

**Review complete, 2026-09-06.** Requested by the user at the consequential
review boundary. The scene contract is accepted for domain implementation with
the corrections now incorporated. The Android direction is new Input System
plus GameActivity, implemented as a coherent input/manifest/configurator change.
Neither decision constitutes a working APK, a device pass or a choice of
scientific measurement method. No runtime/settings changes were made in this
review.

## Findings and decisions

| Finding | Decision and consequence |
|---|---|
| The scene/layer/representation distinction preserves registration when a mesh is replaced. | Keep it. Keep source assets and metadata; do not duplicate every vertex into a second domain-owned mesh. |
| The original coordinate equation left common-unit conversion implicit and began after any importer coordinate changes. | Corrected: name the representation-to-source map and common-unit conversion. Each conversion has one owner; test nonidentity compositions. |
| An arbitrary affine registration cannot always be represented by one Unity transform. | Preserve the full map. A renderer must honor shear/reflection/nonuniform scale or return an explicit unsupported result, never approximate silently. |
| Double storage alone cannot recover detail already rounded into a large float coordinate. | Subtract/combine origins in double before rendering; retain source feature references. A float raycast is not precision evidence. |
| Requiring independent calibration for all physical scale readouts adds an unnecessary gate. | Declared units and supported mappings permit source-based display scale. Preserve validation status; unknown units show zoom. Measurement methods still require their later review. |
| A complete planetary projection engine would delay the first useful viewer. | Keep supplied Cartesian registration first; preserve Earth/Moon/Mars/custom/local definitions. Geographic transformation support stays in the roadmap. No implicit Earth datum or body conversion. |
| Android configuration tooling actively restores the setting that failed the build. | Fix the configurator and validator together with Player Settings. A manual setting change alone is incomplete. |
| Legacy input reads occur in the selected platform prefabs, not only demos. | Migrate or narrowly bridge the actual UI input path when enabling new-input-only. Preserve functionality and avoid editing vendored MRTK wholesale. |
| The manifest template and deployment guide name the old activity. | Align GameActivity, manifest generation/validation and deployment together. Inspect the merged manifest and launcher resolution. |

## Android evidence

- `PlatformBootstrapper.Awake` selects the platform prefab; the shared scene
  references `PlatformRoot.Quest3.prefab`. Editor auto-selection uses the mobile
  variant, so an ordinary Editor play session would not qualify Quest wiring.
- The Quest prefab contains an enabled `MixedRealityInputModule` on its active
  Main Camera beneath the active MixedRealityPlayspace. The mobile prefab also
  contains this module. It inherits `StandaloneInputModule.UpdateModule`, calls
  `base.ActivateModule()` and `base.Process()`, and has no identified project
  input override. The installed uGUI `BaseInput` reads `UnityEngine.Input`.
  Source evidence therefore contradicts the idea that changing only
  `activeInputHandler` is sufficient. Runtime behavior is still untested.
- `GeoXInput.cs` already reads the new Input System for project mouse/touch
  helpers. Keep it; there is no reason to reintroduce legacy-only input.
- `QuestAndroidStoreSettingsConfigurator.cs` sets `ActiveInputHandlingBoth`
  in `ApplyCoreAndroidPlayerSettings` and requires it in
  `CollectValidationFailures`. The installed Input System maps new-only to
  serialized value `1` and Both to `2`. The earlier Android build rejected Both.
- Installed OpenXR 1.16.1 `MetaQuestFeature.cs` checks GameActivity on Unity 6.
  It currently reports a warning, not the fatal input error. Both the checked-in
  Android manifest and the configurator's template name `UnityPlayerActivity`.
  The deployment guide also hard-codes that activity.
- The configurator's template requires passthrough while the checked-in
  manifest makes it optional. A blanket rerun would alter an unrelated product
  choice. Preserve the current optional setting in this correction and validate
  the intended manifest content instead of calling the whole configurator
  blindly.

Unity's [XR input documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/xr-input-overview.html)
supports the Input System direction; its
[OpenXR input documentation](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.16/manual/input.html)
explains action/binding setup. GameActivity also changes threading assumptions
for Android plug-ins, so voice, anchors, keyboard and lifecycle behavior need
checks with the retained dependencies. See Unity's
[GameActivity compatibility guidance](https://docs.unity3d.com/6000.0/Documentation/Manual/android-application-entries-game-activity-requirements.html).

## The preloaded-assets change

The two current references in `ProjectSettings.asset` resolve to
`XRGeneralSettingsPerBuildTarget.asset` and `OpenXRPackageSettings.asset`.
Installed XR Management build processors add settings to preloaded assets
during preprocessing and perform cleanup after a build. These sources provide
a plausible explanation for the observed change around the failed build.
They do not reconstruct the original dirty file or prove exactly which entry
changed. Keep both references pending a snapshot-based before/after check;
do not revert to HEAD or delete the assets.

## Mechanical execution after review

1. Complete the contract bead and implement `geox-k1v.1.2` as portable domain
   state and real invariant tests. Use the corrected contract, including opaque
   metadata, unknown units, transform composition and stable layer identity.
2. In the Unity integration lane, snapshot current settings and relevant prefab
   bytes before edits. Correct new-input-only configuration/validation and
   GameActivity/manifest/deployment consistency. Preserve unrelated dirty work.
3. Qualify the active UI input path. Prefer the existing Input System UI module
   for ordinary canvas input where its bindings cover the flow. Where the
   retained MRTK pointer path is still needed, test a small project-owned input
   bridge rather than duplicate the input stack or patch vendor files. Remove
   that bridge when the scheduled MRTK3/XRI replacement owns those interactions.
   Merely disabling the legacy module does not prove working controls.
4. Run compilation, input lifecycle/event checks, existing regression tests and
   an Android build. Check generated manifest/activity and repeat configuration
   for idempotence without resetting optional passthrough or unrelated settings.
5. Record the identified APK and actual Quest boot/stereo/tracking and retained
   flow results before closing #10; retain failures as baseline findings. URP
   and the rest of the approved modernization keep their integration order.

Domain implementation and importer qualification can progress independently
of the hardware lane. Importer selection still needs real package/Android
evidence. The coordinate engineering review no longer blocks the pure domain
task. This review does not close hardware or importer tasks and does not select
a geodesic, registration-fitting or scientific measurement method.

## Delete and simplify

Remove the Both-setting requirement, the old-activity assumptions and the
extra calibration prerequisite described above. Avoid a general projection
engine, global source registry, duplicate geometry store or new input framework
at this stage. Preserve modernization, legacy content and scientific metadata.

Review checks: current source/prefab/configurator trace, comparison with the
installed Unity package code and primary documentation, and authored contract
fixture checks. The previously reported 18 passing Edit Mode tests and failed
Android build are historical baseline evidence; they were not rerun or promoted
to new-input or Quest validation by this document review.
