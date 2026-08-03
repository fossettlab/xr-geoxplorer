# Handoff — xr-geoxplorer

**State as of 2026-08-02 (UTC):** Active work is on branch **`unity6-upgrade-spike`** (not merged to `main`). Project upgraded **2022.3.62f2 → Unity 6000.4.4f1**; **Unity CLI + Pipeline MCP** connected and verified. **GeoXShared** is the open scene; compile **green**; build target **Android**; EditMode tests **7/7 pass**; Android build **dry-run valid**. Legacy **JsonDotNet** removed; **com.unity.nuget.newtonsoft-json** 3.0.2 via UPM. **HoloLens/UWP deprecated** (Quest-first). **#28 async hygiene** in progress on branch (`docs/concurrency-model.md` + `AnchorExchanger`/`CreateASA`/`FindASA` touched).

**Memory slug:** `~/.claude-washu/projects/-Users-abradley-Dropbox--Geospatial-Fossett-Lab-09-XR-xr-geoxplorer/memory/`

---

## Unity 6 upgrade spike (branch: `unity6-upgrade-spike`)

### What changed (this spike)

| Area | Change |
|------|--------|
| **Editor** | `ProjectSettings/ProjectVersion.txt` → **6000.4.4f1** |
| **Packages** | Unity 6 migration bumped XR/OpenXR/Input System/etc.; added **com.unity.pipeline** 0.4.0-exp.1, **com.unity.nuget.newtonsoft-json** 3.0.2 |
| **JsonDotNet** | Deleted `Assets/JsonDotNet/` (GUID conflict with UPM Newtonsoft); scripts still `using Newtonsoft.Json` |
| **HoloLens** | `Platform.cs` / `PlatformBootstrapper` — no WSA detection; `LegacyHoloLens2` obsolete. MRTK `MixedRealityOptimizeUtils` — UWP/WMR stubs removed |
| **MRTK compile** | `AwaiterExtensions.cs` — skip `AsyncOperation` awaiter on Unity 6 (conflict with built-in) |
| **AR Foundation 6** | `LobbyManager`, ASA `AnchorHelpers` — `TryAddAnchorAsync` / `TryRemoveAnchor` (sync API removed in AF 6) |
| **Unity 6 API sweep** | MRTK/Photon demos — `linearVelocity`/`linearDamping`, `GraphicsSettings.defaultRenderPipeline`, `SubsystemManager.GetSubsystems` |
| **#28 (partial)** | `AnchorExchanger` shared `HttpClient` + cancellable poll; `CreateASA`/`FindASA` try/catch; `docs/concurrency-model.md` |
| **Agent tooling** | `.cursor/mcp.json` (Unity MCP server), `.cursor/permissions.json` (CLI/MCP allowlist + auto-review hints), `AGENTS.md` Unity workflow section |
| **Scene** | **GeoXShared.unity** in build settings and opened via `unity command open_scene` |

### Unity MCP / CLI workflow (operator machine)

- **Requires:** Unity Editor open on this project (`unity open "/Users/abradley/Dropbox/_Geospatial/Fossett_Lab/09_XR/xr-geoxplorer"` — use full path, not `~`).
- **Verify connection:** `unity pipeline list` → Server Reachable; `unity status`.
- **Read-only checks:** `unity command list_open_scenes`, `unity command get_console_logs -- --limit N`, `unity command recompile_status`.
- **Do not** run destructive Pipeline commands (`delete_*`, `editor_play`, `package_remove`, etc.) without explicit user approval.
- **Batch mode** (`unity run`) compiles then exits — Pipeline server needs a **GUI Editor** session.

### What is NOT done yet (Unity 6 track)

