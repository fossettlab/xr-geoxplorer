# Async hygiene preliminary audit

**Pre-rewrite source inspection; not final acceptance of #28.** Inspected
`a87c7d7` on `codex/scientific-scene-foundation`; no C# changes were made.
The live [issue #28](https://github.com/fossettlab/xr-geoxplorer/issues/28)
requires its final audit after #17 and #23. The local plan omitted those
blocking edges; they are restored following this preliminary inspection.

## Existing fixes confirmed

- `CreateASA.Start` and `FindASA.Start` are ordinary void lifecycle methods.
  Their `RunInitializeAsync` tasks await initialization inside exception handlers.
- An exact search across `Assets/Scripts/*.cs` finds no `async void`,
  `Task.Run(...)`, `Task.Factory.StartNew(...)` or `Thread.Start(...)` matches.
  Source inspection is still required: absence of these spellings does not
  prove lifecycle safety or absence of other thread-launch forms.
- `AnchorExchanger` has a static readonly shared `HttpClient`, with no per-call
  construction in its request methods. Its polling loop and delay inspect a
  cancellation token.
- [concurrency-model.md](concurrency-model.md) already documents the async Task
  default, coroutine exceptions, cancellation and singleton-client conventions.
  No new async dependency or repeated modernization rewrite is needed here.

## Residual lifecycle work for the anchor rewrite

| Location | Observed limitation | Required follow-through |
|---|---|---|
| `CreateASA.cs`, readiness and upload-result loops | Delays have no lifetime cancellation. Initialization may resume after destruction or wait for a completion callback that cannot arrive. | The #17 replacement must cancel waits and prevent late UI/object writes when its owner exits. |
| `FindASA.cs`, initial-list and refresh-result loops | Delays likewise lack lifetime cancellation. The task's outer catch is error handling, not cancellation of pending work. | Include pending lookup/refresh teardown in #17 regression checks. |
| `AnchorExchanger.cs`, poll request | `StopWatching` cancels the loop/delay, but `RetrieveLastAnchorKey` does not pass that token into its HTTP request. A pending result can still append a key before the next cancelled delay. | If this helper remains after the rewrite, propagate cancellation through the request and recheck ownership before publishing results. |
| `AnchorExchanger.cs`, POST | The response and request content do not have explicit disposal scopes. | If retained, give request/response resources scoped ownership and exercise failure paths. |

`RoomManager` still creates `CreateASA` and `FindASA` at runtime. They must not
be deleted just because ASA is being replaced. The app-source search found only
commented call sites for `AnchorExchanger.WatchKeys`; reassess the helper's
consumers during migration before choosing deletion or repair. Do not treat
this observation as proof that every external consumer has been ruled out.

## Verification and limitations

Reproduce the targeted source searches from the repository root:

```bash
rg -n '\basync\s+void\b|\bTask\.(Run|Factory\.StartNew)\s*\(|\bThread\.Start\s*\(' \
  Assets/Scripts --glob '*.cs'
rg -n 'new HttpClient' Assets/Scripts --glob '*.cs'
rg -n 'while \(|Task.Delay|GetStringAsync|PostAsync' \
  Assets/Scripts/AnchorExchanger.cs \
  Assets/Scripts/CreateASA.cs Assets/Scripts/FindASA.cs
```

The first search returned no matches; the client search returned the shared
initializer. The relevant source files were read to examine the actual control
flow and request ownership, rather than inferring correctness from grep alone.

Attempted `dotnet run --project tools/yield-lint -- Assets`; the shell reported
`command not found: dotnet`. This is not a lint pass. No Unity build or device
test was performed for this source-only audit. Existing EditMode tests cover
configuration/backend clients; they do not establish cancellation safety for
these legacy MonoBehaviour lifecycles.

Keep #28 and `geox-1mn` open. After #17/#23, inspect the surviving implementation,
run the lint/build and appropriate lifecycle tests, then update the concurrency
document only where the resulting code requires it. This preliminary audit
does not authorize closing the GitHub issue.
