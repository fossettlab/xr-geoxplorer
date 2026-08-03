# Networking spike — NGO + Relay + Vivox on Quest 3 (#22)

**Status: plan + scorecard template. Not yet run.** **`main` is Unity 6000.4.4f1**
(Photon PUN still in production). Run this spike in a **throwaway project** — see
[`ngo-package-pins.md`](ngo-package-pins.md) for editor version options.

The plan locks in **Unity Netcode for GameObjects (NGO) + Unity Relay + Vivox** as
the default replacement (Photon PUN 2 is EOL; Photon Fusion 2 is the fallback if
NGO can't hit Quest 3 perf). This spike is go/no-go on that stack.

## What this reproduces (tie to #21)

The #21 PUN characterization harness pins the behavior the rewrite must preserve:
the transform-sync payload is `position, rotation, scale` (in order), and a shared
anchor ID propagates to every client. The transform-sync and authority tests below
are the NGO equivalent of those contracts — if NGO reproduces them, the rewrite has
a green path.

## Prerequisites (operator — before the session)

These need a Unity account/dashboard and are not automatable here:

1. **Unity Cloud project + UGS.** Create (or link) a Unity Cloud project, then in
   Project Settings -> Services link it. Enable **Relay**, **Lobby**, and **Vivox**
   for that project. Note the project ID; Relay/Vivox keys are read from the linked
   project at runtime (do not paste keys into the repo or the transcript).
2. **Two test clients.** One Quest 3 + one Editor instance is enough (two headsets
   is better for the voice + latency feel). The Editor client connects over Relay
   the same as a headset.
3. **A perf baseline** from the Session 1 runbook (pre-URP FPS / frame time). The
   perf test below compares against it.

## Operator checklist (before the spike session)

Use this as a printable run sheet. Check items off as you go; paste Unity Cloud
project ID into your private notes (never commit keys to the repo).

### Unity Cloud + UGS (one-time)

- [ ] Create or select a Unity Cloud project for the spike (separate from production).
- [ ] In the throwaway Unity project: **Edit → Project Settings → Services** → link the cloud project.
- [ ] Unity Dashboard → **Relay** → enable for the project.
- [ ] Unity Dashboard → **Lobby** → enable for the project.
- [ ] Unity Dashboard → **Vivox** → enable for the project.
- [ ] Note free-tier limits for Relay CCU and Vivox concurrent users (for the scorecard).

### Throwaway project packages

Install in the throwaway spike project (prefer **6000.4.4f1** on `main`; **2022.3.62f2**
optional for NGO 1.x baseline — see [`ngo-package-pins.md`](ngo-package-pins.md)):

| Package | Purpose |
|---|---|
| `com.unity.netcode.gameobjects` | NGO core — pin **1.15.1** on 2022.3 (see [`docs/ngo-package-pins.md`](ngo-package-pins.md)) |
| `com.unity.services.relay` | NAT traversal / hosting |
| `com.unity.services.lobby` | Session discovery |
| Vivox (via Unity Services) | Voice |

Start from **Multiplayer Center** sample (Window → Multiplayer) rather than Boss Room.

### Test clients

- [ ] Quest 3 with Developer Mode + USB debugging (see Session 1 runbook).
- [ ] Second client: second Quest **or** Unity Editor play mode connected through Relay.
- [ ] Session 1 perf baseline captured (FPS / frame time) for test #5 comparison.

### Session flow (3–5 days, hard stop at day 5)

| Day | Goal |
|---|---|
| 1 | Packages + Relay join from Editor; one moving cube synced |
| 2 | Quest build joins same Relay session; measure transform latency |
| 3 | Vivox voice between clients |
| 4 | Authority transfer + WiFi kill/resume test |
| 5 | Fill scorecard below; go / no-go decision |

## Setup (throwaway project — NOT this repo)

Keep this out of `xr-geoxplorer` so it can't contaminate the main project.

1. Fresh Unity project at **6000.4.4f1** (or 2022.3.62f2 for NGO 1.x baseline).
2. Install: `com.unity.netcode.gameobjects`, `com.unity.services.relay`,
   `com.unity.services.lobby`, and Vivox (via Unity Services / package).
3. Start from the **Multiplayer Center** sample (simpler than Boss Room) for the
   NGO + Relay connection pattern; add Vivox to it.
4. Build to the Quest the same way as the main app (OpenXR, Android, IL2CPP/ARM64,
   Vulkan — see the Session 1 runbook).

## Tests and pass bars (the 3-5 day box; hard stop at 5 days)

| # | Test | Pass bar |
|---|------|----------|
| 1 | **Transform sync** — one client moves an object | remote sees it at **<= 100 ms** latency, smooth interpolation |
| 2 | **Voice (Vivox)** — one client speaks | other hears them, low latency, intelligible |
| 3 | **Authority transfer** — "anyone can place a planet" | NGO ownership-transfer API supports shared-authority-style placement |
| 4 | **Connection resilience** — kill host WiFi 5 s, restore | reconnects, or disconnects gracefully (no hang/crash) |
| 5 | **Perf on Quest 3** — networking + voice active | frame budget regresses **<= 5%** vs the Session 1 baseline |

## Scorecard (fill in, then commit)

| Criterion | Result |
|-----------|--------|
| Transform interpolation quality (1-5) | |
| Voice quality + latency | |
| Authority model fit for shared planet placement | |
| Relay free-tier headroom (max users x CCU pricing) | |
| Vivox cost / free-tier headroom | |
| Quest platform support today | |
| Vendor roadmap risk | |
| **Recommendation (go / no-go)** | |

**Decision rule.** *Go* -> the #23 rewrite proceeds on NGO + Relay + Vivox. *No-go*
-> name the specific failure mode (which test failed and how) and expand #23 to
begin with a **Fusion 2** spike before the rewrite. Make the call with whatever
data the 5-day box produced; do not run past it.

## Out of scope

Migrating production code (this is a throwaway prototype); production Relay/Vivox
billing/subscriptions (that is part of #23); testing Normcore (dropped — Roblox
acquisition roadmap risk).
