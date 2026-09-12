# Portable scientific scene: reviewed contract

Engineering disposition: accepted for `geox-k1v.1.5` on 2026-09-07.
This is a review artifact, not an implemented serializer or published standard.
It extends the [accepted Cartesian contract](scientific-scene-contract.md).
The [review schema](contracts/scene-v2.review.schema.json) and
[authored example](contracts/scene-v2.review.example.json) make the decision
concrete. The example is synthetic; its identity matrix defines a declared
relation and supplies no observation, physical calibration or accuracy claim.

## Smallest useful document

Use one scene document with tables of frames, assets, operations, registration
solutions and layers. Separate tables allow multiple representations to refer to
the same source without copying its scientific definition. Do not add a general
scene graph, plugin registry, service container, event log or provider marketplace.

| Record | Required meaning |
|---|---|
| Scene | Schema version, stable ID, opaque revision, common frame ID, record tables, metadata and versioned extensions. No display state required. |
| Frame | Stable ID; explicit body or null; kind; original definition and its encoding. A Cartesian frame may declare axes and a common metres-per-unit factor. |
| Asset | Stable ID, optional content hash and media format, optional portable locators, metadata/evidence. It remains valid without a locator or downloaded bytes. |
| Operation | Stable ID, kind and operation version, source and target frame IDs, typed payload, evidence. Affine is one kind; geodetic operations retain a standard operation definition. |
| Registration solution | Stable ID, operation ID, optional superseded solution ID, evidence. Immutable once recorded. |
| Layer | Stable ID/name, original source frame ID, nullable active solution ID, source asset IDs, representations, metadata/evidence. |
| Representation | Stable ID, asset ID, representation frame ID, nullable representation-to-source operation ID. Geometry format does not determine scientific layer identity. |

Null body means unknown. A known body uses its supplied identifier (for example
Earth, Moon, Mars, or a project-defined body); a label alone does not select an
ellipsoid. Unknown frames/units may be persisted and previewed without pretending
they can be overlaid scientifically. Preserve full WKT2/PROJJSON when supplied;
an authority code alone is insufficient for reproducible interpretation. Opaque
definitions remain opaque. Do not parse old free-text axes by guessing.

## Coordinate and operation semantics

An operation maps a tuple in its source frame's declared axis order and units
to a tuple in its target frame's declared axis order and units. Affine version 1
uses a row-major 3-by-3 linear array and a translation vector: `q = A p + t`.
This is the existing core convention. Its scale includes any source-to-target
unit/calibration relation; do not apply that conversion again after evaluation.
Conversion from scene units to render metres occurs only at the display boundary.
Preserve shear/reflection from supplied maps; do not decompose them into Unity TRS.

Representation-to-source and source-to-scene are distinct operations. A missing
representation mapping means unresolved unless the frame IDs are identical;
that exact identity case needs no redundant operation. A null registration means
unregistered even when a preview happens to look aligned. Importer basis conversion
must be explicit in the adapter contract so it is neither omitted nor applied twice.

Geodetic version 1 carries a full selected operation definition, provider/version,
database identity, resource identities, domain description and inverse availability.
It has a distinct payload from affine. Ordered concatenations belong inside the
standard operation definition; avoid a second GeoXplorer pipeline language.
Do not execute a source-supplied PROJ string, filesystem path or grid reference
before the qualified provider checks it against the allowed operation/resource set.

Scientific evaluation stays in double precision. The render adapter subtracts
one shared origin in double precision before float conversion, applies the render
basis and XR placement, and returns inverse pick mappings only where supported.
The common scene frame can be stored even if a reader cannot render it. Runtime
support for a Cartesian/local scene does not justify rejecting the whole document.

Frame and operation IDs identify immutable definitions across revisions. A changed
definition gets a new ID and explicit dependent mappings; otherwise historical
registration solutions would silently change meaning. If a Cartesian convenience
field disagrees with an interpreted CRS definition, fail evaluation and report the
conflict rather than choosing one silently.

## Reading, validation and extension behavior

There are three separate outcomes: malformed document; valid but unavailable
capability/resource; executable operation. Only the last permits derived coordinates.
An unknown operation version can round-trip as a record without becoming identity.
An unknown required extension makes the affected document unavailable for execution;
an unknown optional extension is preserved. All extension envelopes have a version,
a required flag and JSON payload. Schema versions are integers; object revisions
are opaque strings, avoiding cross-language large-integer assumptions.

JSON Schema checks shape only. `.1.5` must also reject duplicate JSON properties,
non-finite numbers, unknown core fields, duplicate IDs in each table, dangling
references and conflicting definitions for a shared frame ID. Validate operation
endpoints, solution/layer consistency, representation mappings, acyclic solution
history and supported affine invertibility using the existing numerical guard.
An operation between the same frame ID must be identity. A registration must map
the layer's original source frame to the scene's common frame. Known body conflicts
fail execution; unknown source body may be related by an explicit declared solution
without rewriting that original unknown value. Do not normalize celestial names
into a guessed registry identifier. Unsupported definitions remain inspectable.

