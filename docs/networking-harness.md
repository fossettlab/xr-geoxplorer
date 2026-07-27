# Networking characterization harness

This harness captures the current Photon PUN 2 networking behaviour as a
regression target for the networking rewrite (#23). Without it, "did the rewrite
preserve behaviour?" has no answer. It observes behaviour only - no production
networking code is refactored.

The four workflows it characterizes are:

1. **Room join** - lobby flow, picking a room, entering it.
2. **Transform sync** - one user moves an object, another sees the movement.
3. **Anchor-ID exchange** - anchor placement, ID over the wire, remote loads it.
4. **Teardown** - leaving a room, reconnect, exit.

Coverage is split into two tiers because a true two-client round-trip is not
something a single headless Editor can run: `PhotonNetwork` is a static,
single-client-per-process singleton, so one process hosts exactly one PUN client,
and a live Photon Cloud connection is non-deterministic (network latency, shared
app, timing) and unfit for a merge gate.

## Tier 1 - deterministic contract tests (CI gate)

The durable regression signal is the *wire contract* and the *observable effect*
of each exchange, both of which are deterministic and need no connection. These
run on every PR via the `Unity Tests` workflow (`.github/workflows/unity-tests.yml`,
`testMode: EditMode`), which auto-discovers the test assembly.

Location: `Assets/Tests/Network/`
- `GeoX.Network.Tests.asmdef` - EditMode test assembly. References the Photon
  assemblies directly; resolves the gameplay types (which live in
  `Assembly-CSharp`, un-referenceable from a test assembly) by reflection.
- `PunWireContractTests.cs`:
  - `SerializeView_Write_EmitsLocalPositionRotationScaleInOrder` - the sender
    serializes exactly `localPosition`, `localRotation`, `localScale`, in order.
  - `SerializeView_Read_StoresReceivedPositionRotationScale` - the receiver reads
    three incoming values, in order, into its network-target fields.
  - `SharedAnchorIdHandler_WritesIdIntoNetworkManager` - the buffered anchor-ID
    RPC handler copies the received string into
    `GenericNetworkManager.AzureAnchorID`.

These are phrased against preserved *behaviour* (the transform payload shape; the
anchor ID propagating to the shared field), not against PUN implementation
details, so they remain meaningful after PUN is replaced.

Run locally: Unity Editor -> Window -> General -> Test Runner -> EditMode -> Run All.

### What Tier 1 does not cover

The transform-sync owned-user/camera branch (anchor-relative substitution) depends
on reliable PUN ownership and is exercised by the Tier 2 live run. Room join, RPC
routing/buffering across real clients, disconnect, and reconnect are inherently
multi-client and live only in Tier 2.

## Tier 2 - live smoke procedure (manual, not a CI gate)

The four end-to-end workflows against a live Photon app, run manually. This is a
batched step for a local two-instance run or an on-device (Quest) session, not CI.

**Setup.** Two client instances are required. Either:
- Two Editor instances via a project clone tool (e.g. ParrelSync), or two cloned
  checkouts opened in separate Editors; or
- One Editor plus one on-device build (Quest 3), which also validates the headset
  path.

The Photon Realtime app ID is in
`Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset`. Photon
app IDs are client-distributed by design (they ship in every build), so this is
not a server secret; it is noted here only so the run is reproducible.

**Procedure and expected outcomes.**

| # | Workflow | Steps | Pass criteria |
|---|----------|-------|---------------|
| 1 | Room join | Launch both clients; on client A create/pick a room; on client B join the same room | Both clients report in-room; each sees the other in the player list |
| 2 | Transform sync | With both in-room, move a synced object (or the user avatar) on client A | Client B sees the object/avatar move within a frame or two, at the same pose |
| 3 | Anchor-ID exchange | On client A place/set an anchor and share it | Client B receives the anchor ID (`GenericNetworkManager.AzureAnchorID` matches on both) and resolves to the same anchor |
| 4 | Teardown | Client B leaves the room; rejoin; then both exit | Leave/rejoin succeeds cleanly; no leaked synced objects; exit disconnects both without error |

Record pass/fail per row and the build/date. A failure here after the #23 rewrite,
against a Tier 1 that stayed green, localizes the regression to live transport
behaviour rather than the payload contract.

## Note on acceptance criteria

Ticket #21 originally asked for all four workflows "green in CI". Because a
single-process Editor cannot host two PUN clients and a live cloud round-trip is
non-deterministic, the CI gate is the Tier 1 deterministic contract suite; the
four live workflows are the documented Tier 2 procedure above. This satisfies the
ticket's purpose - a regression target for #23 - while keeping the gate reliable.
