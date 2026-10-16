# Next Quest session — desk-ready sideloads

Use this when a USB-debugging-authorized Quest 3 is connected. The software
APKs below already exist. This session is hardware acceptance, not a new
desktop implementation pass. Do not treat a successful install as closure.

`adb` (Unity 6000.4.4f1):

```bash
export ADB="/Applications/Unity/Hub/Editor/6000.4.4f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb"
"$ADB" devices -l
```

Expect a `Quest_3` line marked `device`. Accept the headset USB-debugging prompt
if it shows `unauthorized`.

## 1. Current GeoXplorer baseline — `geox-3kj` / #10

APK: `build/GeoXplorer-openxr-1.17.0-2026-09-07.apk`  
SHA-256: `85315d46e3fc344d219c39c5599167fd1a504c9cf111efaabc47a670430226c8`  
Package: the product identifier already on that build  
Receipt: `docs/android-build-checkpoint-2026-09-07.md`

Install, launch, and record boot, stereo, tracking, and the current
content/room/anchor flows. Keep failures as baseline findings. The July 2025
pass is not this APK.

```bash
"$ADB" install -r build/GeoXplorer-openxr-1.17.0-2026-09-07.apk
```

## 2. PROJ probe, offline — `geox-k1v.7.1`

APK: `build/proj-qualification/GeoX-ProjQualification-2026-09-09.apk`  
Package: `edu.wustl.fossett.geoxprojqualification`  
Receipt: `docs/proj-qualification-2026-09-09.md`

Disable headset network before launch. This is a separate development app. It
does not replace GeoXplorer.

```bash
"$ADB" install -r build/proj-qualification/GeoX-ProjQualification-2026-09-09.apk
"$ADB" shell am start -n edu.wustl.fossett.geoxprojqualification/com.unity3d.player.UnityPlayerGameActivity
```

Retrieve:

```text
/storage/emulated/0/Android/data/edu.wustl.fossett.geoxprojqualification/files/proj-qualification/
```

- `android-managed-receipt.json`
- `android-report.json`
- `android-missing-database-report.json`
- `android-recovery-report.json`

Confirm the path on device. A launch without those files does not close `.7.1`.
After a passing offline run, record the provider disposition before starting
`.7.2`.

## 3. Isolated glTFast player — `geox-k1v.2.1`

APK: `build/gltf-importer-trial/GeoX-GltfTrial-2026-09-12.apk`  
SHA-256: `e06d7a3fae7813c8026e2ba1347b6ae380749159a207c7d782a82307b6bb5590`  
Receipt: `docs/contracts/gltf-importer-android-2026-09-12.json`

Package: `edu.wustl.fossett.geoxgltftrial`  
This player imports packaged fixtures only. It has no public network fetch.

Launch, then retrieve:

```text
/storage/emulated/0/Android/data/edu.wustl.fossett.geoxgltftrial/files/gltf-importer-trial/android-importer-receipt.json
```

Confirm the receipt belongs to the current attempt and reaches `status=passed`
with `success=true`. An `in_progress` receipt is unfinished, not a pass. Retain
its unique per-attempt receipt alongside the canonical file.

Record device identity, shader names, pass/fail per fixture, and any memory or
load-time notes. Those notes can inform `.2.2` budgets. They are not themselves
a production limit.

## Order and stop rules

1. Baseline GeoXplorer first if time is short; it unblocks the hardware chain.
2. PROJ next if the probe APK is still unrun; it unblocks `.7.2`.
3. Importer player last; it can close `.2.1` only after a recorded recommendation.
4. Do not change `Packages/manifest.json` or production settings from headset
   notes in this session.
5. Do not start `.7.2`, `.2.4` or URP from a green install alone.
