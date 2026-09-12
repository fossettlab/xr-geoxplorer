# Framework direction: GitHub update packet

**Published and read back, 2026-09-07.** Created
[#174](https://github.com/fossettlab/xr-geoxplorer/issues/174) (geodetic operations)
and [#175](https://github.com/fossettlab/xr-geoxplorer/issues/175) (registration/evidence).
Updated #1, #171, #172, #173, #15, #16, #17, #23 and #26. Each final body/title was
read back and compared with its local payload. Existing issue metadata was
preserved; dated scope sections take precedence over historical prose. Two
conflicting legacy sentences were corrected: #1's modernization-only exclusion
and #15's instruction to copy historical layouts. No issues were closed.

The text below records the new content. Posted updates also link #174/#175;
#1 adds both to its checklist, and #175 names #174 as its prerequisite.

## New issue: Add qualified geodetic operations and coordinate isolation

GeoXplorer is a geospatial/scientific spatial framework for XR. Establish a qualified coordinate-operation layer beneath its portable scientific scene, then keep scientific world/scene coordinates separate from local rendering and XR placement. Preserve the existing Cartesian affine core as a supported subset.

## Acceptance

- Review the supported CRS/body subset and a mature geodetic provider before adoption. Record axis, unit, vertical-reference, epoch and area-of-use policy; required grids/resources, versions and offline failure behavior; Android/IL2CPP integration and sourced reference tests.
- Preserve full source definitions and the selected operation/resource identities in a renderer-independent description. Affine maps and nonlinear coordinate operations remain distinct. Missing evaluators or resources cannot silently substitute identity or a lower-accuracy operation.
- Include bounded, explicitly sourced Earth and Moon/Mars cases where supported. Unknown/local frames stay valid; do not infer WGS84 or planetary constants. General cross-body ephemeris operations are outside this increment.
- Implement the reviewed subset with justified numerical tolerances, forward/inverse behavior and explicit invalid/out-of-domain cases. Do not equate numerical round-trip error with scientific accuracy.
- Distinguish source coordinates, optional body-fixed/world frames, common scientific scene coordinates, shared local rendering coordinates and XR placement. Body-fixed world is not Unity world and is not mandatory for a local specimen scene.
- Verify shared-origin subtraction, render-axis/unit conversion, display movement/scale/reset and source picking preserve scientific meaning. Nonlinear geometry approximations must be explicit and evidenced, or unsupported.

## Dependencies and scope

Extend #171's portable scene contract. Pure provider/schema work can proceed alongside the current #10 baseline; production Unity integration follows the identified baseline and package review. The GLB and legacy adapters consume this boundary; Cesium/tiles are later adapters, not the owner of scene meaning. Scientific transformation policy requires explicit review before implementation.

## New issue: Register unreferenced models with inspectable scientific evidence

Allow a researcher to explicitly register an unreferenced model into a scientific scene while preserving its original source metadata. Opening and previewing remain easy; preview placement, scene manipulation and accepted scientific registration have different meanings.

## Acceptance

- Review supported supplied/estimated mappings, constraints or control observations, scale handling, solution acceptance/revision and uncertainty semantics before implementing a fitting method.
- An unknown local model can acquire an evidenced target placement/scale without rewriting unknown source body, units or CRS as observed facts. Reject conflicting known bodies and unsupported mappings explicitly.
- Preserve source data, controls and solution provenance; support preview, explicit commit, replacement/undo and save/reload. Ordinary scene grabs, view scaling and room anchors never edit registration.
- Carry minimum provenance from the portable scene schema onward. Contextual inspection shows source/processing identity, registration solution, supplied/derived/checked/unknown status, and uncertainty where supported.
- Keep source accuracy, fit residual, geodetic-operation accuracy, tracking and representation approximation distinct, with units/frame/method when stated. Missing uncertainty is unknown, not zero; no unreviewed propagation rule.
- Preserve observed, reconstructed, interpreted, generated and mixed lineage where supplied. For world-model outputs, retain evidence of observed versus inferred coverage when available; otherwise record that it is unavailable.
- Demonstrate the workflow in a registered Quest study area using referenced layers plus an initially unreferenced model. Save/reload and representation replacement retain scientific meaning. Keep the interface quiet, contextual and direct-manipulation first.

## Dependencies and scope

Use #171's portable scene contract and the qualified coordinate-operation/render boundary. Integrate with #15/#16 and the study-area acceptance in #173. Registration-method review and geodetic-provider review are separate decisions. This does not add a general 3D editor, automatic scientific validation or a mandatory cloud service.

## Update #1: Modernize GeoXplorer as a scientific spatial framework for XR

## Framework direction — 2026-09-07

GeoXplorer is a geospatial/scientific spatial framework for XR. It owns portable scientific scene meaning: reference systems, registration, layers, source identity, provenance and uncertainty where known. Unity/OpenXR is the first runtime; render engines and mesh/tile/point/splat representations are replaceable through qualified adapters.

The near-term sequence is current Quest/OpenXR baseline → portable GeoX.Spatial abstractions → qualified CRS/geodetic operations → scientific/world/local/render separation → explicit registration of unreferenced models → fuller provenance/uncertainty inspection → additional adapters. Minimal provenance begins with the schema, not at the end. Keep one GLB adapter as an early integration probe; format count is not the product goal.

A scientific scene must eventually serialize independently of its renderer and retain spatial meaning in Unity/OpenXR, Three.js/WebXR or another conforming backend. Prove the contract with independent readers before claiming portability. Quest remains first; iOS follows the scientific Quest workflow. Keep URP, MRTK3/XRI, networking, voice, anchors, passthrough, Azure catalogs and trusted legacy bundles.

World Labs Marble/Atlas are later input/backend opportunities. Atlas means https://www.worldlabs.ai/blog/atlas. Prefer export/adapter reuse; do not build a world generator or assume a generally available Atlas integration. Generated content retains its evidence limits.

This sequence supersedes conflicting historical GLB-led expansion and modernization-only wording below. The earlier platform exclusions remain; this update does not restore HoloLens or change qualified package versions.

Existing issue body retained below this section.

## Update #171: Establish portable scientific scenes and spatial layers

## Framework direction — 2026-09-07

Extend the existing Cartesian core into a renderer-independent scene contract. Separate canonical scientific document/revision from optional presentation, room-anchor and runtime resource state. Preserve identities, original reference definitions, serializable operations, asset identity/version with optional portable locators, and minimal evidence/provenance. An unresolved asset or missing renderer must not invalidate the scientific record.

Publish a language-neutral schema and fixtures outside the Unity app. Define migration from the experimental document before making it an interchange promise. Keep affine maps as one operation kind; qualified geodesy and explicit registration estimation have separate work items. Verify headless C# and independent JavaScript readers recover the same scientific values and semantic round trips, with explicit unsupported-operation behavior.

Retain the legacy adapter and lifetime/invariant criteria below. Do not close this issue from the existing pure-C# assembly alone: no-engine-reference code is a starting point, not cross-runtime proof.

Existing issue body retained below this section.

## Update #172: Open HTTPS GLB as a spatial layer on Quest

## Framework direction — 2026-09-07

Keep HTTPS GLB as the first narrow representation adapter and practical test of the scientific framework. Implement it on the revised portable document and qualified coordinate boundary, preserving source identity, registration and evidence. It must not introduce another model/scene data model or reinterpret scientific coordinates in Unity transforms.

Importer trials and fixture preparation can run alongside core work. The initial Cartesian/unregistered probe need not wait for every registration tool. Broader format expansion follows the registered scientific Quest demonstration in #173. Easy opening and the retained modernization checks below remain required.

Existing issue body retained below this section.

## Update #173: Validate scientific scene continuity on Quest and expand adapters

## Framework direction — 2026-09-07

Make the registered study-area workflow the acceptance milestone before broad format expansion: referenced terrain/imagery and observations, an explicitly registered previously unreferenced model, contextual evidence inspection, source-coordinate picking, save/reload and representation replacement. Use the portable contract in #171 and the qualified coordinate/registration work; metadata-only body tests do not establish planetary reprojection.

After this milestone, evaluate additional mesh/tile/point/raster adapters, then Gaussian splats and World Labs Marble/Atlas outputs. Atlas means https://www.worldlabs.ai/blog/atlas. Reuse exported formats before building provider connectors; record actual access, coordinate/scale conventions, provenance, inferred coverage and representation/query limits. A splat or collision proxy is not automatically a measurement surface.

Add a minimal second-renderer conformance probe after the Quest workflow: the same scientific scene must retain meaning while view/renderer state differs. This is not a parallel WebXR product launch. Keep iOS after scientific Quest acceptance and preserve all existing format targets with explicit supported/prepared/unsupported status.

Existing issue body retained below this section.

## Update #15: (13) Rebuild HandMenu, MenuManager dialogs, slates, buttons in MRTK3

## Framework direction — 2026-09-07

The UI serves the scientific framework through quiet contextual spatial interaction. Keep Open/preview easy, with no required CRS form. Explicit registration and evidence inspection are separate progressively disclosed flows. Show relevant unknown/generated/mixed-origin status and meaningful uncertainty without a permanent dense inspector. Preserve scientific information while keeping the dataset dominant; do not copy historical panels wholesale.

Existing issue body retained below this section.

## Update #16: (14) Hand tracking + XRI 3.x interactor wiring (replaces MRTK 2 interaction)

## Framework direction — 2026-09-07

Use the qualified scientific-to-render boundary for scene-root interaction and source-coordinate picking. Distinguish view gestures, temporary inspection offsets and explicit registration commits. Keep source/registration identity stable through tabletop/immersive presentation changes. A picked proxy or splat must expose source/query limitations rather than imply measurement accuracy.

Existing issue body retained below this section.

## Update #17: (15) Meta Spatial Anchors on Quest 3 — vertical slice (replaces ASA)

## Framework direction — 2026-09-07

Room anchors and runtime placement belong to optional session/view state. A portable scientific scene must load and retain its meaning without its original headset, anchor or room. Saving/restoring anchors must not rewrite scientific reference systems or registration; backend anchor IDs are not canonical scientific coordinates.

Existing issue body retained below this section.

## Update #23: (20) Networking rewrite: migrate off Photon PUN 2 to chosen stack

## Framework direction — 2026-09-07

Share the portable scientific document/revision and stable layer, asset and operation identities. Keep personal view changes and device/room anchors separate from scientific revisions and registration edits. Clients may use different conforming representations or report an unsupported layer without changing the shared science. Unity object hierarchies and NetworkTransform values are not the canonical scientific record. Retain transport, voice and existing regression requirements.

Existing issue body retained below this section.

## Update #26: (23) Mobile companion: AR Foundation 3.1.3 → 5.x (does not touch headset path)

## Framework direction — 2026-09-07

iOS follows the registered scientific Quest workflow in #173. Reuse the portable scene meaning, qualified operations and adapter contracts with platform-specific view/input state. This sequencing does not require implementing a Three.js/WebXR product first and does not revive old Unity/AR Foundation version targets.

Existing issue body retained below this section.

