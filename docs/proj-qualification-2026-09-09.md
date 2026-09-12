# PROJ qualification checkpoint — 2026-09-09

`geox-k1v.7.1` has passing desktop and Unity bridge evidence and a verified
Android ARM64 IL2CPP probe APK. **Quest execution is still pending.** No headset
was connected during this run. Keep `.7.1` open and `.7.2` behind its dependency.
This is a local qualification increment, not production provider adoption.

The [build and verification receipt](contracts/proj-qualification-2026-09-09.json)
identifies the artifacts, toolchain, resources and source files. The
[reference results](contracts/proj-reference-results-2026-09-09.json) contain the
inputs, independent expected values, tolerances, complete CRS definitions,
selected operations and observed outputs.

## What passed

| Check | Evidence |
|---|---|
| Pinned native builds | PROJ 9.8.1 and statically linked SQLite 3.53.4 build on macOS and Android ARM64 using Unity's NDK r27c. |
| Desktop cases | All 16 cases pass; the same suite passes again after a missing-database failure. |
| Numerical references | Published Earth Cartesian and UTM values; four analytically calculated Moon/Mars spherical projection cases. |
| Failure behavior | Wrong axis declaration, missing/null height, missing ordinate, out-of-domain latitude, conflicting body, unknown CRS, dynamic frame, missing grid and missing database fail explicitly. |
| Ownership | Each successful suite repeats 100 context lifecycles. C# passes borrowed UTF-8 paths and receives an integer result; native contexts/objects are destroyed within the call. |
| Unity bridge | One Edit Mode integration test passes, exercising the complete suite, missing database and recovery through P/Invoke. No Console errors before the build. |
| Android build | Unity 6000.4.4f1 succeeds with zero build errors and one expected warning that Pipeline is absent from the player. |
| APK | Signature verifies; only ARM64 libraries; IL2CPP present; packaged resources match their hashes; APK and native load segments pass 16 KB alignment checks. |
| Native ABI | The packaged PROJ shim exports only `geox_proj_probe_run`; its dynamic dependencies are Android's `libm`, `libdl` and `libc`. |
| Application integrity | All 7,796 pre-existing Assets/Packages/ProjectSettings files retain their starting hashes. |

The APK is `build/proj-qualification/GeoX-ProjQualification-2026-09-09.apk`.
It is a separate development application, `edu.wustl.fossett.geoxprojqualification`,
with a minimal Android status display. It does not replace GeoXplorer or qualify
XR rendering, tracking, interaction, headset performance or the existing Quest baseline.

The Editor log also contains host Metal-toolchain errors during shader preparation.
The Android BuildReport succeeds with zero errors; the probe makes no claim about
macOS graphics. Its desktop numerical and managed-bridge tests passed before the build.

## Provider and scientific limits

The native build disables CURL/network support and TIFF resources. Every probe
context explicitly selects the bundled database/search directory and disables
network access. Desktop calls pass despite deliberately invalid ambient
`PROJ_DATA` and `PROJ_NETWORK=ON`. The APK still has Unity's Internet permission;
that permission is not an offline-execution result. Run the device test with
network connectivity disabled.

