# Quest Android Store Settings

Issue #9 locks the Android/Quest project settings that must be stable before
store-track builds. Run the configurator after package restore or after changing
Android build support modules:

```text
GeoXplorer > XR > Configure Quest Android Store Settings
```

The command also runs:

```text
GeoXplorer > XR > Validate Quest Android Store Settings
```

## Serialized Settings

The configurator sets the Android player to:

- Unity 2022.3 Android target with IL2CPP, ARM64 only, Linear color space, and
  .NET Standard API compatibility.
- Minimum API level Android 10/API 29.
- Target API level Android 14/API 34.
- Internet Access set to Require.
- Package name `edu.wustl.fossettlab.xrgeoxplorer`.
- Vulkan as the only Android graphics API.
- ASTC as the selected Android texture compression build target.
- Custom main manifest, main Gradle template, Gradle properties template, and
  Gradle settings template enabled.

The OpenXR Android settings are kept on OpenXR with Single Pass Instanced /
Multi-view rendering and 16-bit depth submission. The Meta Quest, Oculus Touch,
Meta Quest Touch Plus, and Meta Hand Tracking Aim OpenXR features are enabled by
the OpenXR migration configurator.

## Android Manifest

`Assets/Plugins/Android/AndroidManifest.xml` is intentionally committed because
Meta Quest store permissions and features need to be explicit:

- `android.permission.INTERNET`
- `android.permission.RECORD_AUDIO`
- `com.oculus.permission.USE_ANCHOR_API`
- `com.oculus.permission.USE_SCENE`
- `oculus.software.handtracking` with `android:required="false"`
- `com.oculus.feature.PASSTHROUGH` with `android:required="true"`

The `com.oculus.supportedDevices` device-targeting metadata is intentionally
*not* declared here: the Meta Quest OpenXR feature injects it at build time from
its Target Devices setting, and a hand-written value collides with the feature's
during manifest merge. Set the target device list through the Meta Quest feature
in Project Settings > XR Plug-in Management > OpenXR, not in this manifest.

## Signing

Do not commit a keystore, passwords, or signing credentials. Use a private lab
keystore location for release signing and document password recovery separately
from the repository.

## Manual Validation

1. Open the project in Unity 2022.3 with Android Build Support installed.
2. Run `GeoXplorer > XR > Configure Quest Android Store Settings`.
3. Confirm the Unity Console has no compile errors.
4. Open Project Settings > Player > Android and confirm the API levels, IL2CPP,
   ARM64, Linear, Internet Require, package name, Vulkan, and custom template
   toggles.
5. Open Project Settings > XR Plug-in Management > Android and confirm OpenXR is
   enabled with Meta Quest features.
6. Switch Build Target to Android and run a development build if the local
   Android SDK/JDK/NDK modules are installed.
