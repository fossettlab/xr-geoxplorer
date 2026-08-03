# NGO + Relay + Lobby package pins (#22 / #23)

**Status:** planning reference (2026-08-02). Verified against Unity Manual for
**2022.3.62f2** (this repo's editor version). Do **not** add these packages to
`xr-geoxplorer` until the #22 spike scorecard says **go** — use a throwaway project.

## Unity 2022.3.62f2 (current repo — spike target)

Pin these in the throwaway project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.unity.netcode.gameobjects": "1.15.1",
    "com.unity.services.relay": "1.2.0",
    "com.unity.services.lobby": "1.3.0",
    "com.unity.services.core": "1.14.0",
    "com.unity.transport": "2.5.3"
  }
}
```

| Package | Pin | Unity 2022.3 manual | Notes |
|---|---|---|---|
| `com.unity.netcode.gameobjects` | **1.15.1** | [NGO 1.15](https://docs.unity3d.com/2022.3/Documentation/Manual/com.unity.netcode.gameobjects.html) | **1.x line only** on 2022.3 — do not install NGO 2.x |
| `com.unity.services.relay` | **1.2.0** | [Relay 1.2](https://docs.unity3d.com/2022.3/Documentation/Manual/com.unity.services.relay.html) | NAT traversal for Quest + Editor |
| `com.unity.services.lobby` | **1.3.0** | [Lobby 1.3](https://docs.unity3d.com/2022.3/Documentation/Manual/com.unity.services.lobby.html) | Room discovery |
| `com.unity.services.core` | **1.14.0** | UGS bootstrap | Required by Relay/Lobby |
| `com.unity.transport` | **2.5.3** | NGO dependency | Pulled transitively; pin if PM resolves wrong version |

### Vivox (voice)

Vivox is enabled through **Unity Dashboard → Vivox** for the cloud project, then
added via Package Manager (Unity Services integration). Package id varies by
editor channel — in the spike project use **Window → Package Manager → Unity
Registry** and search `Vivox` after linking UGS. Do not commit Vivox credentials.

### NGO 1.x vs 2.x

| Editor | NGO line | Example version |
|---|---|---|
| Unity **2022.3.62f2** (this repo today) | **1.x** | 1.15.1 |
| Unity **6000.x** (#160 migration target) | **2.x** | 2.13.x |

The #22 spike runs on **2022.3** to match production. After Unity 6 lands (#160),
re-run a minimal NGO connectivity check on **NGO 2.x** before starting #23 in the
main repo.

## Throwaway project bootstrap

1. Unity Hub → **2022.3.62f2** → new 3D project (not this repo).
2. **Edit → Project Settings → Services** → link Unity Cloud project with Relay,
   Lobby, Vivox enabled.
3. Add package pins above to `Packages/manifest.json`; let Unity resolve lock file.
4. **Window → Multiplayer → Multiplayer Center** → install NGO + Relay sample
   (preferred over Boss Room for Quest scope).
5. Build Android (Quest) + run Editor as second client through Relay.

Full session plan: [`docs/networking-spike.md`](networking-spike.md).

## Wire contracts to validate (from #21)

| Behaviour | PUN today | NGO spike test |
|---|---|---|
| Transform sync | `OnPhotonSerializeView` pos/rot/scale | `NetworkTransform` or custom `NetworkVariable` |
| Shared anchor ID | `RPC_SetSharedAnchorID` → `GenericNetworkManager.AzureAnchorID` | `NetworkVariable<string>` or ServerRpc |
| Room join | Photon room | Lobby + Relay join code |
| Voice | Photon Voice | Vivox channel |

Harness reference: [`Assets/Tests/Network/PunWireContractTests.cs`](../Assets/Tests/Network/PunWireContractTests.cs).

## When #22 says go — main repo package PR

1. Add pins to [`Packages/manifest.json`](../Packages/manifest.json) in a dedicated PR.
2. Add `NetworkBootstrap` empty scene test (no gameplay migration yet).
3. Keep Photon installed until Phase 3 of [`docs/networking-rewrite-plan.md`](networking-rewrite-plan.md).

Touch-point map: [`docs/networking-file-inventory.md`](networking-file-inventory.md).

## Related

- [#22](https://github.com/fossettlab/xr-geoxplorer/issues/22) — spike
- [#23](https://github.com/fossettlab/xr-geoxplorer/issues/23) — rewrite
- [`docs/unity6-migration-runbook.md`](unity6-migration-runbook.md) — Unity 6 path (NGO 2.x)
