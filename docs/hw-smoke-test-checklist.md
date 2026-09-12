# Quest 3 hardware smoke test checklist (#32)

**Status:** manual procedure — run on device after Phase 1+ features land. Not a CI
gate (requires headset).

Pair with Tier 2 networking checks in [`docs/networking-harness.md`](networking-harness.md).

## Prerequisites

- Quest 3 with Developer Mode + USB debugging
- Built APK from Unity **6000.4.4f1** (see [`docs/quest3-build-and-deploy.md`](quest3-build-and-deploy.md))
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

## Planned scientific-scene and open-input checks

These extend the existing matrix as [the new content path](open-3d-plan.md)
lands. They are planned acceptance checks, not recorded passes.

- Open an HTTPS GLB that has never been imported into this Unity project,
  using the actual Quest UI. Verify materials, placement, manipulation, reset,
  recenter, layer information, and unload.
- Cancel during download/loading, retry after an error, and repeatedly load and
  unload models. Check for late/orphaned objects and retained memory.
- Load known-registered layers; toggle and select them. Move, rotate, and scale
  the whole scene, then verify alignment and scientific coordinates are unchanged.
- Preserve explicit Earth, Moon, Mars, and unknown/local-frame metadata. A
  mismatched layer must not be silently treated as registered in the scene.
- Save/reload scene state and replace/unload a runtime representation without
  losing the layer's scientific identity and provenance.
- Once shared layers are integrated, verify both clients agree on the scene,
  registration, and display pose, or show a clear content-load failure.
- Apply the [UI design acceptance](ui-design-principles.md): the dataset dominates
  at rest, direct manipulation is discoverable, contextual controls dismiss
  predictably, and typography/targets work at actual headset distances. Inspect
  advanced scientific detail without crowding the default view.
- Once implemented, transition tabletop to immersive and back without losing
  registration, layer identity, visibility or selection. Verify easy return and
  recenter; use calibrated scale claims only when physical dimensions are known.

Record source/fixture identity and any preparation as well as the build and
device evidence. A rendered derived mesh does not prove native DEM/point-cloud
support, and a working local GLB does not prove shared-scene support.

## Post-#23 additions

Replace Photon rows with NGO + Relay + Vivox equivalents from the networking spike
scorecard.

## Related

- Session 1 baseline: [`docs/quest-session-1-runbook.md`](quest-session-1-runbook.md)
- Store gates: [`docs/store-submission-checklist.md`](store-submission-checklist.md)
