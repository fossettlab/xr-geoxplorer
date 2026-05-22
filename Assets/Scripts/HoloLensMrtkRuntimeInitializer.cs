using Microsoft.MixedReality.Toolkit;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class HoloLensMrtkRuntimeInitializer : MonoBehaviour
{
    [SerializeField]
    private MixedRealityToolkitConfigurationProfile activeProfile;

    [SerializeField]
    private string runtimeObjectName = "MixedRealityToolkit";

    private void Awake()
    {
        if (activeProfile == null)
        {
            Debug.LogWarning("HoloLens MRTK runtime initializer has no active profile assigned.", this);
            return;
        }

        MixedRealityToolkit existingToolkit = MixedRealityToolkit.Instance;
        if (existingToolkit != null)
        {
            if (!existingToolkit.HasActiveProfile)
            {
                existingToolkit.ActiveProfile = activeProfile;
            }

            return;
        }

        GameObject toolkitObject = new GameObject(runtimeObjectName);
        MixedRealityToolkit toolkit = toolkitObject.AddComponent<MixedRealityToolkit>();
        toolkit.ActiveProfile = activeProfile;
    }
}