Both native bundles use the identical database, SHA-256
`ba59d662d0e3e4d46b3b43e72dba5d25b8bbd1e1441c80f03cb70572aa8862b4`.
It reports EPSG v12.029, dated 2025-10-02. This differs from the earlier Homebrew
9.8.0 probe: upstream 9.8.1 deliberately reverted EPSG content from v12.049 to
address regressions. Keep binary and database identities together; the patch
number alone is insufficient. [PROJ release notes](https://proj.org/en/stable/news.html).

The bundle carries PROJ's COPYING notice, the bundled nlohmann/json and wrapper
notices, and SQLite's source notice. Exact source archive hashes and the published
SQLite SHA3 digest are pinned in `tools/proj-qualification/sources.json`.
No optional grid files are bundled or qualified.

The Earth Cartesian fixture explicitly maps latitude/longitude/ellipsoidal height
in EPSG:4937 to EPSG:4936 using their common GRS80 ellipsoid. The independent
expected XYZ values come from the published conversion example. The UTM fixture
uses EPSG:4258 and EPSG:25832 and the published zone-32 GRS80 example.
[Cartesian example](https://proj.org/en/stable/operations/conversions/cart.html),
[UTM example](https://proj.org/en/stable/operations/projections/utm.html).

Planetary fixtures use the explicit IAU_2015 spherical definitions 30100/30110
(Moon) and 49900/49910 (Mars). Their radii are 1,737,400 m and 3,396,190 m.
Expected projection coordinates are calculated independently as
`x = R × longitude_radians`, `y = R × latitude_radians` for zero central meridian,
origin and standard parallel. These are horizontal-only, east-positive spherical
cases. They do not qualify planetary heights, ellipsoidal or planetocentric
variants, west-positive longitude, vertical datums, registration or cross-body
conversion. Preserve those definitions as unavailable until separately tested.
[USGS Moon definitions](https://psdi.astrogeology.usgs.gov/moon/standards/data_examples/),
[Mars registry](https://voparis-vespa-crs.obspm.fr/web/mars.html),
[projection formula](https://proj.org/en/stable/operations/projections/eqc.html).

Published Earth outputs are rounded, so their forward tolerances are one unit
of the published last decimal: 0.0001 m and 0.01 m. Analytic projection tolerances
use a named binary64 regression margin of `64 × epsilon × coordinate scale`.
Inverse checks are additional arithmetic checks, not independent scientific
validation. Provider accuracy and dataset accuracy remain unknown in these
results. No threshold is a positional accuracy claim or a registration tolerance.

The harness respects declared axes, verifies WKT2/PROJJSON CRS equivalence,
records selected operation definitions and reloads the exact selected PROJ
pipeline for evaluation. The operation WKT2/PROJJSON exports are recorded as
descriptions; standalone evaluation of those exports was not tested. A production
adapter must preserve the complete executable operation and explicit endpoint
axes/units, rather than assuming every exported conversion object contains its
axis/unit boundary steps.

The domain check uses declared fixture envelopes. Provider area-of-use metadata
is recorded where available and stays null otherwise. This does not qualify
automatic area selection, antimeridian wrapping or arbitrary user-input handling.
All fixtures and native operation strings are controlled qualification inputs;
this harness is not a safe general-purpose interpreter for scene-supplied PROJ
strings. Dynamic frames are deliberately rejected by this initial subset.

## Reproduction and remaining device step

From the repository root, with the pinned Unity/Android installation and a CMake
and native C/C++ toolchain available, first create a starting integrity map with
`python3 scripts/record_proj_qualification.py --snapshot --date YYYY-MM-DD`,
substituting the actual run date. The snapshot command refuses to overwrite an
existing map. Then run:

```bash
python3 scripts/build_proj_qualification.py --target macos --download
python3 scripts/build_proj_qualification.py --target android --download
python3 scripts/run_proj_qualification.py
python3 scripts/prepare_proj_unity_trial.py
```

The preparation command prints a fresh temporary Unity project. Open that exact
path with the official `unity open` CLI in automated mode; inspect `unity list`.
Run `run_tests --mode editor --async_tests true`, poll `test_status`, and check
`console --level error`. Preserve those CLI JSON results as
`build/proj-qualification/unity-editor-tests.json` and `unity-editor-console.json`.
Invoke `GeoX.ProjQualification.ProbeBuild.BuildAndroid` through registered `eval`
after the tests pass. The method exits the isolated Editor after writing its
build result. It refuses to run without the qualification-project marker.

`scripts/record_proj_qualification.py --date 2026-09-09` verifies the APK and
creates this checkpoint's JSON receipts. Its starting application hash map is
`build/proj-qualification/source-start-2026-09-09.json`. On a fresh run, create
a new map/date before working; do not borrow this run's integrity assertion.
Scripts and harness sources are currently uncommitted.

With a USB-debugging-authorized Quest connected, select its actual serial in adb,
disable network connectivity on the headset, and install this separate APK.
Launch `edu.wustl.fossett.geoxprojqualification/com.unity3d.player.UnityPlayerGameActivity`.
The player copies only packaged local resources, runs the suite, tests a missing
database and reruns the valid suite. Reports are under the app's
`Application.persistentDataPath`, normally
`/storage/emulated/0/Android/data/edu.wustl.fossett.geoxprojqualification/files/proj-qualification/`
on Android. Confirm the path on the device, then retrieve files with adb or
`run-as` using that full external-files path. Do not assume the internal
`/data/data/.../files` directory.
[Unity persistent-data path](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Application-persistentDataPath.html).
Retrieve:

- `android-managed-receipt.json`
- `android-report.json`
- `android-missing-database-report.json`
- `android-recovery-report.json`

Verify all case outcomes, resource hashes and the managed receipt against the
recorded APK and fixtures; record device/OS identity and the offline conditions.
A successful install or launch alone does not close `.7.1`. The repeated
lifecycles are a smoke test, not a memory-leak or memory-pressure assessment.
After the device evidence, record the provider disposition before starting `.7.2`.
Return to review if results require a different provider, operation policy or
resource strategy. No commit, push or GitHub publication occurred in this increment.
