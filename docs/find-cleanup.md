# Find* cleanup (issue #30)

## Audit summary (pre-change)

About **60** call sites under `Assets/Scripts/` used
`GameObject.Find`, `FindObjectOfType`, `FindGameObjectsWithTag`, or
`FindGameObjectWithTag`.

| Category | Examples | Treatment |
|---|---|---|
| Scene-static managers | `LobbyManager`, `FirebaseExchanger`, `TableAnchor`, `PlanetManager` | **Singletons / Instance** + `ServiceLocator` |
| Per-interaction lobby access | `FindGameObjectWithTag("NetworkRoom").GetComponent<LobbyManager>()` | `LobbyManager.Instance` |
| Dynamic multi-object sets | tooltips, flags, tiles, loaders | `SceneQueries.WithTag` / `OneWithTag` / `AnyWithTag` |
| Hot-path Update scan | `FadeOutRealtimeEarth` TilePlane find every frame | **Cached** on `OnEnable` |
| Cold AR wiring | `ARAnchorManager`, `ARPlaneManager`, `ARRaycastManager` | Left as `FindObjectOfType` with comments |
| Editor/resources scan | `Resources.FindObjectsOfTypeAll<InspectorModelObject>()` | Left (not a scene Find; editor asset scan) |

## Patterns introduced

### `LobbyManager.Instance` / `FirebaseExchanger.Instance`

Registered in `Awake`, cleared in `OnDestroy`, also registered with
`ServiceLocator`.

### `Assets/Scripts/Services/ServiceLocator.cs`

Typed `Register` / `Unregister` / `Get` / `GetRequired` for optional
cross-cutting lookups without grepping the scene.

### `Assets/Scripts/Services/SceneQueries.cs`

Single place allowed to call Unity tag/name Find APIs for **dynamic** sets
(spawned tooltips, flags, map tiles). Call sites must not scatter raw
`GameObject.Find*`.

## Remaining justified Find* sites

Run:

```bash
rg -n 'GameObject\.Find\(|FindObjectOfType|FindGameObjectsWithTag|FindGameObjectWithTag' Assets/Scripts --glob '*.cs'
```

Expected residual (≈10, all justified):

| Location | Why it stays |
|---|---|
| `SceneQueries.cs` | Central wrapper — only approved Find* implementation for tags/names |
| `LobbyManager` AR manager lookups | One-shot cold path when starting AR / joining a room |
| `RoomManager.EnsureRaycastManager` | Cached after first cold lookup |
| `Resources.FindObjectsOfTypeAll` in `AssetBundleInteraction` | Asset database scan, not a scene Find |
| Commented lines in `UIEnterAndLeave` / `TileStageOrganizer` | Dead code comments only |

## Follow-ups (out of scope)

- Serialized Inspector refs on prefabs once PlatformRoot wiring is stable
- Active-model tracker to remove `activeModel` tag scans on the hand menu
- Align with #31 `Tags.*` constants when that PR merges (stack/rebase)
