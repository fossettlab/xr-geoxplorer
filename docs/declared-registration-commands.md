# Declared registration commands

`GeoX.Spatial.DeclaredRegistration` provides a small command boundary for an
explicitly supplied affine declaration. The caller supplies the scene
revision, layer and target frame, numeric map, record IDs, new revisions, and
provenance evidence. The commands do not solve for a registration and do not
infer geodesy, units, bodies, or CRS meaning.

The examples below use the actual C# API:

```csharp
DeclaredRegistrationProposal proposal = DeclaredRegistration.Begin(
    scene,
    expectedRevision,
    layerId,
    scene.SceneFrameId,
    mapping,
    suppliedEvidence);

SceneDocument preview = proposal.PreviewScene;
SceneDocument applied = DeclaredRegistration.Apply(
    scene,
    proposal,
    operationId,
    registrationId,
    newRevision);

SceneDocument cancelled = DeclaredRegistration.Cancel(scene, proposal);
SceneDocument undone = DeclaredRegistration.Undo(
    applied,
    applied.Revision,
    layerId,
    undoRevision);
```

## Begin and preview

`Begin` checks that `expectedRevision` exactly matches the supplied scene and
that `targetFrameId` is the scene's current common frame. It checks the layer
and frame references, finite and invertible affine coefficients, and the
evaluation boundary. Evidence must contain a valid `declaration` statement
with status `supplied`; the caller remains responsible for what that statement
means.

The returned `DeclaredRegistrationProposal` is detached from the canonical
scene. Its `PreviewScene` appends a temporary affine operation and registration
using generated, currently unused preview IDs, activates that preview on the selected layer, and
uses a preview revision derived from the base revision. `proposal.Evaluate`
evaluates the supplied map for inspection. Nothing in `Begin` or preview
changes the canonical scene.

A minimal evidence value has the shape below; real callers should provide the
lineage and declaration details appropriate to the supplied registration:

```json
{
  "origins": [],
  "lineage": [],
  "statements": [
    {
      "category": "declaration",
      "status": "supplied",
      "details": { "basis": "caller-supplied declaration" }
    }
  ]
}
```

## Apply, cancel, and undo

`Apply` accepts a proposal only against the exact scene from which it was
begun: scene identity, revision, and serialized content must still match.
The caller must supply fresh `operationId`, `registrationId`, and
`newRevision` values. Apply appends the affine operation and registration,
links the new registration from the selected layer, records the prior active
registration in `supersedes`, and validates the resulting revision. Existing
operations, registrations, frames, assets, metadata, and unrelated layers
remain intact.

`Cancel` discards only the detached preview and returns the canonical scene
unchanged. It checks that the canonical scene belongs to the proposal before
returning it.

`Undo` is an append-only revision command. It requires the caller's exact
`expectedRevision`, a known layer with an active registration, and a fresh
`newRevision`. It sets the layer's active registration to the `supersedes`
value while retaining every operation and registration record in history. It
does not erase a registration or rewrite an older definition.

## Scientific and capability boundary

These commands preserve unknown units and body identifiers exactly as supplied,
along with metadata, frame definitions, assets, operation history, registration
history, and evidence. A known conflict between two non-null body identifiers,
geographic frames, non-opaque CRS definitions, required extensions, or a
non-Cartesian common frame blocks `Begin` with an explicit reason. Affine
invertibility is a numerical usability check; it is not a registration accuracy
claim.

No command invokes a solver, geodetic provider, CRS database, asset resolver,
renderer, Unity scene, or XR device. A successful preview or applied document
therefore records an explicit caller declaration and its provenance. It does
not establish geodetic correctness, scientific acceptance, renderer fidelity,
or device behavior.