1. **PR to `main`** — spike unmerged; CI (android-build, unity-tests) not re-run on Unity 6 yet.
2. **Android/Quest build** on 6000.4.4f1 — dry-run valid; real APK/AAB not built yet.
3. **#28 async hygiene** — grep acceptance on `Assets/Scripts/` mostly clean; finish sweep + codex review.
4. **MRTK 2 → MRTK 3** — not started (#14); legacy MRTK still in `Assets/MRTK/`.
5. **Package Manager online search auth error** in console — cosmetic unless searching registry in Editor.

### Gotchas (Unity 6)

- **`main` is still 2022.3.62f2** until this branch merges.
- **Preserve CRLF + UTF-8 BOM** on `.cs` edits.
- **Legacy HoloLens** assets remain under `Assets/Scenes/_legacy/`, `PlatformRoot.HoloLens2.prefab` — reference only, not supported.

---

## Prior sprint context (2026-07-27, `main`)

**Last committed on `main`:** `22410a2` — "Migrate mouse/touch input to the new Input System (#8)".

## One-paragraph summary

Sean (contractor) is on vacation ~1 month; the operator has the Quest 3 back and wants **batched** headset validation, not tiny device tasks — so the plan is to drive every non-headset task to merged/staged while quarantining device-only work. This sprint reviewed all open PRs (operator + codex), merged the three clean ones, then merged RemoteConfig (#147, closes #25) and stood up the first **automated no-device test coverage** (#148: a `GeoX.Config` asmdef + EditMode tests + a `Unity Tests` CI workflow via `game-ci/unity-test-runner`). The big win: codex plan+impl review revealed PR **#143** was not a simple input migration but bundled a ~14k-line scene rewrite + a 2,350-line per-frame UI bootstrapper; we extracted only the legitimate input migration (a `GeoXInput` wrapper + 4 migrated scripts, Active Input Handling kept on **Both**), fixed two behavior regressions codex found, merged it as **#149** (closes #8), closed #143 as superseded, and filed the excluded work as **#150** (`needs-quest`). Infrastructure now in place: **Beads** as the local agent execution layer (stealth, git-excluded; `bd ready` = the work queue), **elves** installed globally (native-worker-only guardrail). The operating rule the operator set: **codex reviews both the plan and the implementation of everything** before merge. Next checkpoint: task **#28 async hygiene** (coroutine-native model chosen), through the same spec → codex → implement → codex → CI loop.

## Where we are

- **Primary artifact:** the Unity app on `main`, all merged work compile-gated (Android build) + EditMode-tested + CS1626 lint green.
- **Last committed state:** `22410a2` — "Migrate mouse/touch input to the new Input System (#8)".
- **Key external state:** 0 open PRs. Beads DB at `.beads/` (git-excluded, local). Open GitHub issues tracked in Beads with `--external-ref gh-N`; `bd ready` gives the dependency-unblocked queue. New issue **#150** = deferred #143 UI/scene work (`needs-quest`, `headset-only`).

## What changed this session (committed vs uncommitted)

### Code / config (all committed, on `main`)
- `Assets/Scripts/Config/RemoteConfig*.cs` + `GeoX.Config.asmdef` — RemoteConfig ScriptableObject + its assembly (#147/#148).
- `Assets/Tests/EditMode/RemoteConfigTests.cs` + `GeoX.Config.Tests.asmdef` — 5 NUnit tests pinning RemoteConfig URLs (#148).
- `.github/workflows/unity-tests.yml` — new `unity-tests (EditMode)` CI job (#148).
- `Assets/Scripts/GeoXInput.cs` (new) + migrated `MobileManipulation/PlanetManager/AssetBundleInteraction/RoomManager.cs` — input migration (#149). CRLF+BOM preserved; Active Input Handling stays **Both**.

### Docs
- `docs/remote-config.md` — corrected the prod-injection note (editor-only override; a device build needs CI to rewrite the committed `Prod.asset`).

### Tooling (operator machine, not this repo)
- Beads initialized (`.beads/`, git-excluded); elves installed at `~/.claude/skills/elves`.
- `~/.claude/rules/account-data-routing.md` updated (WashU codex now defaults to `gpt-5.6-sol`) — uncommitted in the `~/.claude` config repo.

## What is NOT done yet

1. **#28 async hygiene** — coroutine-native model chosen; run spec → codex plan review → implement → codex impl review → CI. Start here.
2. **#21 PUN characterization test harness** — headless test code (regression baseline for the eventual PUN→NGO rewrite).
3. **#13 URP migration** — operator must approve the Quest-tuned URP config **before** codex implements; then stage as a build-green PR (perf validated later on Quest).
4. **Editor Play Mode feel-check for #149** — mouse zoom speed / touch rotation on macOS (scroll units platform-dependent; Windows parity restored). No headset; operator's Mac.
5. **`headset-only` beads** (#10,#11,#13,#17,#18,#19,#32,#33,#34) — batched Quest session when ready.

## Gotchas for the next session

- **Active Input Handling MUST stay "Both" (2)** — `QuestAndroidStoreSettingsConfigurator` on `main` validates and fails otherwise. Do NOT flip to "New" until MRTK3 (#14). (Memory: `input-handling-must-stay-both`.)
- **`.cs` files are CRLF + UTF-8 BOM; do NOT renormalize endings.** `main`'s `MobileManipulation.cs` is legitimately mixed CRLF/LF — preserve per-line endings, change only real lines (the extraction used a difflib ending-preserving merge).
- **Green CI ≠ correct/in-scope.** #143 was green but bloated — read a large PR's actual contents, not its stats.
- **codex reviews everything (plan + impl)** — operator asked for this explicitly. Route via `~/.claude/scripts/codex_call.sh`; WashU session = `gpt-5.6-sol`. Watch for the codex "grok delegation" rabbit hole and >200KB prompts (both cause empty/timeout) — filter big diffs (exclude regenerated prefabs/scene/.meta).
- **Beads is stealth/local** — do not commit `.beads/`; GitHub Issues stay canonical for humans (Sean/cursor). Reconcile bead status at each merge.
- **elves external workers stay OFF** (no API keys) — native codex only, so proprietary code never leaves the machine.
- **`scripts/__pycache__/`** is untracked noise — never `git add -A`; stage explicit paths.