JSON decoding must preserve unknown payload values, including large metadata
integers, without JavaScript rounding them silently. Compare semantic round trips,
not whitespace or property order. Scientific coordinates use finite binary64;
identity/version fields never depend on numeric coercion. Arbitrary metadata is
inert data, not authority to execute instructions or fetch resources.

## Evidence without an invented error model

Evidence is a small envelope: origin categories, lineage records and statements.
Origin can include measured, reconstructed, interpreted, generated and mixed;
an empty list means unknown. Lineage preserves supplied source/process/version
information. A statement has a category, supplied/derived/independently-checked/
unknown status and its original details. Typed categories distinguish source
accuracy, fit residual, operation accuracy, tracking and representation error.
Numerical interpretation requires an explicit value, unit, frame, basis and method
in its details; otherwise preserve it as uninterpreted information. Do not reduce
these statements to a single quality score. See the
[registration review](geodetic-registration-review-2026-09-07.md).

## Scientific, presentation and private state

Changes to frames, asset identity, operations, active registration or scientific
metadata create a new scene revision. View changes do not. No distributed merge
algorithm is selected; networking will exchange expected scene revisions and
handle conflicts explicitly in its own reviewed protocol.

An optional view sidecar has its own version/revision, scene ID, layer visibility
and style, selected representation, and portable bookmarks when meaningful.
XR pose, render origin/basis, current LOD, camera, room anchors and resource handles
belong to a renderer/session sidecar. The scientific JSON loads without either.
The full schema for these sidecars is implementation work; none can supply missing
scientific facts during load. Reset means a view operation unless explicitly
invoked from the registration workflow.

Portable locators may be credential-free HTTPS or safe package-relative paths;
resolution is optional and never automatic during parsing. No home-directory paths,
traversal, embedded credentials or signed download queries in portable locators.
Private resolver keys and access grants live in an optional private access map keyed
by asset ID. A hash verifies fetched bytes only when its algorithm and value are
known. Missing hashes/locators are unknown, not made-up identifiers or fetch errors.
Opening and metadata inspection must work offline with unresolved assets.

## Explicit migration from experimental schema 1

Keep the strict schema-1 reader. Migration is an explicit import action that writes
new files and never overwrites the original. Reject unsupported versions. Return
the scientific document, optional view/session/access sidecars and a migration
report with old/new identities, preserved values and unresolved interpretation.

| Schema-1 field | Migration |
|---|---|
| Scene ID, layer IDs/name/metadata | Preserve exactly; assign a new documented revision for migration. |
| Inline scene/source frames | Deduplicate equal definitions by ID; conflicting same-ID definitions are an error. Preserve original definition, body, axis string and unit factor. |
| `SourceToScene` | Copy the exact matrix/translation to an affine operation and a supplied solution. No fitting, unit rescaling or validation upgrade. |
| Missing registration | Keep null. |
| `Visible` | Move to optional view state. |
| `Display`, `ResetDisplay` | Preserve together in a versioned legacy Unity session sidecar, including origin/basis; no room/display pose in canonical science. |
| Asset ID/hash/format/metadata | Preserve. If the old hash has no explicit algorithm, retain its original string under legacy metadata and leave the typed hash null; never guess an algorithm. Schema 1 only guaranteed asset IDs unique within a layer; if duplicates conflict across layers, issue new deterministic IDs and report the old-to-new mapping. |
| `AccessReferenceId` | Move to the private access map. No invented public URL. |
| Provenance/validation JSON | Preserve verbatim as uninterpreted legacy evidence details; do not promote validation status from key names. |

Generate new record IDs deterministically from source identity/path using a
documented collision-checked mapping; do not derive scientific equivalence from
display names. Keep schema-1 tests as migration fixtures. Check equal source-to-scene
and source-to-display results before/after migration, including unknown units,
nonuniform scales/shear, unresolved assets and the existing large-origin example.

## First-principles disposition

Keep the affine math and strict-reader discipline. Remove required presentation
from scientific persistence. Avoid per-renderer scientific models and duplicated
CRS notation. Retain minimal typed evidence now because deferring it would make
unknown and validated data indistinguishable. Detailed fitting, propagation,
collaboration conflict resolution and a universal scene graph are unnecessary
for this implementation increment.

Mechanical acceptance: `.1.5` implements this contract and semantic/migration
checks; `.1.6` independently exercises C# and JavaScript on the same fixtures.
Review-schema validation is not proof that those readers or Unity integration exist.
