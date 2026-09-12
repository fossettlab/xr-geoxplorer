# Portable scientific scene documents

`GeoX.Spatial.SceneDocument` implements the accepted
[v2 review](portable-scene-schema-review.md). It holds scientific records without
Unity objects, loaded geometry, display settings or access credentials. Meshes,
splats and other representations can refer to the same scientifically defined
layer. This is the persistence foundation for adapters; the Quest opening flow
does not yet load these documents.

## Reading and evaluating

```csharp
SceneDocument scene = SceneDocument.Read(json); // Explicitly schema 2; offline.
JObject inspection = scene.ToJObject();         // Detached copy, safe to inspect/edit.
string saved = scene.ToJson();

if (scene.TryGetAffineOperation(operationId, out AffineMap map, out string reason))
{
    Vector3d target = map.Apply(source);
}
// Otherwise show reason; never substitute identity.
```

The [JSON Schema](contracts/scene-v2.schema.json) describes shape. The reader also
checks duplicate properties/IDs, finite scientific values, references, registration
endpoints, representation endpoints, acyclic history and affine invertibility.
It rejects non-JSON conveniences such as comments, single quotes and trailing
commas. Integer version tokens are required; strings and floating tokens are not
coerced into versions. Unknown payload integers, including integers beyond Int64,
remain integers. Metadata dates stay strings.

`SourceToScene` requires an active registration and an available Cartesian common
frame. `RepresentationToSource` handles the separate representation mapping. Only
an exactly shared frame ID allows an omitted representation map to mean identity.
An omitted layer registration always means unregistered.

Affine evaluation reuses `AffineMap` and its existing numerical guard, preserving
row-major coefficients, shear, reflection and translation. It maps source tuples
directly to target tuples (`q = A p + t`); it does not apply a second unit conversion.
Both forward and inverse evaluation use doubles. Render-origin subtraction and
scene-units-to-metres conversion belong at the presentation boundary.

A valid document may contain unavailable operations, unresolved assets or an
unrenderable common frame. Unknown operation kinds/versions and required extensions
stay inspectable but block affected execution. Geodetic operations await a
qualified provider. Full WKT2/PROJJSON definitions remain intact; affine execution
also waits for CRS interpretation when those definitions are present. This avoids
silently choosing Cartesian convenience values over the supplied CRS. Known body
conflicts block execution. An explicit map from an unknown source body preserves
that unknown value; Earth, Moon, Mars and project body identifiers are not normalized.

Evidence retains separate origin, lineage, category, status and details fields.
`supplied`, `derived`, `independently-checked` and `unknown` are distinct declarations.
Neither a numeric field nor a field named `passed` becomes an accuracy claim.

## Revisions and optional state

Documents are immutable after parsing. To propose a revision, edit a detached
JSON object, parse it, and call `proposed.ValidateRevisionOf(previous)`. Scientific
changes require a different opaque revision. Reused frame and operation IDs cannot
change their definitions, and recorded registration solutions cannot change.
This checks the supplied predecessor; the caller retaining revision history must
also prevent reusing IDs removed in older revisions. No merge or history service
is introduced here.

Superseded solutions can retain their old target frame when a new revision adopts
a new common frame. Active registration endpoints must match the current layer
and scene; historical operations remain unchanged and their references valid.

`SceneSidecars` reads optional state separately and checks scene/layer/asset
references. It never fills missing scientific facts:

| Sidecar | Required fields |
|---|---|
| View v1 | `schemaVersion`, `sceneId`, its own `revision`, `layers`, `bookmarks` |
| View layer | `layerId`, `visible`, inert `style` object, nullable `selectedRepresentationId` |
| Portable bookmark | `id`, `name`, `frameId`, three-coordinate `position` in that declared frame |
| Private access v1 | `schemaVersion`, `sceneId`, `assets` map keyed by asset ID; each entry has unique `resolverKeys` |
| Legacy Unity session v1 | `schemaVersion`, `sceneId`, `kind: "schema1-unity-display"`, `display`, `resetDisplay` |

View and private maps may omit entries. Bookmarks have unique IDs. The legacy
session retains the original PascalCase display fields (`Origin`, `AxisBridge`,
`Pose`, `Scale`) and validates rigid placement/orthogonal basis as schema 1 did.
It is a compatibility sidecar, not a general XR session format. Future LOD, camera,
anchors and resource handles belong to their renderer's session, outside science.

Portable asset locators accept HTTPS without credentials, fragments or queries,
and package-relative paths without traversal, percent escapes or home/absolute
paths. All HTTPS query strings are excluded because this reader does not classify
signing schemes. Use private resolution for such URLs. Parsing never fetches a
locator, interprets a hash algorithm, or invokes a resolver.

## Explicit schema-1 migration

```csharp
SceneMigrationResult import = SceneMigration.FromSchema1(oldJson, newRevision);
import.WriteNewDirectory(newDirectory); // Fails if the target already exists.
```

The strict schema-1 reader remains available as `SceneJson.Read`. The v2 reader
never upgrades implicitly. Migration returns immutable science plus detached view,
legacy session, private access and report objects. `WriteNewDirectory` writes a
new directory atomically and cannot overwrite the original or an existing target.
Share `scene.json` independently; `access.private.json` belongs with private local
configuration, and `session.legacy-unity.json` may contain private room placement.

Migration preserves scene/layer identities and metadata. Inline frames deduplicate
only when the complete original definitions agree. Each opaque frame definition
contains `value.schema1` with the original `definition`, `body`, `axisConvention`
and `metresPerUnit`; Cartesian convenience fields copy explicit usable values.
Blank legacy body/format labels remain in legacy details while their typed values
become null, with a report note. No coordinate interpretation is inferred.

Matrices copy exactly to affine operations. Their declarations are `supplied`;
old provenance and validation JSON are retained as `legacy` statements with status
`unknown`. Missing registration stays null. No representation frame is invented
from a schema-1 asset: migration leaves the representation list empty.

Asset IDs are preserved where possible. Equal records with the same old ID share
an asset; conflicts receive generated IDs. All incoming asset IDs are reserved
first. Generated IDs are `schema1:<kind>:<sha256>` using lowercase SHA-256 of the
UTF-8 compact JSON array `[sceneId, sourceJsonPointer]`. If occupied, append
`:collision` until free. These hashes name migration paths; they do not identify
dataset bytes. The report lists every source path and old/new ID.

Unknown hash algorithms stay unknown: typed `contentHash` is null; original hash
and format strings are preserved under asset metadata `geox:schema1`. If occupied,
append `:legacy` until a free key is found and report the chosen key. Private
`AccessReferenceId` values move to the access map; public locators stay empty.

`LegacySourceToDisplay` checks parity with the old display calculation, including
the old unitless display fallback. That compatibility fallback supplies no physical
scale to the scientific document. Origin subtraction stays in double precision.

## Reproducing the checks

From the project root, with the pinned Unity installation available:

```sh
python3 scripts/generate_spatial_scene_fixtures.py
python3 scripts/test_spatial_headless.py
```

The headless command compiles the actual spatial core and its NUnit tests against
the bundled .NET runtime, JSON.NET and NUnit. It uses no Editor, Unity engine
assembly, license, package restore or network. `--unity-resources` selects another
installation location. Results are in `build/spatial-headless/results.xml`.

Unity runs the same spatial tests as Edit Mode tests. The fixtures outside Assets
are synthetic, authored examples with explicit expected arithmetic. The independent
C#/JavaScript conformance bead `.1.6` remains separate: passing these tests does not
establish an independent reader, provider qualification, Android build, hardware
behavior or scientific method acceptance.
