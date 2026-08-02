# Quest 3 hardware smoke test checklist (#32)

**Status:** manual procedure — run on device after Phase 1+ features land. Not a CI
gate (requires headset).

Pair with Tier 2 networking checks in [`docs/networking-harness.md`](networking-harness.md).

## Prerequisites

- Quest 3 with Developer Mode + USB debugging
- Built APK from Unity 2022.3 (see [`docs/quest3-build-and-deploy.md`](quest3-build-and-deploy.md))
- Optional second Quest or Editor clone for multiplayer rows

## Smoke matrix

| # | Area | Steps | Pass |
|---|---|---|---|
| 1 | Boot | Install APK, launch from library | Immersive VR, head tracking works |
| 2 | Lobby | Reach main menu / room UI | No crash; UI readable |
| 3 | Room join | Create or join Photon room (pre-#23) | Both clients in-room |
| 4 | Asset download | Download one outcrop + one dem model | Model loads (magenta OK pre-URP) |
| 5 | Transform sync | Move synced object on client A | Client B sees movement |
| 6 | Anchor flow | Create or find named anchor | Anchor resolves; no NRE |
| 7 | Voice | Speak on client A | Client B hears (when voice enabled) |
| 8 | Teardown | Leave room, rejoin, exit app | Clean disconnect, no hang |

Record build git SHA, date, pass/fail per row, and logcat file for failures.

## Post-#23 additions

Replace Photon rows with NGO + Relay + Vivox equivalents from the networking spike
scorecard.

## Related

- Session 1 baseline: [`docs/quest-session-1-runbook.md`](quest-session-1-runbook.md)
- Store gates: [`docs/store-submission-checklist.md`](store-submission-checklist.md)
