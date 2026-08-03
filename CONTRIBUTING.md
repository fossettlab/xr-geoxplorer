# Contributing

## Unity Version

Use Unity **6000.4.4f1**, matching `ProjectSettings/ProjectVersion.txt`.

Open the project from the repository root. The main scene is:

```text
Assets/Scenes/GeoXShared.unity
```

For agent/MCP sessions, launch the Editor in **automated mode** (see [`AGENTS.md`](AGENTS.md)):

```bash
unity open "/path/to/xr-geoxplorer" --args "-automated"
```

## Branches And Pull Requests

- Branch from `main`.
- Use short, scoped branch names such as `codex/android-ci` or `fix/menu-null-guard`.
- Open pull requests against `main`.
- Prefer conventional commit-style summaries, for example:
  - `build: add Android compile workflow`
  - `fix: guard missing Photon room config`
  - `docs: update asset bundle bake notes`

## Local Build Check

Before opening a PR that changes scripts, packages, or project settings:

1. Open the project in Unity **6000.4.4f1** (or use `./scripts/unity.sh compile` with the GUI Editor closed).
2. Let Unity finish importing packages and compiling scripts.
3. Confirm the Console has no new red compile errors.
4. For Android/Quest-facing changes, switch Build Settings to Android and run a local Android build when possible.

Headless import/compile check (Editor must not already have this project open):

```bash
./scripts/unity.sh compile
```

Or explicitly:

```bash
'/Applications/Unity/Hub/Editor/6000.4.4f1/Unity.app/Contents/MacOS/Unity' \
  -batchmode \
  -quit \
  -projectPath /path/to/xr-geoxplorer \
  -logFile /tmp/xr-geoxplorer-import.log
```

## GitHub Actions

Pull requests to `main` run:

- `C# Lint`, which catches `yield return` inside `try` blocks with `catch`.
- `Android Unity Build`, which uses GameCI to build the Android target.
- `Unity Tests` (EditMode) and `Functions Tests` where applicable.

The Android build requires repository secrets:

- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

Maintainers must configure those secrets in GitHub before the Android gate can pass. UWP/HoloLens CI is intentionally not included for v1 because HoloLens 2 is best-effort and hosted UWP build cost is not justified yet.

Fork pull requests cannot access repository secrets. In that case, the Android build workflow reports a notice and skips the Unity build until a maintainer runs it from a trusted branch or merge context.

## Unity Analyzers

This repo includes `Microsoft.Unity.Analyzers` under:

```text
Assets/Plugins/Analyzers/Microsoft.Unity.Analyzers.dll
```

Unity recognizes the `RoslynAnalyzer` asset label on that DLL and runs the analyzer during script compilation. Analyzer warnings are there to catch Unity-specific C# mistakes earlier; treat them as review hints unless a PR explicitly turns a warning into a hard gate.

## Style

The root `.editorconfig` defines the shared formatting defaults:

- UTF-8 text
- LF line endings
- final newline
- 4-space C# indentation
- folder-matching namespaces as a suggestion

Do not run broad auto-formatting across unrelated legacy files. Keep formatting changes scoped to files you are already editing.

**Note:** tracked `.cs` files in this repo use **CRLF + UTF-8 BOM** historically. Preserve line endings on files you edit; do not normalize the whole tree.
