# glTF importer trial — review completed, 2026-09-09

**Proceed with the isolated glTFast 6.20.0 trial using Unity's built-in
Collections 6.4.0.** The dependency review is complete. The corrected harness
compiles and passes one Unity Edit Mode test containing eleven cases, with no
skips. Bead `geox-k1v.2.1` remains in progress for qualification; this does not
adopt the importer into GeoXplorer or establish Android/Quest compatibility.

**Subsequent execution tranche:** four Unity Edit Mode tests now pass, with zero
failures or skips. Child beads `.2.1.1` (authored fidelity) and `.2.1.2`
(controlled cancellation and resource cleanup) complete the next bounded tranche.
See the [tranche receipt](contracts/gltf-importer-tranche-2026-09-09.json).

The [review receipt](contracts/gltf-importer-review-2026-09-09.json) records
the fresh run, source identities, dependency graph and license artifacts.
The [earlier receipt](contracts/gltf-importer-trial-2026-09-09.json) preserves
the initial failed test and interrupted rerun as historical evidence.

## Dependency disposition

The approved [runtime package review](runtime-package-review-2026-09-07.md)
requires unexpected dependency changes to return to review before adapter work.
Unity resolved a different Collections package from the registry manifest request:

| Dependency | glTFast 6.20.0 requests | Actual isolated resolution |
|---|---|---|
| Burst | 1.8.30 | 1.8.30 |
| Collections | 2.6.8 | **6.4.0, built into Unity 6000.4** |
| Mathematics | 1.3.3 | 1.3.3 |

The current application's lock has no Collections package. The installed
Collections 6.4.0 manifest declares Unity 6000.4 and brings additional package
dependencies recorded in the trial lock. No pin was forced and no integration
manifest was changed.

The installed Collections changelog explicitly records its move into Unity core
at 6.4.0. Unity's [6.4 release notes](https://unity.com/releases/editor/beta/6000.4.0b9)
also record the Collections version transition. Treating this as an arbitrary
major-version upgrade was an unnecessarily strong assumption. A forced downgrade
would add an unqualified configuration without evidence of a requirement failure.
Continue with the observed graph and qualify it on Android before adoption.

The resolved glTFast source matches the pinned registry archive: all code and
asset files are byte-identical. Of 826 archive files, the resolved cache omits
two signing artifacts (`.signature`, `.attestation.p7m`), and its `package.json`
adds only `_fingerprint`, matching the registry SHA-1. These differences are
recorded explicitly; no claim of whole-archive byte identity is made.

The receipt preserves glTFast's Apache-2.0 license file, the Collections and
Mathematics Unity Companion license files, and Burst's license and third-party
notices. This is an artifact inventory; final redistribution notices remain part
of adoption work. Keep these Unity dependencies in the rendering adapter. They
must not enter the portable scientific scene schema or determine its meaning.

## Review findings and fixes

1. **False rejection passes fixed.** Previously, any exception that prevented
   instantiation could pass an adverse fixture. Each case now requires its
   expected diagnostic; unexpected exceptions and cleanup exceptions fail.
   Successful cases also check diagnostics after instantiation. Unknown fixture
   names fail rather than inheriting generic rejection success.
2. **Edit Mode scheduler corrected and verified.** glTFast's default defer agent
   creates a persistent GameObject, invalid in Edit Mode. Injecting its public
   `UninterruptedDeferAgent` makes the small correctness probe valid. This says
   nothing about scheduling or performance in a Player.
3. **Coverage claims narrowed.** Case identifiers distinguish retry and
   cancellation requested before load or instantiation. They do not describe
   cancellation during work. Calling `Dispose` without an exception also does
   not prove that all resources were reclaimed.

The trial confirms that both denied external-image fixtures return `loaded=true`
while logging `TextureDownloadFailed` and `TextureLoadFailed`. Our harness
correctly withholds instantiation. This validates the existing requirement to
check diagnostics and companion requests before presenting a scene. Importer
success alone is insufficient.

