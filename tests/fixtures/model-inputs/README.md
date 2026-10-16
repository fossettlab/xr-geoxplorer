# Model-input fixtures

Generate the small authored correctness set from the repository root:

```bash
python3 scripts/generate_model_input_fixtures.py
```

The default output is the ignored `generated/` directory here. The generator
refuses to overwrite an existing directory. Use `--output` with a fresh scratch
directory when regenerating for comparison. Each output has a byte count,
SHA-256 and expected behavior in `manifest.json`; the script records its zlib
runtime because PNG compression participates in the byte identity.

The triangle, hierarchy, material and pixel image are authored test data. They
are not scientific measurements or a device workload benchmark. No external
model or private research asset is copied. The repository does not declare a
general redistribution license for these new fixtures; the inventory records
that fact rather than guessing one.

The GLB writer follows the [Khronos glTF specification](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#glb-file-format-specification).
The embedded RGBA image follows the [PNG specification](https://www.w3.org/TR/png/).
This offline fixture writer is not a runtime parser. Validate good files with
the [official validator](https://github.com/KhronosGroup/glTF-Validator/tree/main/node)
and test actual behavior with the chosen importer on Quest.

Files marked adverse deliberately contain malformed bytes, unresolved resources
or an unsupported required extension. Do not “repair” those outputs to make an
unqualified all-files-pass check succeed. `external-image.glb` is structurally
valid with its companion; the planned initial self-contained-only input policy
still rejects it. Format validity and product acceptance are separate checks.

See [the loading-policy draft](../../../docs/model-input-policy.md) for HTTPS
policy tests, authored pressure workloads, the Khronos sample inventory and the
remaining Quest budget work. No generated asset should be presented as a model
already proven to load in this Unity application.

`scripts/generate_model_input_workloads.py` writes a separate ignored
`workloads/` directory. Those files measure authored expansion; they are not
device budgets.

`scripts/model_input_http_fixture.py` provides loopback transport fault routes
for the generated triangle. On 2026-09-09 all eleven fixture/transport tests
passed with authorized loopback access. Official Khronos validation also matched
the expected outcomes for all nine models; see the policy and its linked receipt.
See the policy document for the exact
coverage and limitations. Plain HTTP fixture access is not permission to relax
the production HTTPS requirement.
