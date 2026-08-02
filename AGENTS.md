# xr-geoxplorer

Guidance for coding agents and contributors working in this repo.

## Commit messages

This repo has student/collaborator contributors. Commit messages here
target them — high-level, plain English:

- Subject ≤72 chars saying what changed, from a repo user's point of view.
- Body: 2–4 sentences on what changed and why it matters to someone using
  the repo. No internal infrastructure jargon (run ids, hostnames,
  pipeline internals, phase numbers).
- Mechanism detail belongs in the PR description or code comments, not
  the commit message.

## Unity agent workflow

Use the official **Unity CLI** (`unity`) and **Unity MCP** server (configured
in `.cursor/mcp.json`) instead of GUI automation when interacting with the
Editor.

- Keep the Unity Editor open with this project loaded for connected-Editor
  commands (`unity status`, `unity command`, `unity list`).
- Inspect available Editor commands with `unity command` and `unity list`
  before invoking anything.
- Prefer registered Pipeline commands over arbitrary eval or undocumented APIs.
- Do not invoke destructive Editor actions (delete assets, modify scenes,
  enter Play Mode that mutates state) without explicit user approval.
- After C# changes, wait for Unity compilation to finish and check Console
  errors before proceeding.
- Run relevant Edit Mode or Play Mode tests (`unity test`) before declaring
  work complete.

**Editor connection requirement:** The official `com.unity.pipeline` package
(required for CLI/MCP Editor access) needs **Unity 6.0+**. This project targets
**2022.3.62f2** (Quest 3 port); Pipeline cannot be installed until the Editor
version is upgraded. Until then, CLI/MCP config is in place but live Editor
tools are unavailable—use file-based inspection and `tools/yield-lint` instead.

**HoloLens / UWP:** Not supported. Quest 3 and mobile are the only headset/AR
targets. Legacy HoloLens prefabs and scenes remain under `Assets/Scenes/_legacy/`
for reference only.
