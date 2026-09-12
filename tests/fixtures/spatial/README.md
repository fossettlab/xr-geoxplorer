# Synthetic portable scientific scenes

Regenerate with `python3 scripts/generate_spatial_scene_fixtures.py` from the root.
The accepted review example supplies the base structure. All quantities are
authored test inputs, not observations or geodetic definitions selected for use.

`cases.json` lists valid documents and the expected availability of operation
`declared-placement`. The affine case uses `p = (2,3,4)` with a reflection, shear,
nonuniform scale and translation; hand arithmetic yields `(14,26,46)`. Unavailable
cases must round-trip without producing coordinates. CRS/provider fixtures are
deliberately unqualified and may only be inspected.

`schema1.json` exercises explicit import, display separation, conflicting asset IDs,
legacy metadata-key collision and validation JSON that must not become scientific
acceptance. Additional NUnit cases mutate these fixtures to check malformed input,
unknown units, the binary-exact large-origin example and migration overwrite refusal.

The C# checks run both headlessly and in Unity. These shared files are inputs for
the later independent C#/JavaScript conformance task, not proof of that task.
