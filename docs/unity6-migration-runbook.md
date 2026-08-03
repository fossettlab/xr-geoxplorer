# Unity 6 migration runbook (Mac session)

> **Status: COMPLETED (2026-08-03).** Unity 6 landed on `main` via PR #162
> (`6000.4.4f1`). This runbook is kept as historical operator notes for the
> migration session; do not follow it for day-to-day work — use [`HANDOFF.md`](../HANDOFF.md)
> and [`AGENTS.md`](../AGENTS.md) instead.

**Branch:** `cursor/unity6-migration-50b2` (superseded)

This runbook covers the Mac Unity Editor work for upgrading xr-geoxplorer from
Unity **2022.3.62f2** to Unity **6000.x LTS**. Run it **after** the HoloLens
deprecation PR merges to `main`, then rebase this branch onto updated `main`.

## Prerequisites

- Unity Hub with **6000.x LTS** (latest stable patch) installed alongside **2022.3.62f2**
- Meta Quest 3 + USB debugging (for Phase B2 hardware validation)
- [`docs/quest3-build-and-deploy.md`](quest3-build-and-deploy.md) and
  [`docs/quest-session-1-runbook.md`](quest-session-1-runbook.md)

```bash
git fetch origin main
git checkout cursor/unity6-migration-50b2
git rebase origin/main   # after HoloLens deprecation PR merges
```

---

## Phase B1 — Validate HoloLens cleanup (2022.3 first)

Do this on **2022.3.62f2** before opening the project in Unity 6.

1. Open the project in Unity **2022.3.62f2**.
2. Confirm the Console has **no missing-script warnings** from deleted HoloLens assets.
3. Run **GeoXplorer → Scene Architecture → Validate Scene Architecture**.
4. Open `Assets/Scenes/GeoXShared.unity` and enter Play Mode with:
   - `PlatformBootstrapper` override = **Quest3**
   - `PlatformBootstrapper` override = **Mobile**
5. Optional UWP Player Settings cleanup (if not already cleared in the HoloLens PR):
   - **Edit → Project Settings → Player → Windows Store Apps** — clear certificate path
   - Remove unused `Windows Store Apps` scripting defines if Unity still shows them
   - **Quality Settings** — review the `Windows Store Apps` tier (legacy, harmless if left)

Record any issues in the Unity 6 PR before proceeding.

---

## Phase B2 — Upgrade to Unity 6

1. Close Unity 2022.3.
2. Open the project in Unity **6000.x LTS**; accept package resolution and API upgrade prompts.
3. Let Unity reimport fully; fix compile errors before touching packages.
4. Verify/update key packages in **Window → Package Manager** (or `Packages/manifest.json`):

   | Package | Current (2022.3) | Action |
   |---|---|---|
   | `com.unity.xr.openxr` | 1.14.2 | Bump to Unity 6–compatible version |
   | `com.unity.xr.management` | 4.4.0 | Bump as needed |
   | `com.unity.inputsystem` | 1.6.3 | Bump as needed |
   | `com.unity.xr.arfoundation` (+ arcore/arkit) | 5.2.0 | Bump as needed |

5. Confirm `ProjectSettings/ProjectVersion.txt` shows the Unity 6 editor version.
6. **File → Build Settings → Android** — build APK per [`quest3-build-and-deploy.md`](quest3-build-and-deploy.md).

   Or from terminal (if `scripts/unity.sh` is on the branch):

   ```bash
   UNITY="/Applications/Unity/Hub/Editor/<6000.x>/Unity.app/Contents/MacOS/Unity" \
     ./scripts/unity.sh compile
   UNITY="..." ./scripts/unity.sh build-android
   ```

7. Deploy to Quest 3 and run the Session 1 first-light checklist in
   [`quest-session-1-runbook.md`](quest-session-1-runbook.md).

---

## Phase B3 — Explicitly out of scope for the first Unity 6 PR

Do **not** bundle these into the initial Unity 6 upgrade PR:

- MRTK 2 → MRTK3 (#14)
- Built-in RP → URP (#13)
- Full Azure Spatial Anchors removal (#17)
- Bulk `Assets/MRTK/` vendor deletion

Ship a **compiling Unity 6 + Quest APK** first; follow-on PRs own the rest.

---

## PR checklist

When opening the Unity 6 draft PR, include:

- [ ] Unity 6 editor version used (`ProjectVersion.txt`)
- [ ] Package versions chosen (manifest diff summary)
- [ ] Android build succeeded (local or CI)
- [ ] Quest 3 first-light checklist results (or note blockers)
- [ ] Any manual migration steps not captured in git

---

## Coordination

- **Critical path** remains Unity 2022.3 until this PR is validated on Quest hardware.
- Comment on epic [#1](https://github.com/fossettlab/xr-geoxplorer/issues/1) when the Unity 6 PR is open.
- If Unity 6 fights MRTK2 too hard, escalate to a stability branch and keep shipping on 2022.3.
