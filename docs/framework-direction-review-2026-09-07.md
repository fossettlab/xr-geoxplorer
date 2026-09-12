# GeoXplorer framework direction: architecture review

**Disposition: keep the implementation; strengthen the scientific core and reorder its next milestones.**
This review applies the product direction supplied on 2026-09-07. It supersedes
the earlier GLB-led expansion order, while preserving Quest-first modernization,
iOS later, easy opening, and the approved spatial UI principles. It defines work
to implement; it does not select new analytical methods or claim them complete.

## Product definition and success test

**GeoXplorer is a geospatial and scientific spatial framework for XR.** It owns
the meaning of a scene: reference systems, registration, scientific layers,
source identity, provenance and evidence. Unity/OpenXR is the first runtime.
Meshes, splats, point clouds, raster-derived surfaces and generated environments
are inputs or representations; rendering engines are backends. These are
separate roles, even when one library supplies more than one role.

The success test is a saved scientific scene that preserves spatial meaning
when assets are unloaded, representations are exchanged, or another conforming
runtime reads it. Identical pixels are unnecessary. Agreement on coordinates,
units, registration, source identity and evidence is required. A renderer that
cannot represent a layer must report that limitation without rewriting it.

The first demonstration should combine referenced terrain/imagery and observations,
then explicitly register a previously unreferenced model. Move the whole study
area on a Quest table, inspect an observation in source coordinates, save/reload,
and exchange a representation without disturbing registration. Earth is an initial
fixture choice, not a core default; qualified Moon/Mars examples follow the same
contract. No site, planetary parameter or accuracy value is invented for this plan.

## Findings in the implementation

| Finding | Consequence | Required change |
|---|---|---|
| `GeoX.Spatial.asmdef` has no engine references; affine math and invariant tests are already separate. | This is a useful foundation. | Keep the core and its tests; do not restart or create a second competing scene model. |
| `SceneState` requires `Display`, `ResetDisplay` and a render `AxisBridge`. | Engine types are absent, but scientific persistence still requires presentation state. | Split a canonical scene document from optional view/session state. Moving a table or changing renderer axes must not revise scientific identity. |
| `Registration.SourceToScene` is always `AffineMap`; geographic mappings are rejected. | Correct for the reviewed first increment, insufficient for CRS operations. | Keep affine as one explicit operation kind. Add serializable operation descriptions and a qualified evaluator boundary; never disguise nonlinear reprojection as a matrix. |
| `ReferenceFrame.Definition` is opaque JSON with string axis convention and one unit factor. | Metadata preservation is useful; it is not validated geodetic interpretation. | Adopt a bounded, versioned reference description based on existing CRS definitions; retain original definitions and explicit supported/unsupported status. |
| Registration requires equal bodies and known-unit presence on both sides. | It correctly blocks accidental overlays, but also blocks deliberately placing an unknown-scale/local model into a referenced scene. | Add a distinct registration solution that can establish a relation using supplied evidence. Preserve the original unknown source metadata; never relabel it as an observed CRS. Keep conflicting known bodies rejected. |
| Assets require an application resolver key. | Meaning can be stored, but another reader cannot resolve assets from that key alone. | Separate asset identity/version/hash from optional portable locators and runtime credentials. Missing/private assets remain unresolved records. |
| Opaque provenance survives round trips; uncertainty has no interpreted contract. | Preservation alone cannot support reliable inspection or accuracy claims. | Add minimal typed evidence fields now; fuller registration evidence and uncertainty inspection follows the method review. Do not promise automatic propagation. |
| The execution graph prioritizes input breadth after GLB. | Format count can become the delivery target before scientific portability is proven. | Gate broader adapters on the registered scientific demonstration; retain one mesh adapter as an early integration probe. |

Code inspected: `Assets/Scripts/Spatial/Core/SceneState.cs`, `SceneJson.cs` and
`GeoX.Spatial.asmdef`. The [execution checkpoint](execution-checkpoint-2026-09-07.md)
records the previous test evidence, including the unresolved OpenXR failure.
No C# or package change is made by this architecture review.

## Boundaries to establish before expanding adapters

### Scientific document, view state and runtime

The **scientific document** contains scene/layer identities and revision,
reference definitions, source asset identities, operation/registration records,
and minimal metadata/provenance/evidence. It must be readable with no Unity
Editor, renderer, network session, asset download or credential present.

**Optional presentation state** contains preferred visibility/style, tabletop or
immersive view, reset state, display placement and scale. Device anchors, current
render origin/axis bridge, camera, active LOD and resource handles belong to the
runtime/session. An engine-neutral bookmark may be portable; a room anchor is
not geographic truth. Scientific, presentation and collaboration revisions need
explicit boundaries so a personal view change does not rewrite a shared study.

