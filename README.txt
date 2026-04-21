GeoXplorerSE

Single Unity project for both the MobileAR (iOS and Android) and HoloLens GeoXplorer applications.

This is an ongoing development to provide a shared AR experience between multiple platforms, including both remote and local sharing.
All scripts within the project are written with platform specific code so the same scripts can be used for whatever build platform.

Interactions are handled by MRTK.
Local sharing can be accomplished using Azure Spatial Anchors, or through manual placement of the anchor position. Manual placement is the only method for remote sharing.
Azure Spatial Anchor IDs are shared over a Google Firebase NoSQL database. Anchors are stored for 1 day.
State and Voice sharing is handled by Photon Engine.
Models are hosted on Azure Storage - stored as Unity Assetbundles

HoloLens development Unity version 2019.2.0f1
MobileAR development Unity version 2019.2.2f1
MRTK version 2.2.0
Visual Studio 19

****IMPORTANT****
****DO THIS BEFORE STARTING****

There is only one scene to build for any platform - GeoXShared. This is because Photon requires the same scene name.

When first using the project after downloading, replace the GeoXShared scene contents of GeoXShared with that of the build platform you want to build for. This would be either 'HoloLens' (For HoloLens 1 or 2) or 'MobileMRTK' (for iOS or Android). This should be the latest versions of the scene hierarchy.

After working on development for a particular platform, save any GeoXShared changes in the respective platform scene.

Push any scene changes for the platform scenes. DO NOT push scene changes to GeoXShared. The GeoXShared scene will always remain local to your machine(s).

Also do not share the ProjectSettings.asset and the EditorBuildSettings.asset or any ProjectVersion.txt file as this can lead to recompile issues across different platforms. Use the project settings that work for the platform of choice.

For HoloLens builds, make sure InternetClient, InternetClientServer, PrivateNetworkClientServer, WebCam, Microphone, Bluetooth, SpatialPerception, and RemoteSystem capabilities are all checked in Project Settings > Publishing Settings.

******************************

