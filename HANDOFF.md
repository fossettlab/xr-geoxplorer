# Handoff — xr-geoxplorer modernization

This repo is being revived from an archived **Unity 2019.4.8f1** state to a shippable **Meta Quest 3** mixed-reality app on **Unity 2022.3 LTS / URP / OpenXR / MRTK3**, with HoloLens 2 supported best-effort.

## Start here

1. **Read the epic:** https://github.com/fossettlab/xr-geoxplorer/issues/1 — it has the goal, the Quest-first decision principle, the dep graph, and a tree of 36 sub-issues. ~5 minutes.
2. **Read the Azure storage inventory:** [`docs/azure-storage-inventory.md`](docs/azure-storage-inventory.md) — what's deployed on Azure today, which 5 of 23 containers the runtime uses, and the bundle-structure finding that reshaped #6. ~5 minutes.
3. **Work the Pre-flight tickets in order:** #2 → #3 → #4. Most of #2's archaeology is already done (see its body); the dev decision left there is small. Ticket #4 is a 5-minute `git tag` task, tagged `good first issue` as a warm-up.
4. **Each ticket is self-contained.** File:line references against the current codebase, acceptance-criteria checkboxes, suggested approach with code patterns, doc links, and explicit out-of-scope. Tickets with `## Update YYYY-MM-DD` sections at the top have been revised since first filing — read those first, they supersede older framings in the body. If anything is unclear, comment on the ticket before starting work.

## Workflow

- **Fork the repo.** PRs come in from your fork. Each PR closes one sub-issue.
- **Conventional commits** on PRs: `feat:`, `fix:`, `docs:`, `refactor:`, `test:`.
- **Never commit directly to `main`.** Open a PR.
- **One PR per sub-issue** (or per coherent piece of one — the URP migration #13 may be multiple PRs).
- Tag this repo's lead in PR review when a sub-issue is ready to close.

## Quest-first principle

When Meta Quest 3 and HoloLens 2 pull in different directions, **Quest wins**. Explicitly:

- URP is mandatory (Quest perf parity).
- Vulkan over D3D11 (Quest preferred).
- MRTK3 over MRTK 2.8 (skipping the intermediate step).
- Meta Spatial Anchors first; HL2 local-only anchors are best-effort.
- HL2 work never blocks Quest critical path. If MRTK3-on-HL2 turns out to be too unstable in practice, escalate to a separate stability-branch epic — the call is in ticket #20.

## Sizing convention

Sub-issues use AI-first dev workflow estimates (Claude / Cursor / Copilot driving most typing):
- **S** ≤ 2 h
- **M** 2 h – 1 d
- **L** 1 – 3 d
- **XL** 3 – 7 d
- **XXL** > 1 wk

Hardware-in-loop work (device deploys, Unity build cycles, anchor lifecycle debugging) does **not** compress with AI and is sized accordingly. Adjust upward if your workflow is more manual.

## What you need that's not in the public tickets

These are deliberately not in tickets — request them from the project lead before starting the relevant phase:

| Need | When | What |
|---|---|---|
| **Azure subscription access** (Fossett Lab) | Phase 1 onward | Read access for the `haringerverdiag` storage account (asset bundles + thumbnails + restricted); write access for staging blob + the Azure Function (#24). Account inventory: `docs/azure-storage-inventory.md`. |
| **NAS access** (Fossett Lab) | Phase 1 (#6) | The 48 GB `GeoXAssetBundles` Unity project at `/mnt/nas/dev/fossett_xr_apps/GeoXAssetBundles/` holds source content for the bundle re-bake. Likely accessed via lab VPN or a one-time rsync. |
| **Firebase project access** | Phase 4 (#24) | Read access to inventory existing data before audit-or-delete. |
| **Meta Quest Developer org invite** | Phase 6 (#33) | For App Lab submission. |
| **One Quest 3 device, ideally two** | Phase 1 (#10) onward | Two are needed for the networking spike (#22) and the HW smoke suite (#32). |
| **HoloLens 2 device** | Phase 3 (#20), optional | For HL2 best-effort validation. Not blocking. |
| **Unity Pro / Plus / Education license** | Phase 1 (#12) onward | Required for GitHub Actions builder; recommended for IL2CPP builds. |
| **Production Android keystore** | Phase 6 (#33) | For signed App Lab uploads. Generate fresh; store in a secrets manager, not the repo. |

## Hard external deadline

**Meta requires `targetSdkVersion = 34` (Android 14) for new binary uploads to App Lab and the Quest store from March 1, 2026.** Ticket #9 lands this; #33 verifies it in the final build.

## Top risks (also in the epic)

1. **Bundle-structure mismatch** (#6): the NAS pipeline outputs one bundle per category; deployed bundles are one per model. Dev writes a fresh pipeline matching the deployed shape — small Editor script, ~1–2 days. Details in #6 and `docs/azure-storage-inventory.md`.
2. **Custom shader rewrites** in #13 (`Blend2Textures.shader` fixed-function, `Planet.shader` Surface Shader) are URP **rewrites**, not conversions.
3. **Networking rewrite** (#23) is genuinely large — 40 PunRPCs + Photon Voice migration + anchor-ID bridge.
4. **URP perf gate** (#13) could fail on first attempt — keep the PR reversible.
5. **March 2026 Meta deadline** is hard external (targetSdkVersion 34 for new uploads).

## Decisions already made (do not re-litigate without raising it on #1)

- **Networking:** Unity Netcode for GameObjects + Relay + Vivox (default). Fusion 2 as fallback if NGO can't hit Quest perf. Normcore dropped.
- **Anchors:** Meta XR Core SDK Spatial Anchors / Building Blocks (default). AR Foundation OpenXR as fallback. 1-2 day spike validates inside #17.
- **Auth backend:** signed Azure Function. Firebase Unity SDK only if a feature actually needs it. Same Function also issues SAS tokens for the `restricted` container (#37).
- **Scene architecture:** one `GeoXShared.unity` with per-platform prefab variants. Ends the manual scene-swap workflow described in the old `README.txt`.
- **AssetBundle pipeline:** the NAS `GeoXAssetBundles` project (Unity 2017.4) is reference, not runnable; the dev writes a fresh small pipeline in #6 that produces the per-model layout the runtime expects. See `docs/azure-storage-inventory.md` and #6.

## Questions

Comment on the relevant sub-issue, or on the epic (#1) for cross-cutting questions.