Publish the wire schema and language-neutral fixtures outside `Assets/`;
C# DTOs are an implementation of that contract. Newtonsoft is an implementation
dependency, not a public file-format requirement. Choose an explicit migration
from current experimental schema 1; preserve fixtures and reject unsupported
versions rather than silently reinterpreting saved documents. This is a project
interchange contract, not a claim to have created an industry standard.

### Source, world, scene and render coordinates

Name the spaces instead of relying on an overloaded `world` variable:

```text
imported representation coordinates
  -> declared representation-to-source mapping
  -> source coordinates and source reference definition
  -> qualified operation or explicit registration solution
  -> common scientific scene coordinates
  -> shared local rendering origin, render basis and unit conversion
  -> XR placement and viewpoint
```

A **body-fixed/world frame** is an explicit optional scientific frame in that
operation chain. It is useful for geocentric data; it is not Unity world space,
not always Earth ECEF, and not mandatory for a specimen or engineering-local
scene. The common scientific scene may use an appropriate Cartesian local or
projected frame. Preserve original geodetic definitions separately. The existing
Cartesian formula remains valid for its supported affine subset.

Maintain double precision through scientific operations and origin subtraction.
Coordinate conversion includes axis order, units and vertical reference; source
precision, transformation accuracy and render precision are different quantities.
Nonlinear transforms need documented validity and inverse availability. Densifying
geometry or deriving a local approximation is a recorded adapter/preparation
operation, with its own error evidence, not an invisible global matrix.

CRS **conversion** and **registration estimation** solve different problems.
Known CRS definitions do not locate an arbitrary model. Choosing corresponding
points does not identify an unknown source CRS. A preview placement is not an
accepted registration. An explicit registration commit creates a new recorded
solution and preserves the previous one; display gestures never do this.

### Geodetic provider qualification

Use an existing geodetic implementation rather than writing a projection engine.
PROJ is a candidate for evaluation, not an installed dependency or an automatic
choice of every available transform. The trial must define the supported CRS/body
subset, axis/vertical/epoch handling, operation selection, required grid resources,
versioning, offline behavior, Android/IL2CPP packaging and reference test evidence.
Unavailable resources or out-of-domain coordinates must produce explicit failures,
not silent ballpark substitutions. Store the selected operation and resource
identities so another runtime need not repeat an ambiguous operation search.

Start with a bounded sourced Earth conversion and explicitly defined Moon/Mars
cases that the chosen provider can support. Do not claim planetary correctness
from a body-name serialization test or assume terrestrial registries cover every
planetary convention. General cross-body/time-dependent ephemeris operations
remain outside this increment. Missing context remains unknown.

