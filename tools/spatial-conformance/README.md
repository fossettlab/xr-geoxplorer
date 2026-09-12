# Provider-free spatial conformance

This directory contains two readers for the portable scientific scene v2
contract:

- `csharp/Program.cs` compiles the actual `GeoX.Spatial.SceneDocument` core
  outside the Unity Editor.
- `js/reader.mjs` is an independent JavaScript reader used for comparison.

Both readers parse one JSON request from standard input and write one JSON
response to standard output. They do not load Unity, fetch asset locators, call
network services, or invoke geodetic/CRS providers. They evaluate only a
declared affine operation when its explicit capability conditions are met.

## Prerequisites

The C# build uses the pinned Unity installation's Roslyn compiler, .NET runtime,
and JSON.NET assembly. Build it from the repository root:

```bash
python3 scripts/build_spatial_conformance.py
```

The script prints the executable command and writes
`build/spatial-conformance/csharp/command.json` as an `argv` array containing
absolute paths to `dotnet` and `SpatialConformance.dll`. The generated command
is the reproducible C# entry point; no package restore or Unity license is
needed.

The JavaScript reader requires Node.js 22 or newer. It uses the JSON reviver's
source text and `JSON.rawJSON` to preserve arbitrary-size integer lexemes in
the semantic document roundtrip. A runtime without `JSON.rawJSON` cannot
provide the same integer-preservation contract.

## Request and response

The minimum request has a scene object. A coordinate selector and point are
optional:

```json
{
  "document": { "schemaVersion": 2, "...": "scene v2 fields" },
  "operationId": "declared-placement",
  "point": [2, 3, 4],
  "inverse": false
}
```

Use either `operationId` or `layerId`, never both. `layerId` evaluates the
layer's active source-to-scene registration. Add `representationId` with a
`layerId` to evaluate that representation-to-source mapping; an omitted
representation mapping is an exact shared-frame identity. A point is required
for layer and representation evaluation. `inverse` requests the inverse map.

For example, run the built C# reader through its generated command:

```bash
python3 -c \
  'import json, subprocess, sys; \
   command=json.load(open("build/spatial-conformance/csharp/command.json"))["argv"]; \
   raise SystemExit(subprocess.run(command, input=sys.stdin.read(), text=True).returncode)' \
  <<'JSON'
{"document": {"...": "scene v2 object"}, "operationId": "declared-placement", "point": [2, 3, 4]}
JSON
```

Run the independent JavaScript reader directly:

```bash
node tools/spatial-conformance/js/cli.mjs <<'JSON'
{"document": {"...": "scene v2 object"}, "operationId": "declared-placement", "point": [2, 3, 4]}
JSON
```

Use an authored fixture or a complete scene v2 object in place of the
placeholder above. Both readers reject duplicate keys, trailing content,
non-JSON conveniences, invalid scene shape, dangling references, and
non-finite numeric values.

The response has `status: "valid"` and a semantic `document` roundtrip when
the request and scene parse successfully. When a selector is supplied, it also
has `available` and `reason`; an available evaluation uses `reason: null`, and
an unavailable capability reports a stable reason such as
`qualified_geodetic_provider_required`, `crs_interpretation_required`, or
`unsupported_required_extension`. A transformed `point` appears only when the
requested evaluation is available. `status: "invalid"` with an `error` means
the request or document itself was malformed; it is distinct from a valid
document whose requested operation is currently unavailable.

## Comparing the readers

Build the C# reader first, then run the shared authored cases:

```bash
python3 scripts/build_spatial_conformance.py
python3 scripts/run_spatial_conformance.py \
  --output docs/contracts/spatial-conformance-YYYY-MM-DD.json
```

The output path is opened exclusively; choose a new dated path for a new
receipt. The comparison checks semantic document equality and authored
availability/reason/point expectations. It is provider-free semantic
conformance evidence. It does not qualify geodetic transformations, interpret
WKT2 or PROJJSON definitions, establish scientific registration accuracy,
qualify a renderer, or demonstrate Unity/device behavior.
