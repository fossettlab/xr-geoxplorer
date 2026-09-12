# Scientific scene coordinate contract

**Engineering review complete, 2026-09-06.** Bead `geox-k1v.1.1`, branch
`codex/scientific-scene-foundation`. Accepted for the first domain implementation
with the clarifications below; see the [review](implementation-review-2026-09-06.md).
This does not select a reprojection or scientific measurement method, or establish
runtime/device correctness.

**Scope note, 2026-09-07:** this remains the accepted first Cartesian contract,
implemented by the current core. The [framework review](framework-direction-review-2026-09-07.md)
extends it through new beads: portable scientific persistence separate from
optional view/runtime state, qualified nonlinear operations, and evidenced
registration of unreferenced models. Proposed records below describe the first
increment, not the final public interchange schema. Do not freeze required
display/axis fields or affine-only registration into that future schema.

Read with [the architecture](open-3d-plan.md),
[UI principles](ui-design-principles.md), and
[execution plan](beads-execution-plan.md).

## Reviewed decisions

| Choice | Recommendation | Alternative and cost |
|---|---|---|
| Initial registration | Support explicitly supplied Cartesian registration in a declared common frame. Preserve geographic/planetary definitions without converting them yet. | Implement a general projection engine first; introduces library, reference-system and planetary decisions before the basic viewer works. |
| Scientific coordinate storage | Keep source and scene coordinates in double precision outside Unity transforms. Require a declared scene axis convention; convert at the rendering boundary. | Adopt Unity coordinates as the scientific record; couples source meaning and precision to rendering. |
| Unknown inputs | Open them immediately in an explicitly unregistered scene/layer. Keep unknown units and physical calibration visible when relevant. | Require reference metadata before opening; adds friction and excludes ordinary models. |
| Scene display | One invertible display transform for the entire registered scene, including a shared rendering origin. | Normalize each layer independently; breaks relationships between layers. |
| First persistence | Versioned domain state and separate display state; preserve unknown metadata and stable identities. | Serialize Unity objects or loader state as the scene; couples persistence to platform and representation lifetime. |

These choices implement the user's accepted direction. Cartesian registration
is the first implementation boundary; geographic and planetary transformations
remain explicit later work, not removed product requirements.

## Minimal records

The names below describe proposed records, not existing APIs or a requirement
for a separate class for every row.

| Record | Required meaning |
|---|---|
| Scene | Stable ID, schema version, declared common reference frame or explicit unknown state, ordered layer IDs, display state. |
| Layer | Stable ID, source assets, original reference description and metadata, registration record, visibility, provenance. Exists while its representation is unloaded. |
| Reference description | Body, frame identity/definition and revision when supplied, coordinate kind, ordered axis names/directions, axis units, datum/reference surface, vertical reference, angular conventions and epoch where applicable. Missing fields remain absent. |
| Registration | Explicit unregistered or registered state; registered records name source and target frames, the supplied mapping and its provenance. Retain whether the registration was merely declared or independently validated. |
| Asset | Stable identity, format/media description, source/version/hash when available. Runtime access credentials belong outside saved scene state. |
| Representation | Runtime ownership of objects and resources for one layer; loading, replacement and disposal do not own or redefine scientific state. |
| Display state | Shared render origin in scene coordinates, room placement/orientation and positive uniform display scale, plus a separate saved reset pose. |

Use a minimal typed core with an opaque metadata extension area for information
that has no implemented interpretation. Do not create duplicate discovery and
domain metadata records. A layer can have several source assets; the first
runtime path can own one active representation without prohibiting later tiles.

## Reference and registration rules

- Earth, Moon, Mars and another explicitly named body are supported as metadata.
  An unknown body remains unknown; a local frame does not imply Earth.
- Frame identity names a documented coordinate definition, not just a display
  label. Same body, equal axis units, or similar frame names do not establish
  alignment. Geographic latitude/longitude also needs its stated convention,
  datum/reference surface and vertical reference to be scientifically usable.