## Evidence completed

- All eleven existing Python fixture/loopback transport tests pass, without
  skips, with authorized local socket access.
- The official Khronos validator 2.0.0-dev.3.10 produces the expected outcomes
  for all nine model fixtures. Five intended-valid models have zero errors and
  warnings; intentional bad inputs remain bad. Unknown required extensions are
  informational to this validator and still require explicit importer rejection.
- The importer and validator archives were fetched from their official registries
  and checked against published integrity metadata; SHA-256 pins are in
  `tools/gltf-importer-trial/sources.json`. No npm install hooks ran.
- Unity 6000.4.4f1 compiled the corrected harness in a fresh isolated project.
  Before testing, the Console had no errors. Afterward its only error was the
  exact `JsonParsingFailed` message expected by the malformed-input test.
- Eleven importer cases pass: triangle, hierarchy, embedded texture, two denied
  companion images, unknown required extension, truncated GLB, bad magic, a
  successful retry, and cancellation requested before load and instantiation.
  Successful imports each produce one mesh with three vertices and report
  `glTF/PbrMetallicRoughness`; this is not a visual shader qualification.
- All 7,796 pre-existing application source/settings files retain their starting
  hashes. The main project's Packages, settings, scenes and prefabs are untouched.

The [validator receipt](contracts/model-input-validation-2026-09-09.json) records
all findings. Validation permits only the hash-checked authored companions;
it does not fetch network resources or establish production URL policy.

## Earlier interruption and remaining limits

The first test failed at importer construction, before reading a model. Its
default defer agent creates a persistent `glTF-StableFramerate` GameObject and
calls `DontDestroyOnLoad`, which is invalid in Edit Mode. This is a harness
configuration failure, not evidence that glTFast cannot import the fixture.

The previous project's Pipeline connection became unreachable during the
correction attempt, and its owned Editor was terminated normally. The review
used a fresh project, `/private/tmp/geox-gltf-trial-a2sqdxjz`, and obtained a
completed passing test result. The connection failure is not attributed to
Collections; no causal evidence supports that claim.

The original harness records load/instantiate booleans, diagnostic codes, denied
companion requests, mesh/vertex counts, and cancellation requested before work.
The additional qualification tests below extend that evidence. Time-budget
scheduling, visual rendering, interruption inside native decode jobs, exhaustive
resource-leak freedom and Player lifetime behavior remain unqualified.
No production parser, fetcher, registration method or format adapter was added.

## Completed execution tranche

`tools/gltf-importer-trial/unity/Editor/QualificationTests.cs` adds three tests
alongside the original eleven-case test:

- Authored fidelity checks positions, normals, winding, UVs, mesh bounds,
  hierarchy world positions and scale, metallic/roughness/culling values, shader
  identity, and the exact embedded RGBA pixel. Expected data comes from
  `scripts/generate_model_input_fixtures.py`; X reflection, UV-Y reflection and
  winding reversal are the inspected glTFast representation conversion. They
  are not scientific registration or a newly selected CRS transformation.
- Load cancellation occurs at the first armed importer defer checkpoint after
  work starts. The defer agent yields, requests cancellation, and lets the
  importer observe its own token. A subsequent fresh import passes fidelity.
- Hierarchy cancellation occurs after at least one child object exists. The
  partial hierarchy is destroyed, and a fresh import passes fidelity.

Each probe tracks available meshes, materials and textures. Instance destruction
must leave their liveness unchanged; after importer disposal, every tracked
resource must compare as destroyed. Successful and instantiation-cancelled probes
require nonempty resource tracking. This is an Edit Mode ownership check, not a
global allocation census. Textures are explicitly readable for pixel assertions;
that diagnostic setting is not a production memory recommendation. The named
float tolerance is an arithmetic test tolerance, not scientific uncertainty.

