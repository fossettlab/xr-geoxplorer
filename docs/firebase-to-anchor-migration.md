# Firebase → Anchor backend migration guide (#40)

**Status:** wiring guide for the Mac/Unity agent after #17 (Meta anchors) and #23
(networking) land. Backend scaffold and client helpers are on PR #161; gameplay
still uses [`FirebaseExchanger.cs`](../Assets/Scripts/FirebaseExchanger.cs).

## API shape mapping

| Firebase today | Anchor backend (#40) |
|---|---|
| `GET firebaseAnchorsUrl` → JSON array | `GET /api/anchors` + `X-API-Key` |
| `PUT firebaseAnchorsUrl` → full array replace | `POST /api/anchors` (append one record) |
| Fields: `name`, `identifier`, `dateCreated`, `dateExpired` | List GET returns same camelCase; POST uses `dateExpired` |

**Behaviour change:** Firebase PUT replaces the entire list client-side
(read-modify-write). The Function backend stores **individual records** in Table
Storage. Create flow becomes POST-only; list refresh is GET (no client-side merge
before upload except duplicate-name check against GET result).

## RemoteConfig

Reuse existing #25 fields (no new assets required for v1):

| Field | Value |
|---|---|
| `sasEndpointBaseUrl` | `https://<function-app>.azurewebsites.net` |
| `sasApiKey` | same as Function `SAS_API_KEY` |

Remove after cutover: `firebaseAnchorsUrl` on Dev / Staging / Prod assets.

## Client helpers (already scaffolded)

| Firebase | Replacement |
|---|---|
| `FirebaseExchanger.FetchAnchorsFromServer` | `AnchorBackendClient.ListAnchors` |
| `FirebaseExchanger.PutAnchorsRoutine` | `AnchorBackendClient.CreateAnchor` |
| `FirebaseExchanger.FindAnchorByName` | filter `ListAnchors` result locally |
| `FirebaseExchanger.CheckForNameConflict` | same filter on list |

## Suggested adapter (minimal diff)

Option A — **rename in place:** replace HTTP calls inside `FirebaseExchanger` with
`AnchorBackendClient` coroutines; keep public API (`PutAnchorsAndWait`,
`RefreshAnchorsAndWait`, `FindAnchorByName`) unchanged so `CreateASA`, `FindASA`,
and `RoomManager` need no edits.

Option B — **new component:** `AnchorExchangerBackend` implementing the same
public surface; swap the component on the prefab and delete `FirebaseExchanger`.

Prefer **Option A** for fewer scene/prefab diffs.

## Cutover steps

1. Deploy Function + Table Storage ([`docs/azure-function-provisioning.md`](azure-function-provisioning.md)).
2. Set `sasEndpointBaseUrl` / `sasApiKey` on active RemoteConfig.
3. Swap fetch/create implementation in `FirebaseExchanger` (or adapter).
4. Smoke on Quest:
   - GET list returns (empty or migrated records)
   - POST create → name appears in GET list
   - Find-by-name returns ASA/Meta anchor identifier
5. One-time migration script (optional): GET Firebase URL, POST each non-expired
   record to `/api/anchors`.
6. Remove `firebaseAnchorsUrl`; grep repo for `firebaseio.com` → zero hits.
7. Make Firebase DB read-only → delete after 30 days.

## Expired anchor filtering

Firebase client filters `dateExpired > DateTime.Now` on fetch. Preserve in the
adapter after `ListAnchors`:

```csharp
foreach (var entry in entries)
{
    if (DateTime.TryParse(entry.dateExpired, out var exp) && exp > DateTime.Now)
        fetched.Add(entry);
}
```

## Tests before merge

- EditMode: [`AnchorBackendClientTests.cs`](../Assets/Tests/EditMode/AnchorBackendClientTests.cs)
- Local Function: `./scripts/test_anchor_function.sh` with Azurite
  ([`docs/functions-local-dev.md`](functions-local-dev.md))
- Tier 2 networking harness unchanged until #23

## Related

- [`docs/firebase-anchor-audit.md`](firebase-anchor-audit.md) — Phase A audit
- [`docs/auth-backend.md`](auth-backend.md) — HTTP contracts
