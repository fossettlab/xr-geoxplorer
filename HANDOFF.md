# Handoff — xr-geoxplorer

**State as of 2026-08-03 (UTC):** **`main` is Unity 6** (**6000.4.4f1**). Unity 6 upgrade (#162), cloud backend (#161 content), HoloLens deprecation, and anchor race fixes (#163, #164) are **merged**. CI green on lint, functions tests, EditMode, Android compile. **Unity CLI + Pipeline MCP** connected; Editor must launch with **`--args "-automated"`**. **#28 async hygiene** grep-clean in `Assets/Scripts/` (see `docs/concurrency-model.md`). **Next hard gate: Quest 3 device smoke test** (#32).

**Memory slug:** `~/.claude-washu/projects/-Users-abradley-Dropbox--Geospatial-Fossett-Lab-09-XR-xr-geoxplorer/memory/`

---

## Current `main` (Unity 6)

### Landed recently

| PR | What |
|----|------|
| **#162** | Unity 6 upgrade, cloud auth backend, HoloLens cleanup, CI version-from-ProjectVersion |
| **#163** | Unity 6 native anchor race — async create before `NativeToCloud`/`CloudToNative` |
| **#164** | Copilot follow-ups (try/catch, AGENTS.md, portable MCP path, Newtonsoft 3.2.2) |

Superseded/closed: #161, #159, #160, #158, #156, #157, `unity6-upgrade-spike` branch.

### Unity MCP / CLI workflow (operator machine)

- **Requires:** Unity Editor open on this project in **automated mode** (modal
  popups otherwise break continuous CLI/MCP commands):

  ```bash
  unity open "/Users/abradley/Dropbox/_Geospatial/Fossett_Lab/09_XR/xr-geoxplorer" --args "-automated"
  ```

  Use the full path, not `~`. Do not open from Hub when agent work is planned.
- **Verify connection:** `unity pipeline list` → Server Reachable; `unity status`.
- **Read-only checks:** `unity command list_open_scenes`, `unity command get_console_logs -- --limit N`, `unity command recompile_status`.
- **Do not** run destructive Pipeline commands (`delete_*`, `editor_play`, `package_remove`, etc.) without explicit user approval.
- **Batch mode** (`unity run`) compiles then exits — Pipeline server needs a **GUI Editor** session.

### What is NOT done yet

1. **Quest 3 smoke test** (#32) — create/find anchor, Photon room, bundle load on device.
2. **Real APK/AAB build** on 6000.4.4f1 locally or sideload to Quest.
3. **#28 close-out** — grep acceptance met; codex impl review + close issue.
4. **MRTK 2 → MRTK 3** — not started (#14); legacy MRTK still in `Assets/MRTK/`.
5. **Dependabot #155** — merge manually in GitHub UI (`upload-artifact` 4→7; CLI lacks `workflow` scope).
6. **Azure Functions deploy + device SAS test** — gated on credentials (#24).

### Gotchas (Unity 6)

- **Preserve CRLF + UTF-8 BOM** on `.cs` edits.
- **Legacy HoloLens** assets remain under `Assets/Scenes/_legacy/` — reference only, not supported.
- **Package Manager online search auth error** in console — cosmetic unless searching registry in Editor.

---

## Prior sprint context (2026-07-27, `main`)

**Last committed on `main`:** `22410a2` — "Migrate mouse/touch input to the new Input System (#8)".

## One-paragraph summary

Sean (contractor) is on vacation ~1 month; the operator has the Quest 3 back and wants **batched** headset validation, not tiny device tasks — so the plan is to drive every non-headset task to merged/staged while quarantining device-only work. This sprint reviewed all open PRs (operator + codex), merged the three clean ones, then merged RemoteConfig (#147, closes #25) and stood up the first **automated no-device test coverage** (#148: a `GeoX.Config` asmdef + EditMode tests + a `Unity Tests` CI workflow via `game-ci/unity-test-runner`). The big win: codex plan+impl review revealed PR **#143** was not a simple input migration but bundled a ~14k-line scene rewrite + a 2,350-line per-frame UI bootstrapper; we extracted only the legitimate input migration (a `GeoXInput` wrapper + 4 migrated scripts, Active Input Handling kept on **Both**), fixed two behavior regressions codex found, merged it as **#149** (closes #8), closed #143 as superseded, and filed the excluded work as **#150** (`needs-quest`). Infrastructure now in place: **Beads** as the local agent execution layer (stealth, git-excluded; `bd ready` = the work queue), **elves** installed globally (native-worker-only guardrail). The operating rule the operator set: **codex reviews both the plan and the implementation of everything** before merge. Next checkpoint: task **#28 async hygiene** (coroutine-native model chosen), through the same spec → codex → implement → codex → CI loop.

## Where we are

- **Primary artifact:** Unity app on `main` at **6000.4.4f1**, compile-gated (Android build) + EditMode-tested + CS1626 lint green.
- **Last committed state:** `85a47e4` — Copilot follow-ups (#164); anchor race fix `e1cc862` (#163); Unity 6 upgrade `8ab3ff0` (#162).
- **Open PRs:** #155 (dependabot, merge in UI), #165 (cursor find-anchor placement race — supersede with local fix if landing here).
- **Key external state:** Beads DB at `.beads/` (git-excluded, local). Issue **#150** = deferred #143 UI/scene work (`needs-quest`, `headset-only`).

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

1. **Quest 3 smoke test** (#32) — batched headset validation; first post-Unity-6 acceptance.
2. **#28 async hygiene close-out** — grep clean; codex impl review then close issue.
3. **#21 PUN characterization test harness** — headless test code (regression baseline for PUN→NGO rewrite).
4. **#13 URP migration** — operator must approve Quest-tuned URP config before codex implements.
5. **`headset-only` beads** (#10,#11,#13,#17,#18,#19,#32,#33,#34) — batched Quest session when ready.

## Gotchas for the next session

- **Active Input Handling MUST stay "Both" (2)** — `QuestAndroidStoreSettingsConfigurator` on `main` validates and fails otherwise. Do NOT flip to "New" until MRTK3 (#14). (Memory: `input-handling-must-stay-both`.)
- **`.cs` files are CRLF + UTF-8 BOM; do NOT renormalize endings.** `main`'s `MobileManipulation.cs` is legitimately mixed CRLF/LF — preserve per-line endings, change only real lines (the extraction used a difflib ending-preserving merge).
- **Green CI ≠ correct/in-scope.** #143 was green but bloated — read a large PR's actual contents, not its stats.
- **codex reviews everything (plan + impl)** — operator asked for this explicitly. Route via `~/.claude/scripts/codex_call.sh`; WashU session = `gpt-5.6-sol`. Watch for the codex "grok delegation" rabbit hole and >200KB prompts (both cause empty/timeout) — filter big diffs (exclude regenerated prefabs/scene/.meta).
- **Beads is stealth/local** — do not commit `.beads/`; GitHub Issues stay canonical for humans (Sean/cursor). Reconcile bead status at each merge.
- **elves external workers stay OFF** (no API keys) — native codex only, so proprietary code never leaves the machine.
- **`scripts/__pycache__/`** is untracked noise — never `git add -A`; stage explicit paths.