- Initially accept a declared common Cartesian frame with an explicit identity
  mapping, or a supplied invertible affine source-to-scene mapping. Retain its
  provenance. This supports prepared aligned datasets without fitting a new
  transformation or introducing a projection engine.
- The first common frame uses orthogonal Cartesian axes with a common linear
  unit, or explicitly unknown units. Source axes may differ, but the supplied
  mapping must account for their conventions and units. Preserve those source
  definitions. A frame definition supplies the positive finite metres-per-scene-
  unit factor when known; a display gesture never edits it. Angular, mixed-unit
  or non-orthogonal definitions may be preserved without treating them as this
  supported common frame.
- Do not represent geographic reprojection as an arbitrary affine matrix.
  Unsupported mappings remain inspectable and unavailable for registered use.
  A body/frame conflict cannot be resolved by silently assigning an identity.
- A declaration of registration is not an accuracy claim. Keep supplied
  accuracy/uncertainty and validation evidence, or their absence, explicit.
- Unknown metadata does not prevent viewing. Initially offer an independent
  unregistered scene for content that cannot be placed in the current frame.
  Do not invent a scientific overlay position. A later temporary inspection
  offset must remain separate from registration edits.
- Reject non-finite coordinate values, singular mappings and numerically unusable
  inverses with a named error. The implementation must document conditioning and
  round-trip tolerances rather than use a determinant-only check or invent a
  scientific accuracy threshold. Do not replace values with zeros, discard
  individual points, or silently repair source data. Keep original metadata.

## Coordinate and display boundary

Let `p` be a source position, `R` the explicitly supplied source-to-scene map,
`o` the common scene rendering origin, `U` the declared common-unit conversion
to metres, `B` the orthogonal scene-to-render axis conversion (including
handedness), and `D` the scene display pose and positive uniform scale:

```text
scientific scene position: q = R(p)
render-local position:    r = B(U(q - o))
XR display position:      x = D(r)
```

`R` converts source units to common scene units once; `U` converts those scene
units to metres once. `B` changes axes, not units. In an unknown-unit preview,
use an explicitly nonphysical numeric identity for `U` and label scale as zoom.
This does not assert that one source unit is a metre.

A representation also declares `C`, its mapping from an imported vertex/node
coordinate `v` back into the layer's source frame: `p = C(v)`. This includes
loader basis/unit changes, node hierarchy and any source georeferencing offset.
The complete relationship is `x = D(B(U(R(C(v)) - o)))`. An importer must account
for changes it has already made; its Unity transforms are not automatically
source coordinates. Compose these maps without round trips through large float
positions. Preserve a link from picked features to source identity/coordinates;
inverting a float raycast alone does not establish source-coordinate accuracy.

This is a software contract, not a newly selected analytical method.
For Cartesian affine fixtures, the inverse is applied in reverse order to
recover the source point. Future nonlinear mappings need their own qualified
forward/inverse implementation and documented validity domain.

The domain stores coordinate metadata, origins and registration in double
precision. Retaining source assets plus local geometry and explicit mappings
does not require duplicating every mesh vertex into a second double array.
Only local rendering values cross into Unity's float-based transforms. A source
already quantized by its file format does not gain precision through storage.
The origin is shared by registered layers and is part of display state; changing
it must preserve placement through compensating display state, without editing
registration. Dynamic origin rebasing is not required for the first fixture.

The scene axis convention must be declared, rather than guessed from a file
extension or celestial body. The initial fixture uses a right-handed Cartesian
frame with X right, Y forward and Z up. Loader integration must document its
own axis conversion and any winding/normal treatment, and prove it is applied
once. Do not embed an assumed Unity/glTF conversion into the domain core.

An affine registration can contain shear, reflection or nonuniform scale.
Do not silently approximate it by a Unity position/rotation/scale decomposition.
The representation must evaluate the supported map faithfully, including
normals and winding, or return a named unsupported-representation result while
preserving the registration. No general transform framework is required now.

