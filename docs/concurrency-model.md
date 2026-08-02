# Concurrency model (issue #28)

Conventions for new async and networking code in GeoXplorer app scripts
(`Assets/Scripts/`). Existing coroutines and legacy anchor code are left alone
until #17 / #23 rewrites land; this doc applies to **new** work and to small
hygiene fixes like the anchor startup paths.

## Default: `async Task`

Prefer `async Task` (or `async Task<T>`) for asynchronous work initiated from
Unity lifecycle methods.

Unity `Start`, `Awake`, and `OnEnable` must remain `void`. Kick off async work
explicitly and handle failures:

```csharp
void Start()
{
    _ = RunStartupAsync();
}

private async Task RunStartupAsync()
{
    try
    {
        await DoWorkAsync();
    }
    catch (Exception ex)
    {
        Debug.LogException(ex);
    }
}
```

Do **not** use `async void` except for Unity event handlers that require a
`void` signature (button callbacks, some SDK events). Every `async void` handler
must wrap its body in `try/catch` and log via `Debug.LogException`.

## When `IEnumerator` coroutines are acceptable

Keep coroutines when wrapping Unity APIs that only expose yield instructions, for
example:

- `UnityWebRequest` send loops (existing pattern in `FetchAssetBundle`)
- `WaitForEndOfFrame`, `WaitForSeconds`, asset-bundle download pipelines tied to
  `MonoBehaviour` lifecycle

Do not migrate working coroutines to `async Task` unless you are already editing
that file for another reason.

## Long-running loops: `CancellationToken`

Any background poll loop must be cancellable. Pattern used in
`AnchorExchanger.WatchKeys`:

```csharp
private CancellationTokenSource watchCancellation;

public void StopWatching()
{
    watchCancellation?.Cancel();
    watchCancellation?.Dispose();
    watchCancellation = null;
}

public void WatchKeys(string url)
{
    StopWatching();
    watchCancellation = new CancellationTokenSource();
    CancellationToken token = watchCancellation.Token;

    _ = Task.Run(async () =>
    {
        while (!token.IsCancellationRequested)
        {
            await PollOnceAsync();
            try
            {
                await Task.Delay(500, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }, token);
}
```

Avoid `Task.Factory.StartNew` with unbounded loops and no cancellation.

## `HttpClient` lifecycle

Use **one shared `HttpClient` per type** (static readonly field). Do not
construct `new HttpClient()` per request — socket exhaustion and slow DNS follow.

```csharp
private static readonly HttpClient Http = new HttpClient();
```

For Unity runtime HTTP to non-Azure endpoints, prefer `UnityWebRequest` so TLS
and platform certificate stores stay consistent with the rest of the app.

## UniTask

[UniTask](https://github.com/Cysharp/UniTask) is a good fit for Unity but is **not**
a project dependency today. Do not add it in a drive-by PR; raise on the epic if
the team wants to adopt it.

## Verification commands

```bash
# Should return no hits in app scripts (event-handler async void only, with try/catch):
rg -n 'async void' Assets/Scripts --glob '*.cs'

# Should return at most the shared singleton initializer:
rg -n 'new HttpClient' Assets/Scripts --glob '*.cs'
```

## Related docs

- Unity async/await: https://docs.unity3d.com/2022.3/Documentation/Manual/AsyncAwait.html
- HttpClient guidelines: https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines
- SAS client (uses `UnityWebRequest`): [`RestrictedBundleSasClient.cs`](../Assets/Scripts/Config/RestrictedBundleSasClient.cs)