[PROJ distinguishes coordinate conversions and reference-frame transformations](https://proj.org/en/stable/operations/index.html).
Its [operation APIs](https://proj.org/en/stable/development/reference/functions.html)
make axis order, epochs, area of use, grid availability and approximate-operation
policy relevant to qualification. [PROJJSON](https://proj.org/en/stable/specifications/projjson.html)
is a candidate way to retain a full CRS definition; GeoXplorer should not invent
a competing CRS notation. These references justify the boundary, not a selected
scientific transformation policy.

### Registration, provenance and uncertainty

Keep minimum evidence from the first portable schema: source and processing
identity/version, original reference description, operation description, and
whether facts are supplied, derived, independently checked, or unknown. Retain
measured, reconstructed, interpreted, generated and mixed origin where supplied;
record lineage rather than force every dataset into one exclusive category.

The registration review must choose which user-supplied and estimated operations
are supported first, required constraints/control observations, how solutions
are accepted/replaced, and which residual/uncertainty statements are justified.
A model may open without any of these. Explicitly aligning an unreferenced model
can establish placement and scale without pretending its input carried them.

Residual fit, source accuracy, datum-transform accuracy, tracking stability and
representation approximation are distinct. Preserve units, frame, basis,
method and validation status for any uncertainty statement. Absence is not zero;
a good visual fit is not a scientific accuracy claim. No default error model,
fitting algorithm or propagation formula is selected by this review. The UI
implementation explicitly owns preview/apply/cancel/undo and the reviewed
registration input flow, in addition to inspection; solution records alone do
not deliver a usable registration workflow.

### Adapters and portable conformance

Keep the contracts small: an asset/representation adapter, an operation evaluator,
and a render/view adapter. Introduce interfaces only at an actual caller/provider
boundary. No plugin marketplace, universal scene graph, generic dependency
injection framework or distributed service is needed.

The portable contract needs a headless C# reader and an independent JavaScript
reader of the same fixtures, with semantic round trips and known coordinate
results. Neither may rewrite unsupported operations as identity. Test missing
assets/providers separately from malformed scientific documents. Run this
conformance work alongside initial Unity coordinate integration; require it
before accepting the scientific-scene milestone. Then add a
minimal second-renderer probe using the same document and fixture; it is a
conformance experiment, not a parallel Three.js/WebXR application launch.
Quest remains the only near-term product runtime; iOS follows its acceptance.

Scientific tools depend on the evidence a representation can return. A picked
splat, collision proxy or reconstructed mesh is not automatically a source
observation or a measurement-quality surface. Adapters must identify source
feature/coordinate mapping and query limitations. Unsupported layers stay saved
and inspectable, and importing a fallback is explicit rather than silently
substituting scientific geometry.

## Sequence and deletion decisions

The baseline and science work form two coordinated lanes. The priority remains
current Quest/OpenXR baseline, portable abstractions, qualified geodetic operations,
coordinate isolation, registration, evidence inspection, then adapter breadth.
Pure schema/design/tests can proceed while hardware is unavailable; production
Unity integration keeps its baseline and package-review gates.

Retain one qualified GLB adapter and the legacy seam as integration probes.
Do not require every CRS or manual-registration feature before trying a mesh.
Do require the scientific scene demonstration before expanding OBJ/other meshes,
streamed layers, splats or named generative-service integrations. Input ease
remains a core experience requirement, without becoming the product definition.

Delete or avoid:

- GLB as the product's milestone spine and any format-count success criterion.
- Required render-axis, camera/room-anchor and live-loader state in scientific persistence.
- A second generic viewer model, bespoke CRS language, hand-written global geodesy engine,
  or engine-specific scientific truth inside a tiles/splat backend.
- A separate Marble/Atlas workflow when an exported asset can use an existing adapter.
- Mandatory cloud conversion, speculative caches/registries, and a full web/iOS
  product before the Quest study-area workflow is validated.

Keep URP, modern rig/UI, networking, voice, anchors and passthrough. Simplify their
boundaries around the scientific document. The temporary legacy input bridge is
still scheduled for removal when its modern replacement is validated.

## Splats and generative systems

These are later consumers of the framework. Marble documents both
[splat and GLB exports](https://docs.worldlabs.ai/marble/export/specs), so an
export-first trial can reuse format adapters before adding a service connector.
Its [collider mesh](https://docs.worldlabs.ai/marble/export/mesh) is a separate
representation with a different purpose. Generated/reconstructed content retains
its origin, source links and processing history; visual plausibility does not
establish coordinates, scale or measurement validity.

Atlas means [World Labs Atlas](https://www.worldlabs.ai/blog/atlas), announced
on 2026-09-01. World Labs describes explicit point-cloud and Gaussian-splat
outputs, reconstruction alongside generation, and future use in Marble. The
announcement places Atlas in early access; it does not establish a generally
available integration contract for GeoXplorer. Qualify actual access, exports,
coordinate/scale conventions and terms when available, without blocking the core.

Atlas can also fill regions unseen by its input cameras. Preserve source images,
camera/reference information and observed-versus-inferred coverage when supplied;
when coverage is unavailable, say so rather than assigning confidence or treating
the whole output as measured. This is a requirement for GeoXplorer's evidence
model, not a reason to build a world generator. Geometry types, provider names
and render engines never become the primary scene identity.

## Acceptance and next review gates

The [execution plan](beads-execution-plan.md) and live Beads graph carry the
ordered tasks. Existing completed core/contract beads remain closed for their
original scope. New work extends them instead of rewriting their completion.

The [review phase is now complete](review-decisions-2026-09-07.md) for the portable
schema/migration, declared registration/evidence, interaction contract and bounded
package/provider trial decisions. Empirical package/provider qualification remains
open. Automatic fitting/uncertainty and later scientific-tool decisions retain their
investigator gates. Renderer portability is an acceptance test to build, not a
claim established by this prose. Execution is paused so the user can lower effort.


GitHub scope is published in [#174](https://github.com/fossettlab/xr-geoxplorer/issues/174)
and [#175](https://github.com/fossettlab/xr-geoxplorer/issues/175), with the
[related issue changes](framework-github-update-2026-09-07.md) verified by read-back.
Beads retains OpenXR qualification as the first runtime priority. Portable
implementation and importer/provider trials are ready for the next execution
phase in independent ownership lanes, without treating hardware gates as passed.
