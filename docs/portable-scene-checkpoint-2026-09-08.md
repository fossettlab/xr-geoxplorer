# Portable scene implementation checkpoint — 2026-09-08

Bead `geox-k1v.1.5` is complete as a local, uncommitted software increment on
`codex/scientific-scene-foundation`. The
[implementation contract and commands](portable-scene-documents.md) and
[machine-readable evidence](portable-scene-checkpoint-2026-09-08.json) describe the
delivered boundary and identify tested sources.

The scientific v2 document is independent of renderer, loaded assets, display and
private resolver state. Strict parsing and semantic validation distinguish malformed
documents from valid but unavailable operations. Affine math remains unchanged;
geodetic definitions, planetary body labels and uninterpreted evidence survive
round trips without becoming executable or scientifically accepted by inference.

Schema-1 migration is explicit. It preserves matrices, definitions and legacy
evidence; separates visibility, display/reset and access keys; handles conflicting
asset IDs and occupied metadata keys; and refuses to overwrite an existing target.
No representation frame is inferred from an asset's format.

| Verification | Result |
|---|---|
| Headless C# / NUnit using bundled .NET runtime | 108 passed, zero failed/skipped/inconclusive |
| Full Unity 6000.4.4f1 Edit Mode suite | 131 passed, zero failed/skipped/inconclusive |
| Unity script compilation | Completed, no errors |
| Unity Console after tests | No error entries; capture not dropped |
| Existing Assets/Packages/ProjectSettings integrity | All 7,780 pre-existing file hashes unchanged |
| Spatial sources/tests and JSON fixtures in isolated Unity copy | Byte-for-byte match with workspace |

Unity ran in `/private/tmp/geox-portable-scene-w78oui8q`, keeping Editor-generated
state outside the working tree. The headless runner compiles the same spatial
sources/tests without Unity engine references or an Editor process.

The first headless trial exposed a test-side timestamp coercion in its reference
JSON parse. The comparison now explicitly keeps date text as strings. A separate
review removed an unnecessary same-endpoint requirement for superseded historical
solutions: a new common frame uses new records while old solutions retain their
original endpoints. Current active mappings still require exact layer/scene
endpoint consistency; cyclic history is rejected.

No package, prefab, scene, player setting or existing affine implementation changed.
No new APK was built. The previous Android checkpoint therefore remains evidence
for its original source snapshot and does not contain this new code. Quest
acceptance, geodetic-provider qualification and independent JavaScript conformance
remain open; no GitHub publication, commit or push occurred.

Next domain sequence: `.7.1` empirically qualifies the reviewed CRS provider, then
`.7.2` implements that supported operation subset. `.1.6` follows its existing
prerequisites for independent C#/JavaScript conformance. The Quest baseline and
importer trial retain their independent lanes and acceptance criteria.
