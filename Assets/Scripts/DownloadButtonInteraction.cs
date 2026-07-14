using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;

public class DownloadButtonInteraction : MonoBehaviour
{
    Material instanceMaterial;
    Color instanceOriginalColor;

    //Public variables
    public string storageAccountName;
    public string containerName;
    public string prefabName;
    public string bundleName;
    public string modelName;
    //public Material mobileMaterial;


    public void OnSelect()
    {
        LobbyManager.Instance.CreateInteractableObjects(storageAccountName, containerName, prefabName, bundleName, modelName);
    }
}