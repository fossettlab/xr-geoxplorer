# Quest 3 Build And Deploy

Issue #10 is the first end-to-end **build → deploy → launch** path on a Meta Quest 3.
Use this doc whenever you need a launchable Android APK on headset hardware.

Related settings (do not re-tune casually):

- Store / API gates: [`docs/quest-android-store-settings.md`](quest-android-store-settings.md) (issue #9)
- OpenXR migration notes: [`docs/openxr-migration-notes.md`](openxr-migration-notes.md) (issue #7)

## Prerequisites

| Requirement | Notes |
|-------------|-------|
| Unity **6000.4.4f1** | Matches `ProjectSettings/ProjectVersion.txt`. Install **Android Build Support** (SDK + NDK + OpenJDK) via Unity Hub. |
| Quest 3 in **Developer Mode** | Meta Quest mobile app → headset → Settings → Developer Mode. |
| USB debugging allowed | Connect USB-C; accept **Allow USB debugging** on the headset when prompted. |
| `adb` | Comes with Unity's Android SDK (see path below). Optional: put it on your `PATH`. |
| Meta Quest Developer Hub (MQDH) | Optional but recommended for device logs, APK install, and performance captures. Download from [Meta Horizon docs](https://developers.meta.com/horizon/documentation/unity/ts-odh/). |

### `adb` path (macOS, Unity Hub install)

```bash
export ADB="/Applications/Unity/Hub/Editor/6000.4.4f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb"
"$ADB" version
"$ADB" devices -l
```

Expect a line like `XXXXXXXX device product:… model:Quest_3 …` when the headset is connected and authorized.
If you see `unauthorized`, put the headset on and accept the USB debugging prompt, then re-run `devices`.

### Confirm package / scene targets

These values are already serialized in the repo:

| Setting | Expected value |
|---------|----------------|
| Product name | `GeoXplorerM` |
| Android package | `edu.wustl.fossettlab.xrgeoxplorer` |
| Build scene | `Assets/Scenes/GeoXShared.unity` only |
| Min / target SDK | 29 / 34 |
| Architecture | ARM64 |
| Graphics API | Vulkan only |
| XR loader (Android) | OpenXR + Meta Quest feature group |

Before a first build on a fresh machine, run:

```text
GeoXplorer > XR > Validate Quest Android Store Settings
```

If validation fails, run `GeoXplorer > XR > Configure Quest Android Store Settings`, then validate again.

## Unity Build And Run

1. Open the project in **Unity 6000.4.4f1**.
2. **File → Build Settings…**
   - Platform: **Android** → **Switch Platform** if needed.
   - Scenes In Build: only `Assets/Scenes/GeoXShared.unity` (enabled).
3. Connect the Quest 3; confirm `"$ADB" devices` shows `device` (not `offline` / `unauthorized`).
4. In Build Settings, select the Quest under **Run Device** (or leave default if only one Android device is attached).
5. Click **Build And Run**.
   - Choose an output folder outside the repo if you prefer (for example `~/Builds/GeoXplorerQuest/`).
   - Unity builds the APK, installs it, and launches the app.

### Build only (no auto-launch)

**File → Build Settings → Build**, then install manually:

```bash
export ADB="/Applications/Unity/Hub/Editor/6000.4.4f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb"
"$ADB" install -r "/path/to/GeoXplorerM.apk"
"$ADB" shell am start -n edu.wustl.fossettlab.xrgeoxplorer/com.unity3d.player.UnityPlayerActivity
```

### Development builds

For Logcat / debugging, enable **Development Build** in Build Settings. Leave **Autoconnect Profiler** off unless you intentionally want the Profiler attached (it can change timing).

## MQDH workflow

1. Install and open **Meta Quest Developer Hub**.
2. Connect the Quest (USB or pair over the network per MQDH prompts).
3. Confirm the device appears as online.
4. Use MQDH to:
   - Install an APK built from Unity (**Build** only)
   - Capture device logs during launch
   - Take performance / frame captures later (issue #11)

MQDH does not replace Unity Build And Run for day-to-day iteration; it is the preferred place for store-adjacent device tooling and log collection.

## First-launch smoke checklist (issue #10 DoD)

Put the headset on after Build And Run:

1. App launches and stays up for **≥ 30 seconds** (no immediate crash to Home).
2. You see **stereo VR rendering** of `GeoXShared` (magenta / broken materials are OK until URP).
3. **Head tracking** works (looking around updates the view).
4. Optional: confirm `PlatformBootstrapper` selected the Quest platform root (no HoloLens / wrong prefab).

Out of scope for #10: controller/hand input, MRTK3, anchors, passthrough correctness, networking, shader fidelity.

## Logcat

Stream Unity logs while launching:

```bash
export ADB="/Applications/Unity/Hub/Editor/6000.4.4f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb"
"$ADB" logcat -c
# Launch the app from the headset or via Build And Run, then:
"$ADB" logcat -s Unity ActivityManager AndroidRuntime
```

Capture the first ~30 seconds for the issue attachment:

```bash
"$ADB" logcat -d > quest3-firstboot.log
```

Attach `quest3-firstboot.log` to [#10](https://github.com/fossettlab/xr-geoxplorer/issues/10).

### What “clean enough” means

- No repeating `AndroidRuntime` FATAL EXCEPTION / native crash loops.
- Unity reaches scene load without a hard abort.
- Warnings about missing AssetBundles, Photon, or magenta shaders are expected at this stage.
- Treat missing OpenXR session / black eye buffers / immediate process death as blockers.

## Troubleshooting

| Symptom | Likely fix |
|---------|------------|
| `adb devices` empty | Cable, USB mode, Developer Mode, or try another port/cable. |
| `unauthorized` | Accept USB debugging prompt inside the headset. |
| Build fails on Gradle / ASA Maven | Confirm custom Gradle templates are enabled (#9). See `Assets/Plugins/Android/settingsTemplate.gradle`. |
| Install succeeds, black screen | Confirm OpenXR Android loader + Meta Quest Support; check Logcat for XR provider errors. |
| Wrong package / old build | Uninstall first: `"$ADB" uninstall edu.wustl.fossettlab.xrgeoxplorer` |
| Editor missing Android module | Unity Hub → Installs → 6000.4.4f1 → Add Modules → Android Build Support. |

## Signing note

Debug / Development Build And Run uses the Unity debug keystore. Do **not** commit a release keystore. Release / App Lab signing is documented under the #9 store-settings notes and stays in a private secrets store.

## Verification status

Update this section when someone completes a formal headset pass:

| Check | Status |
|-------|--------|
| `adb` available via Unity Android SDK | Verified on lab Mac paths above |
| Quest 3 attached + authorized (`adb devices`) | Verified (Quest 3 `device`) |
| MQDH installed | Optional; Build And Run used instead |
| Unity Build And Run → launch ≥ 30s | **Pass** (2026-07-24) — app stayed up, no crash to Home |
| Head tracking / stereo view | **Pass** — opaque dark blue-gray clear visible in stereo VR (camera alpha was 0 / passthrough-required before; fixed for opaque OpenXR) |
| `quest3-firstboot.log` attached to #10 | Excerpt posted on issue #10; full log kept locally under `tmp/` (gitignored) |
