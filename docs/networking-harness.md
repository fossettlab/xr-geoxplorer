# Networking characterization harness (PUN 2)

Issue [#21](https://github.com/fossettlab/xr-geoxplorer/issues/1). This harness
captures the **current Photon PUN 2 behavior** as a regression target *before* the
networking rewrite ([#23](https://github.com/fossettlab/xr-geoxplorer/issues/1),
NGO + Relay + Vivox) starts disturbing the PUN code paths. Without it, "did the
rewrite preserve behavior?" has no answer.

It does **not** refactor any production code. `LobbyManager`, `PlanetManager`,
`GenericNetworkManager`, `GenericNetSync`, `PhotonUser`, and `AnchorExchanger` are
left exactly as-is, per the ticket's out-of-scope rules. The pre-rewrite "split the
managers into clean layers" plan is intentionally dropped; seams get extracted in
#23 as the chosen stack requires.

## Layout

```
Assets/Tests/Network/
  Network.asmdef            test assembly (references PhotonUnityNetworking, PhotonRealtime)
  PunHarnessSupport.cs      reflection helpers + a Photon callback recorder
  RoomJoinTests.cs          workflow 1 — room join
  TransformSyncTests.cs     workflow 2 — transform sync wire contract
  AnchorIdExchangeTests.cs  workflow 3 — anchor-ID exchange
  TeardownTests.cs          workflow 4 — leave / reconnect / exit
```

All four are **Play Mode** tests (`[UnityTest]`), using the Unity Test Framework
(`com.unity.test-framework`, already in `Packages/manifest.json`).

## Mock endpoint: PhotonNetwork.OfflineMode (no credentials needed)

The ticket allows either a Photon dev app or a mock endpoint. This harness uses
**`PhotonNetwork.OfflineMode = true`** as the mock. That choice means:

- **No Photon AppId, dev app, or live network is required** — the suite runs in CI
  and on a fresh checkout with zero secrets.
- Room create/join, `JoinRandomRoom` failure, leave/reconnect, and the matchmaking
  callbacks all execute locally and deterministically.
- The trade-off: offline mode has a single local peer, so a literal "a *second*
  device sees the change" assertion is not possible here. That two-device check is
  owned by the hardware-in-the-loop smoke suite
  ([#32](https://github.com/fossettlab/xr-geoxplorer/issues/1)). What this harness
  pins instead is the **wire contract** — the exact serialized payload and the RPC
  side effects — which is what a rewrite can actually regress against off-device.

### Why reflection?

The app scripts compile into the predefined `Assembly-CSharp`, which an `.asmdef`
test assembly **cannot reference** (the dependency only runs the other way). Adding
an `.asmdef` under `Assets/Scripts` would be a production refactor, which #21
forbids. So the harness resolves app types by name at runtime
(`Type.GetType("GenericNetSync, Assembly-CSharp")`) and drives them through Photon
interfaces it *can* reference (`IPunObservable`). When #23 introduces a clean
networking assembly, these reflection seams should become direct references.

## The four workflows and their captured contracts

### 1. Room join — `RoomJoinTests`
- **CreateRoom** in offline mode enters a named room: `InRoom == true`,
  `CurrentRoom.Name` round-trips, `PlayerCount == 1`, and both `OnCreatedRoom` and
  `OnJoinedRoom` fire. `LobbyManager.OnJoinedRoom` relies on that callback to swap
  `LobbyUI → RoomUI` and spawn the player.
- **JoinRandomRoom** with no rooms fires `OnJoinRandomFailed`, which is the trigger
  `LobbyManager` overrides to fall back to creating a room.

### 2. Transform sync — `TransformSyncTests`
`GenericNetSync` streams object state via `IPunObservable.OnPhotonSerializeView`.
Captured contract for a non-`User` object (planets, asset bundles):
- **Write:** exactly three values, in order — `localPosition` (Vector3),
  `localRotation` (Quaternion), `localScale` (Vector3).
- **Read:** those three are stored into the private
  `networkLocalPosition / networkLocalRotation / networkLocalScale` fields that
  `FixedUpdate` copies onto a non-owned transform.

(The `User == true` camera-follow path, which sends head pose relative to
`TableAnchor`, is documented here but not asserted — it depends on scene singletons
and live ownership that belong to the #32 device suite.)

### 3. Anchor-ID exchange — `AnchorIdExchangeTests`
- **`PhotonUser.RPC_SetSharedAnchorID(string)`** writes its argument onto
  `GenericNetworkManager.instance.AzureAnchorID` — the one field the app reads to
  align on the shared anchor. #23 must preserve this hand-off.
- **`AnchorExchanger`** REST contract, exercised against a local `HttpListener`:
  a key is `POST`ed to `baseAddress` and the numeric response body is parsed to a
  `long`. (`RetrieveLastAnchorKey` GETs `baseAddress + "/last"`;
  `RetrieveAnchorKey(n)` GETs `baseAddress + "/" + n`.) This REST side is what
  [#40](https://github.com/fossettlab/xr-geoxplorer/issues/1) later replaces with the
  Azure Function. The test self-`Assert.Ignore`s if the sandbox cannot bind a local
  HTTP listener, so it never reports a false failure.

### 4. Teardown — `TeardownTests`
`LeaveRoom` clears `InRoom` and fires `OnLeftRoom`; a second `CreateRoom` re-enters
(reconnect); turning offline mode off ends in a not-in-room, not-connected state.

## Running the harness

In the editor: **Window ▸ General ▸ Test Runner ▸ PlayMode ▸ Run All**.

Headless (matches the `unity-test-runner` CI step), from the repo root on macOS:

```bash
'/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity' \
  -runTests \
  -batchmode \
  -projectPath . \
  -testPlatform PlayMode \
  -testResults /tmp/geox-network-tests.xml \
  -logFile /tmp/geox-network-tests.log
```

`-testResults` is an NUnit XML file; a green run reports all tests in the `Network`
assembly as `Passed`. CI runs the same via `.github/workflows/unity-tests.yml`,
which — like the Android build — **skips** when the `UNITY_LICENSE` / `UNITY_EMAIL`
/ `UNITY_PASSWORD` secrets are absent (e.g. fork PRs) instead of failing.

## Status / known limitations

- These tests require the Unity editor (2022.3.62f2) to execute; they have not been
  run in the cloud authoring environment, which has no Unity install. Run them in
  the Test Runner to confirm green before relying on the regression gate.
- Reflection against private fields (`networkLocal*`, `AzureAnchorID`, `baseAddress`)
  is deliberately brittle: it is the price of honoring "no production refactor." If a
  field is renamed, the corresponding test fails fast with a clear `MissingField`
  message — that is the signal to update the harness (or, in #23, to replace the
  reflection with a direct reference).