The first launch crashed before tests ran, with a native crash stack including
Burst compiler code. Restarting the same project/configuration succeeded; the
cause is unresolved. The first test run passed three tests and failed one because
the cleanup assertion assumed all referenced resources were still alive after
load cancellation. Cancellation may already release resources. The corrected
assertion compares liveness immediately before and after instance destruction,
and still requires all tracked resources destroyed after disposal. The
[initial receipt](contracts/gltf-importer-tranche-initial-2026-09-09.json)
preserves that failure; the final receipt records four passes and unchanged
hashes for all 7,796 pre-existing application files.

## Reproduce the review evidence

```bash
python3 scripts/fetch_gltf_trial_sources.py
node scripts/validate_model_input_fixtures.cjs
python3 scripts/prepare_gltf_importer_trial.py
```

Preparation creates a fresh temporary project and prints its path. Its manifest
pins glTFast 6.20.0; Unity resolves its dependencies. Fixture files are copied as
`.bytes` so the Editor does not automatically import deliberately malformed GLBs.
Compare the archive and resolved source identities before using source inspection
as evidence for the tested release.

Open the printed project with official `unity open` in automated mode. Inspect
`unity list`, check compilation/Console, then run
`run_tests --mode editor --filter GeoX.GltfTrial --async_tests true` and poll
`test_status`. Preserve every failure and result, then run the recorder with a
new output filename:

```bash
python3 scripts/record_gltf_importer_review.py \
  --output build/gltf-importer-trial/review-rerun.json
```

The recorder checks that trial and repository harness sources match, compares
application hashes, and captures the actual test result. It refuses to overwrite
an earlier receipt and does not infer adoption from a test pass. Ruff passes for
the recorder. C# compilation and the relevant Unity test pass; standalone
`dotnet run` lint could not execute because this shell has no .NET SDK command.

## Android IL2CPP build, 2026-09-11

The isolated ARM64 IL2CPP APK is built and verified. See the
[Android checkpoint](gltf-importer-android-checkpoint-2026-09-11.md).
Quest launch, player receipt retrieval and resource measurements remain open.
Keep `.2.1` in progress until those exist and a candidate recommendation is
recorded. Production packages are unchanged.

## Next mechanical work, in order

1. Sideload `build/gltf-importer-trial/GeoX-GltfTrial-2026-09-12.apk` on Quest.
   Retrieve `android-importer-receipt.json` and record device identity, shader
   names and any load-time or memory notes. Identify URP separately when it is
   qualified.
2. Extend Player ownership/cancellation and representative workload evidence on
   device; keep the rejecting companion provider. The download interface itself
   has no cancellation parameter, so the fetch owner must control request lifetime.
3. Record the candidate recommendation before closing `.2.1`. Production adapter
   `.2.4` retains its existing baseline, fixture and coordinate-boundary blockers.

Fixture/transport work `.2.2` may continue independently. There is no reason to
add a second importer or change the scientific framework sequence. The two
child beads subdivide the existing qualification scope. No application package, setting, prefab or scene was
changed during this review. No commit, push or GitHub publication occurred.

## Desktop evidence hardening, 2026-09-12

Child `.2.1.3` tightens the existing candidate without introducing another
importer. All required glTFast shaders must be present, positive cases check the
observed material shader, and negative cases require the exact expected load,
instantiation, cancellation and diagnostic outcome.

Curated Editor reports now bind results to harness and fixture hashes captured
before and after testing. The recorder checks complete tree equality and
refuses unbound or stale reports. Android build receipts bind the built APK hash
to the build-time harness identity and use new artifact names exclusively.
The Player writes an initial and per-attempt progress receipt, retains partial
results, and distinguishes failure from success even when persistence fails.
A hung import remains in progress; on-device lifetime qualification is still open.

See the [September 12 checkpoint](no-headset-checkpoint-2026-09-12.md) for the
latest verified APK and tests. That artifact supersedes the September 11 trial
for the next headset run; the old APK and receipt remain historical evidence.
