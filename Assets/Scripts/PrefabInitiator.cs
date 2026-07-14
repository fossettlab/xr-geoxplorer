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
        LobbyManager.Instance.PlanetManager.activePlanet = this.gameObject;

        gameObject.AddComponent<ManipulationHandler>();
        gameObject.GetComponent<ManipulationHandler>().OneHandRotationModeNear = ManipulationHandler.RotateInOneHandType.RotateAboutObjectCenter;
        gameObject.GetComponent<ManipulationHandler>().OneHandRotationModeFar = ManipulationHandler.RotateInOneHandType.RotateAboutObjectCenter;

    }
}
