# Review decisions and implementation pause

**Review phase complete; mechanical execution is paused at the user's request.**
Beads milestone `geox-k1v.9` records this bounded review goal. The desktop goal tool
could not create a replacement because the earlier implementation goal remains
unfinished and blocked; that goal has not been falsely marked complete.

GeoXplorer remains a scientific spatial framework for XR: Quest first, iOS later,
portable scientific meaning, interchangeable inputs and renderers, and quiet
contextual interaction. Keep modernization. No restart is needed.

## Decisions and evidence boundaries

| Review | Disposition | Remaining work |
|---|---|---|
| Portable scene/schema migration `.1.4` | Complete. One canonical scientific document; optional view/session/private access state; immutable frame/operation definitions; explicit migration. | `.1.5` implements it; `.1.6` supplies independent reader conformance. |
| OpenXR `.6` | Engineering choice complete: qualify exact 1.17.0 with effective timing/foveation preservation and the real-registry regression test. | Package/dependency/settings trial, then Android repair and current Quest baseline. Qualification bead stays open. |
| Importer `.2.1` | Engineering choice complete: glTFast 6.20.0 trial first; compare UnityGLTF only on a concrete failure. | Exact release/dependency/license capture, fixtures, controlled fetches, cleanup, materials, IL2CPP and device resources. Trial bead stays open. |
| CRS/provider `.7.1` | Engineering policy complete: PROJ candidate, explicit same-body operations/resources and unavailable-data behavior. | Native Android/provider qualification, sourced planetary conversion fixtures and numerical/domain checks. Qualification bead stays open. |
| Registration/evidence `.8.1` | Complete for supplied maps and explicit user declarations; original unknown metadata retained, immutable solutions, separate evidence categories. | `.8.2`/`.8.3` implement records and workflow. Estimation/propagation requires future investigator decision `.8.4`. |
| Opening/contextual UI `.2.3` | Interaction review complete. Preserve direct manipulation, contextual registration, recovery, progressive disclosure and scene/view separation. | Existing UI/rig/device tasks establish visual quality, bindings, readability and ergonomics. |

The concrete packets are [portable schema/migration](portable-scene-schema-review.md),
[runtime package trials](runtime-package-review-2026-09-07.md),
[geodetic operations and registration](geodetic-registration-review-2026-09-07.md),
and [Quest interaction](quest-open-flow.md). Source links sit beside externally
supported claims in those packets. Engineering review is not hardware acceptance
or approval of a new scientific analytical method.

## What was removed or simplified in the plan

- Remove required display/renderer state from scientific persistence; retain the
  existing affine math instead of replacing it.
- Use standard CRS/operation definitions instead of a custom projection language
  or geodesy engine. Missing resources/providers are explicit availability states.
- Qualify one importer first. Avoid two shipped importers, a generic plugin system,
  automatic cloud conversion or a new pairing service.
- Record declared registration first. Defer automatic fitting and uncertainty
  aggregation until there is a concrete scientific method to approve.
- Keep visual controls contextual. No permanent layer tree, technical inspector
  or obligatory CRS form for opening an ordinary model.

None of URP, modern rig/UI, networking/voice, anchors, passthrough or iOS was removed.
Atlas still means World Labs Atlas and remains a later adapter qualification.

## Mechanical queue after resuming

Use the live Beads graph and the [execution plan](beads-execution-plan.md). The
following order is explicit; readiness does not mean execution started.

1. **Integration owner:** run isolated OpenXR `.6` qualification. On an evidenced
   go, integrate the reviewed package/settings diff and apply the prepared Android
   repair `.5`. Compile/test, produce an identified build, then record Quest baseline
   `geox-3kj` and performance `geox-fzr`. Hardware absence leaves those acceptances open.
2. **Independent science lane:** implement `.1.5` from the reviewed schema and
   migration packet. Provider trial `.7.1` can run alongside it. Implement `.7.2`
   only after both succeed. Independent C#/JavaScript conformance `.1.6` and initial
   Unity coordinate integration `.7.3` can then overlap; `.7.3` also needs the baseline.
3. **Independent input lane:** importer `.2.1` and fixture/policy `.2.2` qualification.
   Keep all shared package/settings changes under the integration owner. Feed their
   receipts into GLB `.2.4`; the legacy adapter `.1.3` uses the same spatial boundary.
4. **Converge:** modern UI/rig work follows baseline/URP gates; declared registration
   `.8.2` then `.8.3` follows coordinate integration and the modern interaction layer.
   Scientific Quest acceptance `.3.6` still requires conformance, adapters, workflow
   and actual device evidence before broader inputs, splats or iOS expand.

This supports parallel contributors with isolated ownership; it does not require
launching extra agents. One person can traverse the same dependency order. Do not
create competing Beads databases or start package mutations in the integration
checkout while another lane owns its Editor. Uncommitted files need deliberate
transfer into isolated trial checkouts; HEAD alone is insufficient.

## Return to consequential review only for new evidence

- A candidate requires broader dependency/settings changes, vendor patching or
  relaxed loading controls, or fails its bounded trial.
- PROJ on Android cannot meet the offline boundary, or a real dataset requires
  grids, dynamic/cross-body operations or an unreviewed approximation.
- Registration or measurement needs a solver, uncertainty model, or scientific
  accuracy/acceptance threshold. `.8.4` and `.3.7` retain these method gates.
- The later networking spike requires a stack/service or sharing-policy decision.

These later decisions depend on evidence that does not exist yet; completing this
review phase does not manufacture their answers or remove their gates.

## Review verification

The review JSON Schema validates against Draft 2020-12 using installed jsonschema
4.26.0; the authored example and counterexamples behave as documented. Run:

```bash
python docs/contracts/check_review_contract.py
```

Use a Python environment with jsonschema available. This review used the existing
Anaconda environment and installed no dependency. Ruff passes for the review-check
script. Shape checks deliberately leave dangling references/singular maps to the
future semantic reader; they are not proof of production validation or migration.

The [PROJ desktop receipt](contracts/proj-desktop-review-2026-09-07.json) records
an explicit database/environment and a match to the published Cartesian example.
Planetary CRS lookup is preserved separately from unperformed planetary conversion.
No new Unity test, package trial, Android build or headset result is claimed.

Final verification compares the live dependency graph with the execution table,
checks local document links and whitespace, and compares all files under `Assets/`,
`Packages/` and `ProjectSettings/` against the start-of-review hashes. Existing
uncommitted work is retained. No commit, push or GitHub publication was performed
by this review phase. Stop here so the user can lower effort before execution.
