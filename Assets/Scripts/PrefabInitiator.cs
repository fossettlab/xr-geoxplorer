using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabInitiator : MonoBehaviour
{
    public string prefabName;
    // Start is called before the first frame update
    void Start()
    {
        this.name = prefabName;
        transform.localPosition = Vector3.zero;
        transform.localEulerAngles = new Vector3(0, 180, 0);
        PlanetManager planetManager = ResolvePlanetManager();
        if (planetManager != null)
        {
            planetManager.activePlanet = this.gameObject;
        }
        else
        {
            Debug.LogWarning("PrefabInitiator could not find a PlanetManager to mark the active planet.");
        }

        gameObject.AddComponent<ManipulationHandler>();
        gameObject.GetComponent<ManipulationHandler>().OneHandRotationModeNear = ManipulationHandler.RotateInOneHandType.RotateAboutObjectCenter;
        gameObject.GetComponent<ManipulationHandler>().OneHandRotationModeFar = ManipulationHandler.RotateInOneHandType.RotateAboutObjectCenter;

    }

    private static PlanetManager ResolvePlanetManager()
    {
        if (LobbyManager.Instance != null)
        {
            return LobbyManager.Instance.ResolvePlanetManager();
        }

        return TableAnchor.instance != null ? TableAnchor.instance.GetComponent<PlanetManager>() : null;
    }
}
