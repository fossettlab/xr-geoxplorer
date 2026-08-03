# Platform helper

`Assets/Scripts/Platform/Platform.cs` is the shared runtime platform helper for app-owned GeoXplorer code.

Use `Platform.Current` and the `Platform.Is...` properties when a script is deciding runtime behavior, such as which prefab to instantiate or whether a small legacy adjustment should run.

## Runtime platform ids

- `Editor`: running inside the Unity Editor.
- `Quest`: Android runtime that identifies as Quest, Oculus, or Meta XR hardware.
- `Mobile`: iOS or non-Quest Android runtime.
- `Other`: any runtime that is not one of the above.

HoloLens 2 was dropped as a target on 2026-06-06. Quest 3 is the only headset target.

## Use `Platform` for runtime decisions

Use the helper for decisions that happen while the app is running:

```csharp
if (Platform.IsQuest)
{
    // Quest runtime behavior.
}
```

This keeps reviewer searches simple: runtime platform branches in app-owned code should be discoverable by searching for `Platform.`.

## Keep `#if` for compile-time guards

Preprocessor directives such as `#if UNITY_ANDROID` and `#if UNITY_IOS` are still appropriate when code cannot compile on every target because it references platform-only APIs or packages.

Do not replace those guards with `Platform` unless the guarded code can compile on all active build targets.
