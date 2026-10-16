# Desktop execution checkpoint — 2026-09-12

Status: desktop tranche complete; all three child beads closed. This tranche preserves the existing dirty
checkout and the Sept 11 importer artifact. No commits, pushes, production
package changes or headset execution are part of this checkpoint.

## Reachable work

The live graph's runtime gates do not prevent independent scientific-domain
work. Three child beads separate that preparation from parent acceptance:

| Bead | Scope | Evidence |
|---|---|---|
| `geox-k1v.1.6.1` | Independent C# and JavaScript scene readers | Shared authored conformance receipt; provider-free only |
| `geox-k1v.8.2.1` | Declared affine preview/apply/cancel/undo | Actual core tests, source unknowns and history preserved |
| `geox-k1v.2.1.3` | Fetch-policy and importer evidence hardening | Regression tests, isolated Editor and rebuilt APK |

Astra reviewed the domain commands, readers, input policy and trial evidence.
Sol implemented registration and policy fixes, Terra the independent reader
and Editor evidence binding, and Luna the C# CLI, recorder tests and usage docs.
The integration owner controls the isolated Unity Editor. Astra accepted the final
corrections with no remaining actionable findings in the reviewed scope.

## Verified domain behavior

The C# and JavaScript readers agree on 88 checks across 44 authored requests.
They preserve metadata integers beyond both Int64 and binary64 range, reject
malformed documents, and distinguish unavailable operations from invalid data.
The fixtures exercise affine forward/inverse, layer mapping, representation
identity, unknown operations, required extensions, geographic frames and
uninterpreted CRS definitions. No geodetic calculation or asset loading occurs.

The actual C# core suite passes 125 tests with no failures or skips, including
17 new declared-registration cases. Preview is detached; Apply creates a new
revision and solution; Cancel leaves the canonical document unchanged; Undo
restores the prior active solution without deleting history. No units, body,
calibration, accuracy or uncertainty are inferred.

Evidence: [shared conformance receipt](contracts/spatial-conformance-2026-09-12-final.json).

See [reader usage](../tools/spatial-conformance/README.md) and
[registration commands](declared-registration-commands.md).

## Importer and policy verification

All 31 input fixture/transport/policy tests and 14 recorder tests pass. Invalid
byte budgets and redirect limits fail before transport. Redirect/error bodies
count toward the operation budget; cross-origin redirects permanently remove
authorization. The callable still materializes a body before accounting, so
this is not an in-flight allocation ceiling.

Reproduce the Python checks with:

```bash
python3 -m unittest discover -s tests -p 'test_model_input*.py' -q
python3 -m unittest -q tests/test_record_gltf_importer_android.py tests/test_record_gltf_importer_review.py
python3 scripts/test_spatial_headless.py
```

Loopback socket permission is required for transport tests; skipped socket tests
are not equivalent evidence. Ruff passes for the changed Python tools/tests.
All changed C# files retain UTF-8 BOM and CRLF. The broad dirty-tree whitespace
check still reports pre-existing settings/configurator differences; those were
preserved rather than reformatted.

The isolated Unity 6000.4.4f1 project at
`/private/tmp/geox-gltf-trial-n78u5x07` compiled without Console errors and
passed all 129 Edit Mode tests (125 spatial and four importer tests). The
curated importer test records 11 cases and captures harness/fixture hashes
before and after execution. Its recorder rejects missing, altered or extra
source files. The complete pipeline status is retained as ancillary output;
the curated report has the direct test-time source binding.

[Editor receipt](contracts/gltf-importer-review-2026-09-12.json).
Reproduce with `scripts/prepare_gltf_importer_trial.py`, copy the Spatial source,
EditMode tests and `tests/fixtures/spatial` into that isolated project before
launch, then run the `GeoX` Edit Mode filter through Unity CLI. Record using
`scripts/record_gltf_importer_review.py --output <new-receipt-path>`.

The first temporary-project run omitted the spatial fixtures; Unity cached a
failed test-case discovery result. Supplying them and restarting the isolated
Editor resolved it. The final run above is the evidence, not those setup runs.
The trial Editor assembly now explicitly references the packaged JSON.NET DLL.

Player receipts start as `in_progress`, retain partial results and the active
case, and include a unique attempt ID and build/environment fields. Catchable
failures become `failed` with `success=false`. A hung native/import task remains
in progress; no unsafe abort or invented timeout is introduced. Storage failures
may still prevent receipt persistence and must be checked through logs.

## Verified Android artifact

APK: `build/gltf-importer-trial/GeoX-GltfTrial-2026-09-12.apk`  
SHA-256: `e06d7a3fae7813c8026e2ba1347b6ae380749159a207c7d782a82307b6bb5590`  
[Android receipt](contracts/gltf-importer-android-2026-09-12.json)

The build succeeded with zero errors. Its one warning says Pipeline is disabled
in Player because the trial scene has no RuntimePipelineManager; Editor access
remains separate from the trial app. Signature and 16 KiB ZIP alignment checks
pass. The APK contains only ARM64 native libraries, includes IL2CPP, and its
packaged fixtures match their manifest. All three required glTFast shader names
are in the build's included-shader evidence. Visual shader correctness still
requires the device run.

The recorder verifies the APK against its build-time hash and the harness against
both current repository and build-time hashes. Main application source/settings
hashes are unchanged across the isolated trial. The Sept 11 APK remains intact.
Rebuild through `GeoX.GltfTrial.ImporterBuild.BuildAndroid` in the isolated Editor,
then run `python3 scripts/record_gltf_importer_android.py --date <new-tag>`.
Existing artifact or receipt names are refused.

## Remaining gates

- Current Quest baseline: boot, stereo, tracking and existing content flows.
- Offline Android PROJ probe and provider disposition before geodetic implementation.
- Importer Player execution, rendered materials, lifetime and memory behavior;
  production importer adoption remains separate.
- Runtime streaming enforcement and measured resource budgets before public loading.
- Coordinate/runtime integration and contextual registration UI in their existing order.
- Measurement and estimation/uncertainty methods require their explicit review beads.

These domain results establish portable scientific meaning for the tested
subset. They do not qualify a web renderer, complete a scientific scene in XR,
or establish Quest performance. Additional adapters remain downstream.
