# Quest 3 Session 1 runbook — first light + perf baseline

**Purpose.** Get the app building, deploying, and running on a Quest 3, confirm
the basics work on real hardware, and capture a pre-URP performance baseline. This
session unblocks every other on-device ticket (it is the resolved blocker for the
Phase 3 XR-runtime work). Ticket: #10 (build + deploy + launch); it closes out
#9 (Android/XR settings), which is already configured (see below).

## Status: the project is already Quest-ready

Verified in the committed project — no settings changes needed before building:

- **XR:** OpenXR is the sole Android XR loader (ARCore removed for Android).
  OpenXR "Meta Quest Support" feature enabled, targeting Quest 3 / 3S. Meta Quest
  Touch Plus + Oculus Touch controller profiles and Hand Tracking enabled.
  Render mode single-pass instanced; depth submission on.
- **Player:** Android, IL2CPP, ARM64-only, Vulkan-only (GLES3 removed), min SDK 29,
  target SDK 34, .NET Standard 2.1. Active Input Handling = Both (leave it —
  a store-settings validator fails on anything else until MRTK3).
- **Manifest** (`Assets/Plugins/Android/AndroidManifest.xml`): VR intent category,
  hand-tracking, anchor, scene, and passthrough permissions all present — so it
  boots to VR, not a 2D panel.
- **Scene:** one scene in the build — `Assets/Scenes/GeoXShared.unity`.
- **Render pipeline:** Built-in RP (URP is #13, a later ticket). First light and
  the baseline happen on Built-in RP by design.

## Part A — one-time setup (Mac + headset, ~10 min)

1. Headset: enable Developer Mode (Meta Horizon phone app -> Devices ->
   Developer Mode), then allow USB debugging when prompted on-device.
2. Mac: install platform-tools (`adb`) — `brew install --cask android-platform-tools`
   — or install Meta Quest Developer Hub (MQDH) which bundles adb + logcat + a
   metrics HUD.
3. Connect the Quest by USB-C; `adb devices` should list it (accept the on-device
   "Allow USB debugging" dialog). Optional: `adb tcpip 5555` then
   `adb connect <headset-ip>:5555` for wireless deploy.

## Part B — build the APK

1. Unity -> File -> Build Settings. Confirm platform is **Android** (Switch
   Platform if not — first switch reimports, can take a while).
2. Confirm `Scenes/GeoXShared` is the only checked scene.
3. **Build And Run** (installs + launches), or **Build** then
   `adb install -r <apk>`. First IL2CPP build is slow (~10-20 min); later builds
   are faster.

## Part C — first-light checklist (on-device)

Put the headset on and confirm, in order:

- [ ] App boots into an immersive VR view (two eyes rendering), not a flat panel.
- [ ] Head tracking: the view responds to head movement (6DoF).
- [ ] Controllers (Touch Plus) are tracked and their input reaches the app.
- [ ] Hand tracking: setting the controllers down and using hands is tracked.
- [ ] The scene's core content renders (globe / models / UI). Note anything
      missing or mis-shaded.

## Part D — likely first-run surprises and fixes

The "2-3 things that only surface on hardware." Most likely, in order:

- **Magenta/pink surfaces** — Built-in RP shaders that are not mobile/Quest
  compatible. Expected pre-URP; note which materials so #13 (URP + shader
  rewrites) can target them. Not a blocker for the baseline.
- **App launches but stays 2D / "phone" panel** — would mean the VR manifest
  category is not being applied; it is present in the committed manifest, so this
  is unlikely, but if it happens confirm the built APK's manifest merged it
  (`adb shell dumpsys package <pkg>` or inspect the APK).
- **Black screen / XR not initializing** — check the on-device OpenXR runtime is
  Meta's; check logcat (`adb logcat -s Unity Unity-XR OpenXR`).
- **Networking noise** — the app will try to reach Photon; harmless offline. Do
  not chase network errors this session.
- **Crash on load** — capture `adb logcat -s Unity` and the stack; file it.

## Part E — capture the pre-URP perf baseline (the reason this session matters)

This baseline is the regression target for the URP migration (#13). Capture it on
Built-in RP now, before URP changes anything.

1. Enable a metrics HUD: OVR Metrics Tool (`adb shell setprop debug.oculus.metrics ...`
   via the app), or MQDH's performance panel, or `adb shell` OVR metrics.
2. Stand in a representative scene view and record, for ~30-60 s:
   - Frame rate and target refresh (72 / 90 Hz), and **stale/dropped frames**.
   - GPU and CPU frame time (ms), and the fixed clock levels (GPU/CPU level).
   - App + compositor GPU utilization.
3. Write the numbers down (a table in the PR/notes). These become the pass bar for
   the #13 URP perf-regression gate: URP must match or beat them.

## Part F — scope note (what is NOT in this session)

Later sessions, each after its own headless prep, along the dependency chain:
URP (#13) -> MRTK3 runtime (#14) -> UI rebuild (#15) -> hand-tracking/interaction
(#16) -> anchors/passthrough/audio (#17/#18/#19) -> HW smoke + store (#32/#33).
The networking spike (#22, NGO/Relay/Vivox) has no prerequisites and can run in a
parallel session whenever convenient; it needs its own scaffolding first.
