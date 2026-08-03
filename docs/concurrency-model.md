# Concurrency model (xr-geoxplorer)

Quest-first Unity project conventions for async code, HTTP, and long-running work.
Applies to **new and touched code**; existing coroutines are not bulk-migrated.

## Default: `async Task`

Prefer `async Task` methods called from `Start`/`Awake` via a thin sync entry point:

```csharp
async void Start()
{
    try
    {
        await InitializeAsync();
    }
    catch (Exception ex)
    {
        Debug.LogException(ex);
    }
}

async Task InitializeAsync() { /* ... */ }
```

Unity allows `async void` only on **event handlers** (including `Start`/`Update` subscribers).
Every `async void` entry point must wrap its body in `try/catch` so failures surface in the Console.

Do **not** add new `async void` helpers — only Unity callbacks.

## Coroutines (`IEnumerator`)

Use coroutines only when a Unity API requires yield semantics that `async Task` cannot
express cleanly (e.g. `WaitForEndOfFrame`, chained `yield return` with legacy Firebase helpers).

Prefer converting new wait loops to `await Task.Delay` with cancellation when touching nearby code.

## Long-running loops

- Every background loop must observe a **`CancellationToken`** (or a bounded lifetime tied to a `MonoBehaviour` / service `Dispose`).
- No new unbounded `while (true)` without cancellation.
- No new `Task.Factory.StartNew` / `Task.Run` for polling; use `async Task` loops with `await Task.Delay` and a token.

```csharp
async Task PollAsync(CancellationToken token)
{
    while (!token.IsCancellationRequested)
    {
        await DoWorkAsync();
        await Task.Delay(500, token);
    }
}
```

## `HttpClient`

- **One shared instance per type** (static readonly or injected singleton). Do not `new HttpClient()` per call.
- Set `BaseAddress` / default headers once if needed.
- See [Microsoft HttpClient guidelines](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines).

## UniTask

Not a project dependency today. If adopted later, document the migration here; until then use BCL `Task`.

## Related tickets

- **#28** — async hygiene sweep (this doc + grep acceptance criteria)
- **#17 / #23** — anchor/network rewrites that may supersede legacy `AnchorExchanger` / ASA scripts