Grab, rotation, display scaling, recenter and reset update `D` and display
state. Registered layers retain `R` and their relationships. Ordinary layer
selection or visibility changes never alter either coordinate record.

Physical calibration is distinct from file coordinate units. Declared units
and a supported mapping permit a source-based display scale; independently
validated calibration is not a prerequisite. Retain whether scale/registration
is source-declared or validated and make that status inspectable. Unknown units
permit viewing and zoom but no physical ratio. Do not equate declared metres
with proven scan accuracy. Distance, geodesics, surface measurements, vertical
exaggeration and clipping semantics remain in their later decision task.

## Persistence and lifetime

Propose initial schema version `1` for the domain document. Save stable scene,
layer and asset identities, source metadata, registration, visibility, provenance
and distinct display/reset state. Serialize numeric values without casting to
float or reducing their precision for UI formatting. JSON null/absence denotes
missing data, never a fabricated coordinate.

Unknown newer schema versions produce a clear unsupported-version result rather
than a best-effort scene. Preserve opaque metadata through supported-version
round trips. Transport and access credentials are not persisted as asset URLs;
public immutable asset references and explicit unresolved access references are
distinct from runtime signed URLs.

Unloading or replacing a representation keeps its layer ID and scientific
record. Removing a layer is an explicit domain action. A failed/cancelled load
cannot publish a late representation. Save/reload does not serialize live Unity
objects, native resources, network ownership handles or cancellation objects.

Tabletop and immersive viewing retain the same domain scene and layer state.
Anchors place its display root; personal viewpoint changes do not rewrite a
shared registration. Later networking versions this state explicitly.

## Authored contract fixtures

The fixture generator below is the source of all numerical examples. They are
synthetic test vectors authored for this contract, not
survey data, planetary parameters, accuracy estimates or device benchmarks.
Its output is a review aid; production Unity tests still need to test the actual
implementation independently.

Run from the repository root:

```bash
python3 scripts/spatial_contract_fixture.py
```

The script prints source points, their explicit translations into a shared
scene, authored display cases and expected transformed points. It uses an
identity axis bridge for the original display invariant and an explicit
synthetic basis/unit case for the reviewed boundary. Neither selects a real
importer's conversion. A later importer fixture must additionally test its real
axis bridge, winding and normals.

The acceptance fixtures must cover:

- Two differently located source points that coincide after supplied
  registration; coincidence survives display translation, rotation, scaling,
  combined display changes and reset.
- Canonical source values, registration, body/frame, units and layer IDs remain
  unchanged while display state changes. Inverse mapping recovers source points.
- Earth, Moon, Mars and unknown/local metadata survive persistence. These are
  metadata cases, not tests of geographic or planetary reprojection.
- A Moon-to-Earth registration request, conflicting frame definitions, missing
  unit mappings, singular mappings and non-finite values fail explicitly.
- Representation replacement/unload and visibility changes preserve the layer;
  supported-version save/reload preserves values and opaque metadata.
- Nonidentity representation and axis mappings plus common-unit conversion
  compose once, preserve registration and recover the original point.
- A supplied shear/reflection remains exact or fails explicitly at the renderer
  boundary; a poorly conditioned inverse does not silently pass as usable.
- A large scientific origin with small local offsets exercises subtraction in
  double precision before float conversion. The script supplies a binary-exact
  example; actual implementation tolerances need a numeric rationale. No Quest
  precision envelope is established by these examples.

## Review disposition

**Accepted for domain implementation in the requested engineering review.**
Keep the scene/layer/representation model and initial supplied-Cartesian scope.
Clarifications address common-unit conversion, importer-to-source mapping,
affine fidelity, precision ownership and source-declared scale. Start
`geox-k1v.1.2` with actual domain tests. This disposition completes the contract
task, not its implementation or the later scientific measurement decision.
