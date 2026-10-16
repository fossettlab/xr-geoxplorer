# Model-input policy and fixture preparation

**Draft; runtime enforcement and device limits are not implemented.** Bead
`geox-k1v.2.2` remains open. This document records the policy boundary and initial
fixture preparation, not a qualified importer or a Quest capacity claim.

## Initial loading boundary

The first public entry accepts HTTPS GLB without external resource references.
A GLB can contain external URIs under the
[Khronos specification](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#glb-file-format-specification);
format validity therefore does not establish acceptance by this initial route.
Reject such references clearly until the selected importer's dependency fetches
can all pass through the same policy. Keep local, platform-granted files on a
separate entry path; never interpret a public model URI as permission to read
arbitrary local files.

Validate every redirect before following it. Preserve HTTPS on public requests,
do not forward authentication across origins, and apply the eventual request
budget to both the root operation and every companion. Retained restricted Azure
content uses its explicit authorized adapter. Do not infer trusted bundle
eligibility from an arbitrary file's metadata.

Count actual received bytes even without Content-Length. Check declared and
decoded resource demands where the importer permits before expensive allocation.
Unsupported required extensions fail visibly. Native decoding that cannot be
interrupted must still remain owned until completion; cancelled/superseded
operations cannot publish late objects. Preserve the original layer state on
failure. Error details and logs must not retain credential-bearing URLs.

## Resource-budget decisions still needed

No numeric production budget is chosen here: the current Quest headroom and
selected importer's allocations have not been measured. Treat an unconfigured
budget as a missing release prerequisite, not an unlimited-download policy.

| Budget | Evidence needed before selecting a provisional value |
|---|---|
| Root and aggregate downloaded bytes | Available memory/storage during the actual workload, buffering and concurrent requests. |
| Decoded image dimensions and bytes | Decoder behavior, texture upload copies, mip generation and GPU headroom. |
| Geometry and node allocation | Accessor expansion, indices, hierarchy overhead, mesh duplication and collider work. |
| Request count, redirect count and concurrency | Importer resolver controls and representative companion-file workloads. |
| Connection, transfer and decode lifetime | Cancellation behavior and recoverable failure tests on device. |

Record each provisional setting, its test workload and rationale during the
importer trial/Quest baseline. A compact file can expand substantially; a
download-byte limit alone cannot establish a safe model envelope. The fixture
triangle below is a correctness probe, not a performance workload.

## Generated fixture inventory

[Fixture instructions](../tests/fixtures/model-inputs/README.md) explain
generation and hashes. The standard-library generator creates authored geometry,
PBR material, normals, UVs, hierarchy and a small RGBA image. It also writes
external/missing companions, unsupported-required-extension, truncated and
bad-magic cases. Its manifest distinguishes expected product behavior from
structural validity. Generated files stay outside `Assets/` so they have never
been imported into the Unity project.

Offline checks cover reproducibility, manifest hashes, no-overwrite behavior,
container/chunk lengths, buffer-view ranges, intentional malformed bytes, PNG
CRCs and its decoded authored pixel. Run from the repository root:

```bash
python3 -m unittest discover \
  -s tests -p test_model_input_fixtures.py
ruff check scripts/generate_model_input_fixtures.py \
  tests/test_model_input_fixtures.py
```

On 2026-09-09, the pinned official Khronos `gltf-validator` 2.0.0-dev.3.10 was
downloaded and checked against its registry integrity digest. All nine model
fixtures produced their expected validation outcomes. The five intended-valid
models have zero errors and warnings. Missing resources, truncation and bad
magic remain deliberate failures. The unknown required extension is reported
as `UNSUPPORTED_EXTENSION` at informational severity, so a zero-error count
alone must not admit it. See the [full validator receipt](contracts/model-input-validation-2026-09-09.json).

```bash
python3 scripts/fetch_gltf_trial_sources.py
node scripts/validate_model_input_fixtures.cjs
```

The download script verifies pinned archives and executes no npm installation
hooks. Validation resolves only the authored `pixel.png` and `geometry.bin`
companions after checking their manifest hashes. It never fetches remote data.
The earlier registry/network restriction was overcome in this authorized run;
no Unity rendering, runtime policy enforcement or Quest acceptance follows from
format validation alone.

## Local transport harness

`scripts/model_input_http_fixture.py` serves only the generated triangle bytes
on an OS-assigned loopback port. It checks the triangle against its manifest,
does not serve directories, and does not log request URLs. Its routes exercise
a complete response, a redirect, a redirect loop, absent Content-Length, a
truncated body and a stalled body. The stalled handler is released during
server teardown. The test socket deadline is test configuration, not an app
timeout recommendation.

```bash
python3 -m unittest discover \
  -s tests -p 'test_model_input*.py' -v
python3 scripts/model_input_http_fixture.py
```

Current result, 2026-09-09: all eleven tests pass with no skips when run with
authorized loopback socket access (five offline checks and six transport tests).
The earlier restricted run skipped the six socket cases; that earlier run is
not the evidence for this result. The server is intentionally
plain HTTP for a test transport; it establishes neither public HTTPS policy,
TLS behavior nor access from a headset. Do not loosen the app's public URL
policy to consume it.

## HTTPS, origin and budget policy

`scripts/model_input_fetch_policy.py` encodes the first public route: HTTPS
only, no `file:` URLs, validated redirects, no Authorization forwarding across
origins, counted bytes even without Content-Length, and an unconfigured budget
as a missing prerequisite rather than an unlimited download. Companion fetches
stay rejected on that first route. A later same-origin companion mode is
explicit and still refuses a different host. Error text uses redacted URLs.

Loopback HTTPS coverage uses a temporary `127.0.0.1` certificate from
`scripts/model_input_http_fixture.py`. That certificate is a test transport, not
a public CA or headset TLS result. Importer cancellation remains an importer
trial concern; closing a socket here only proves fetch-owner control.

```bash
python3 -m unittest discover -s tests -p 'test_model_input*.py' -v
ruff check scripts/model_input_fetch_policy.py \
  scripts/model_input_http_fixture.py \
  tests/test_model_input_fetch_policy.py
```

## Authored pressure workloads

`scripts/generate_model_input_workloads.py` writes a 16×16 grid, a 64×64
checker texture and a 32-node chain. The manifest records parameters and
measured file, vertex, decoded-RGBA and node sizes. These are desk measurements
of authored inputs. They are not Quest headroom, importer allocations or a
production limit.

```bash
python3 -m unittest discover -s tests -p test_model_input_workloads.py
```

## Representative licensed samples

`tools/gltf-sample-inventory/sources.json` pins Khronos Box (CC-BY-4.0, Cesium)
and Box Textured (CC-BY with Cesium trademark limits) at commit
`2bac6f8c57bf471df0d2a1e8a8ec023c7801dddf`. Binaries stay out of git. Fetch
them into `build/gltf-sample-inventory/` and verify hashes:

```bash
python3 scripts/fetch_gltf_sample_inventory.py
python3 -m unittest discover -s tests -p test_gltf_sample_inventory.py
```

Do not reuse the Cesium mark. These samples are not a catalog and not Quest
acceptance.

## Remaining fixture work

- Keep official validation outcomes distinct from importer support and product
  acceptance; preserve the existing deliberate failures.
- Quest resource budgets still need measured device headroom from the importer
  trial and the current baseline APK. No numeric production limit is chosen here.
- Complete importer and device load/unload, cancellation, material, transform
  and repeated-recovery checks on the isolated Android/Quest path. Fetch-owner
  socket cancellation is not importer cancellation. The coordinate contract
  governs scientific registration; this fixture generator does not select a
  scientific method.

## Reviewed desktop hardening, 2026-09-12

A missing or invalid byte budget fails before transport. Configured budgets must
be non-negative integers. Redirect and error response bodies count toward the
aggregate received total. Once a redirect crosses origins, authorization stays
removed even if a later redirect returns to the original origin; equivalent
default HTTPS ports count as the same origin. Malformed URLs and Content-Length
values produce named, redacted refusals.

The test transport callable returns a fully materialized response body. These
checks prove accounting and policy decisions, not an in-flight memory ceiling.
Streaming enforcement, decoded-resource limits and numeric Quest budgets still
belong to the runtime/device work in `.2.2`.
