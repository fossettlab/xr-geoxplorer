# Scientific scene and open-input approved issue packet

**Approved and published, 2026-09-04.** Read with [the implementation plan](open-3d-plan.md).
The reviewed text below was posted to GitHub and read back for verification.
Existing bodies/history, labels, assignees, milestones, and issue states were
preserved. No issues were closed. The modernization work remains.

Updated existing issues: #1, #6, #39, #15, #150, #16, #17, #23, #26, #27, #32.
The title of #1 was changed as shown below and its checklist links to:

- [#171 — Introduce scientific scenes and spatial layers](https://github.com/fossettlab/xr-geoxplorer/issues/171)
- [#172 — Open HTTPS GLB as a spatial layer on Quest](https://github.com/fossettlab/xr-geoxplorer/issues/172)
- [#173 — Validate a registered tabletop scene and expand scientific inputs](https://github.com/fossettlab/xr-geoxplorer/issues/173)

The draft labels below are retained as the approved publication record; do not
repost these blocks. The subsequently supplied [UI design principles](ui-design-principles.md)
are incorporated in the local [Beads execution plan](beads-execution-plan.md);
they were not part of this published packet.

## Existing issue updates

### #1 — modernization epic

**Proposed title:** Modernize GeoXplorer for Quest and open scientific scenes

**Text to prepend:**

> **Product update (2026-09-04):** Modernize GeoXplorer as a Quest-first XR
> environment for viewing and sharing spatially registered scientific layers;
> iOS follows. Preserve Unity 6/OpenXR, URP, MRTK3/XRI, networking, anchors, and
> collaboration work. Add standard model inputs, starting with HTTPS GLB, while
> retaining Azure catalogs and trusted AssetBundles. Azure hosting and Unity
> preprocessing become optional for ordinary models. The domain model is a
> scientific scene with registered layers and separate XR display transforms.
> Earth, Moon, Mars, and local/unknown frames must be represented explicitly.
> The former “modernization only; no new features” exclusion is superseded for
> these content and scientific-scene capabilities. The current Editor is
> 6000.4.4f1; historical Unity 2022.3/HoloLens wording below is not a new target.

Keep the historical checklist. Add links to the new issues below only after
GitHub assigns their numbers; do not invent issue references.

### #6 and #39 — bundle pipeline and URP rebake

**Text to prepend to each issue:**

> **Content scope update (2026-09-04):** Retain this work for existing curated
> and Unity-specific content. Standard model inputs will also load directly,
> starting with GLB from HTTPS hosts including Azure. A new GLB must not need a
> GeoXplorer bundle bake or catalog registration. Preserve bundle metadata and
> validate legacy behavior through the content adapter; this update does not
> cancel the bundle or URP modernization work.

### #15 and #150 — UI modernization

**Text to prepend to each issue:**

> **Product scope update (2026-09-04):** Shape the modernized Quest UI around
> easy opening, scientific layers, metadata, catalogs, and shared sessions.
> Opening a model creates a scene with one layer; adding registered datasets
> preserves their scientific alignment. Include URL entry/handoff, loading and
> cancel/retry feedback, scene reset/recenter, layer visibility/selection, and
> clear missing-metadata states. Keep collaboration available without making
> room creation a prerequisite for inspecting a personal model. Review the
> deferred scene/bootstrapper work against these flows rather than reproducing
> every historical screen. iOS interaction follows the Quest experience.

### #16 — XR interaction

**Text to prepend:**

> **Scientific scene integration (2026-09-04):** Retain MRTK3/XRI modernization.
> Direct scene manipulation operates on one XR display root so registered
> layers stay aligned. It must not overwrite their source coordinates, units,
> or registration. GLB and legacy representations share scene/layer interaction
> operations. Independently moving a registered layer is a separate registration
> edit or an explicitly temporary inspection operation, not ordinary scene movement.

### #17 — anchors

**Text to prepend:**

> **Scientific scene integration (2026-09-04):** Retain anchor modernization.
> Anchors place the scene display root in the physical room; they do not define
> or modify its scientific reference frame. Save/restore must preserve scene
> identity and layer registration for Earth, Moon, Mars, and local scenes.

### #23 — networking

**Text to prepend:**

> **Scientific scene integration (2026-09-04):** Retain the networking rewrite
> and existing regression contracts. Extend shared content state to identify
> the scientific scene/revision, layers, asset versions, registration, and scene
> display pose. Clients may render different representations of the same layer.
> Show per-client loading/failure state; do not treat a local import as proof of
> successful sharing. Do not silently change existing bundle message layouts
> or share credential-bearing URLs with peers.

### #26 — mobile companion

**Text to prepend:**

> **Platform sequencing update (2026-09-04):** Quest is the first implementation
> and acceptance target. iOS follows the proven core experience, reusing the
> scientific scene/layer model and loaders with mobile file/share entry and
> AR/touch interaction. Preserve existing mobile code during Quest work. Audit
> current package state before acting on historical AR Foundation version
> targets; do not downgrade packages to satisfy stale wording.

### #27 — Addressables

**Text to prepend:**

> **Content scope update (2026-09-04):** Addressables is a possible improvement
> for curated Unity-specific content, not the mechanism for opening arbitrary
> scientific model files. Before implementation, identify a concrete retained
> bundle use case that benefits from it. Keep standard-format runtime loading
> independent of Addressables and Azure catalogs. No automatic migration of
> newly supported model formats into AssetBundles is intended.

### #32 — hardware smoke tests

**Text to prepend:**

> **Additional acceptance (2026-09-04):** Preserve the modernization smoke
> workflows and add HTTPS GLB opening through the actual Quest UI, with
> cancellation, retry, manipulation, reset, unload, and repeat-load recovery.
> Verify that registered layers remain aligned after scene movement, rotation,
> and scaling, and that selection returns unchanged scientific coordinates.
> Include Earth/Moon/Mars reference metadata and unknown-frame handling.
> Exercise shared scene/layer state once integrated. Record the exact build
> and hardware evidence; prior launch checks do not prove these new capabilities.

## New issue: Introduce scientific scenes and spatial layers

GeoXplorer needs a domain model above individual imported models. A scientific
scene contains registered layers with source coordinates, units, provenance,
metadata, and assets. GLB, legacy bundles, and later tiled/raster/point content
are runtime representations of those layers. Scene placement and scale in XR
must leave scientific coordinates and relationships unchanged.

Acceptance:

- Introduce the minimal scene, layer, and owned representation boundary.
  Represent body/reference frame explicitly, including Earth, Moon, Mars, and
  local/unknown frames. Do not infer WGS84 or scientific calibration.
- Demonstrate that two known-aligned fixture layers retain alignment under
  scene translation, rotation, scaling, reset, and state save/reload.
- Reject silent registration of mismatched body/frame inputs. Missing reference
  information still permits explicitly unregistered viewing.
- Wrap the existing bundle path without changing its prefab/session contract,
  material behavior, or legacy presentation. Preserve metadata and source assets.
- Keep layer identity and scientific state when a representation unloads or is
  replaced. Test resource lifetime and failed/cancelled loading.
- Pass relevant Unity tests and Android build; compare legacy behavior on Quest
  with the current recorded baseline.

Coordinate with #10/#32 for the baseline and #14–17/#23 for runtime integration.
Full projection/GIS tooling, a new catalog standard, and wholesale scene/UI
rewrites are outside this foundational change.

## New issue: Open HTTPS GLB as a spatial layer on Quest

A user should open an independently hosted GLB, see it as a layer in a
scientific scene, and manipulate the scene on Quest without Unity import,
AssetBundle preparation, or Azure catalog registration. Azure-hosted GLB and
existing curated bundles remain valid inputs.

Acceptance:

- Trial a maintained runtime importer, starting with Unity glTFast; pin the
  version/dependencies tested with this project's Editor and Android/IL2CPP.
- Provide URL entry, visible loading/cancel/retry, useful errors, scene
  placement/grab/rotate/scale/reset/recenter, layer information and unload.
- Preserve known registration; explicitly label missing scientific reference
  information. Do not independently normalize registered layers.
- Test materials, textures, hierarchy, transforms, runtime shader inclusion,
  malformed data, resource budgets, cancellation, and repeated load/unload.
  A GLB with external references must be handled deliberately or rejected clearly.
- Load a GLB never imported into the Unity project from the real Quest UI.
  Record performance and verify retained legacy and modernization workflows.
- Identify the next usable desktop/phone link-handoff and local-file path.
  Keep shared scene integration coordinated with #17/#23/#32.

Implement on the scientific scene/layer foundation in bounded changes. Keep
URP and MRTK3/XRI modernization; test the importer and interaction with their
resulting configuration. The first GLB path is the first new input, not the
complete product or the end of format support.

## New issue: Validate a registered tabletop scene and expand scientific inputs

Use the addendum's tabletop study-area example to test GeoXplorer's architecture:
terrain, imagery/maps, point coverage, and observations share scientific space
while their scene moves/scales in XR. Broader model input support continues
alongside this use case.

Acceptance:

- Start with a small reproducible terrain/imagery/observation fixture in a known
  frame. Preserve source provenance; document any derived display assets.
- Toggle layers, inspect metadata, and select observations. Verify scientific
  coordinates and reference distances do not change with XR display transforms.
- Include Earth, Moon, and Mars reference descriptions without an Earth default.
- Add/evaluate OBJ with companion materials/textures; retain STL, mesh PLY, and
  FBX as later input targets rather than silently removing them.
- Evaluate Cesium/3D Tiles as a representation of registered layers on Quest;
  assess point PLY/LAS/LAZ/COPC separately from triangle-mesh import.
- Record each format as direct, requiring an explicit preparation step, or
  unsupported, with actual device evidence. Do not claim native raster or
  point-cloud support from a converted mesh demonstration.
- Specify scientific measurement semantics and display-only exaggeration/
  clipping before implementing those tools; preserve unmodified scientific state.

This is follow-on work after the initial scene/GLB path, split into scoped
implementation issues as importer trials establish feasible paths. iOS follows
the core Quest experience. No mandatory external conversion/upload service or
new geospatial streaming engine is implied.
