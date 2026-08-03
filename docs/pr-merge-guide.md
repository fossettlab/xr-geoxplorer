# PR merge guide — cloud + Unity agent handoff

> **Status: COMPLETED (2026-08-03).** Unity 6 (#162), anchor fixes (#163–#166),
> and doc updates landed on `main`. Kept for historical reference only.

**Purpose:** ordered merge plan for open agent PRs, conflict hotspots, and
post-merge verification. **Do not merge until a human approves each PR.**

Last verified: 2026-08-03 (harmonized on `unity6-upgrade-spike` / PR #162 — **merged**).

## Harmonized merge path (current)

**PR [#162](https://github.com/fossettlab/xr-geoxplorer/pull/162)** (`unity6-upgrade-spike`) now
**includes** the content of #161, #159, and #160 with conflicts resolved locally:

| Absorbed PR | Conflicts resolved | Notes |
|---|---|---|
| [#161](https://github.com/fossettlab/xr-geoxplorer/pull/161) cloud tickets | 5 files: `AGENTS.md`, `AnchorExchanger.cs`, `CreateASA.cs`, `FindASA.cs`, `docs/concurrency-model.md` | Took #161 cloud tooling + #162 Unity 6 async patterns |
| [#159](https://github.com/fossettlab/xr-geoxplorer/pull/159) HoloLens deprecation | 1 file: `Platform.cs` | Removed `LegacyHoloLens2` enum entirely |
| [#160](https://github.com/fossettlab/xr-geoxplorer/pull/160) Unity 6 runbook | none | Doc merged cleanly |

**Recommended action:** merge **#162 only** into `main`, then **close #158–#161** as superseded.
Keep [#155](https://github.com/fossettlab/xr-geoxplorer/pull/155) (dependabot) as a separate merge.

## Original open PRs (superseded by #162)

| Order | PR | Branch | Status |
|---|---|---|---|
| — | [#162](https://github.com/fossettlab/xr-geoxplorer/pull/162) | `unity6-upgrade-spike` | **Merge this** |
| absorbed | [#161](https://github.com/fossettlab/xr-geoxplorer/pull/161) | `cursor/cloud-tickets-networking-auth-50b2` | Close after #162 |
| absorbed | [#159](https://github.com/fossettlab/xr-geoxplorer/pull/159) | `cursor/hololens-deprecation-50b2` | Close after #162 |
| absorbed | [#160](https://github.com/fossettlab/xr-geoxplorer/pull/160) | `cursor/unity6-migration-50b2` | Close after #162 |
| — | Close [#158](https://github.com/fossettlab/xr-geoxplorer/pull/158) | superseded by #161 → now #162 | — |
| — | Close [#156](https://github.com/fossettlab/xr-geoxplorer/pull/156) / [#157](https://github.com/fossettlab/xr-geoxplorer/pull/157) | doc content in #162 | — |

**Why one PR:** #161 and #162 both touched #28 async hygiene files; #159 overlapped
HoloLens deprecation already started on the Unity 6 spike. Harmonizing on #162 avoids
three sequential merges to `main` and duplicate CI runs on 2022.3.

## Conflict analysis (dry-run, pre-harmonization)

### #161 vs #162 (resolved 2026-08-03)

Trial merge had conflicts in:

- `AGENTS.md` — merged Unity 6 MCP workflow + cloud VM instructions
- `Assets/Scripts/AnchorExchanger.cs` — kept `WatchKeysAsync` (no `Task.Run`)
- `Assets/Scripts/CreateASA.cs`, `FindASA.cs` — `void Start` + `RunInitializeAsync`
- `docs/concurrency-model.md` — combined both versions

### #159 vs #162 (resolved 2026-08-03)

- `Assets/Scripts/Platform/Platform.cs` — removed obsolete `LegacyHoloLens2` stub

### #161 vs `main`

No conflicts expected. `git merge-tree` shows additive changes only (new
`functions-tests.yml`, `.gitignore` entries, new docs/scripts).

### #159 vs `main` (after #161 merged)

No conflicts expected with #161 changes — **#161 does not touch** any of these
files:

- `Assets/Scripts/Platform/Platform.cs`
- `Assets/Scripts/PlatformBootstrapper.cs`
- `Assets/Editor/GeoXAssetBundlePipeline.cs`
- `Assets/Prefabs/PlatformRoot/*`
- `docs/platform-helper.md`, `docs/scene-architecture.md`

### #159 vs #161 (if merged in wrong order)

`git merge-tree` between the two branches auto-merges cleanly for overlapping
files — hololens branch wins on platform deletions; cloud branch wins on
functions/docs. **Still prefer #161 first** so CI and auth backend land before
large asset deletions.

### #160 vs `main` (after #159 merged)

Single new doc file — no conflicts expected.

## Step-by-step merge procedure

Dry-run conflicts before merging:

```bash
./scripts/check_merge_conflicts.sh origin/main cursor/cloud-tickets-networking-auth-50b2
./scripts/check_merge_conflicts.sh origin/main cursor/hololens-deprecation-50b2
./scripts/check_merge_conflicts.sh origin/main cursor/unity6-migration-50b2
```

### After #161 approved

```bash
git checkout main && git pull origin main
git merge --no-ff origin/cursor/cloud-tickets-networking-auth-50b2
# verify:
python -m pytest functions/tests/ -q          # expect 16+ passed
python3 scripts/compare_manifest_to_inventory.py  # android/ios/wsa OK
git push origin main
```

Close draft PR **#158**. Optionally close **#156** / **#157** if their doc
content is fully superseded.

### After #159 approved

```bash
git checkout main && git pull origin main
git fetch origin cursor/hololens-deprecation-50b2
git rebase origin/main cursor/hololens-deprecation-50b2   # should be clean
git checkout main
git merge --no-ff cursor/hololens-deprecation-50b2
git push origin main
git push origin cursor/hololens-deprecation-50b2   # update rebased branch if needed
```

**Unity agent validation (#159):**

- Scene Architecture menu: Quest3 / Mobile / Editor variants load
- Play Mode: no missing script refs on `GeoXShared.unity`
- `rg 'LegacyHoloLens2|PlatformRoot.HoloLens2' Assets/Scripts Assets/Scenes` → zero hits
- GameCI EditMode tests still pass (when Unity secrets configured)

### After #160 approved

```bash
git checkout main && git pull origin main
git rebase origin/main cursor/unity6-migration-50b2   # 1 commit, should be clean
git checkout main
git merge --no-ff cursor/unity6-migration-50b2
git push origin main
```

**Mac Unity 6 session** follows [`docs/unity6-migration-runbook.md`](unity6-migration-runbook.md)
— not part of the merge itself.

## CI gates per PR

| PR | Automated | Manual |
|---|---|---|
| #161 | `functions-tests.yml` (pytest), `lint.yml` (yield-lint) | — |
| #159 | `lint.yml`, `unity-tests.yml` (if secrets set) | Play Mode Quest3/Mobile |
| #160 | — | Mac Unity 6 session (separate from merge) |

## Files to watch if conflicts appear anyway

| File | PRs | Resolution hint |
|---|---|---|
| `Assets/Editor/GeoXAssetBundlePipeline.cs` | #159 | Keep hololens version (WSA menu removed) |
| `Assets/Scripts/Platform/Platform.cs` | #159 | Keep hololens version (`LegacyHoloLens2` removed) |
| `Assets/Scripts/PlatformBootstrapper.cs` | #159 | Keep hololens version |
| `docs/platform-helper.md` | #159 | Keep hololens version (HoloLens section removed) |
| `docs/scene-architecture.md` | #159 | Keep hololens version |
| `AGENTS.md` | #161 | Keep cloud version (has Functions/venv instructions) |
| `README.md` | #161 | Updated to Unity 6000.4.4f1 on `main` |

## Post-merge full-stack verification

Once all three PRs land:

```bash
python -m pytest functions/tests/ -q
python3 scripts/compare_manifest_to_inventory.py
dotnet run --project tools/yield-lint -- Assets/Scripts   # Mac/cloud with .NET SDK
```

Then Mac agent: Quest Session 1 ([`docs/quest-session-1-runbook.md`](quest-session-1-runbook.md))
→ Unity 6 session (#160 runbook) when ready.

## Related

- [`docs/azure-function-provisioning.md`](azure-function-provisioning.md) — deploy #24 after merge
- [`docs/networking-file-inventory.md`](networking-file-inventory.md) — #23 touch-point map
- [`docs/perf-baseline-template.md`](perf-baseline-template.md) — #11 capture form
