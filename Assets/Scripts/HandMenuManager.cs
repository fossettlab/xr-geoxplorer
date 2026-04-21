using Microsoft.MixedReality.Toolkit.UI;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HandMenuManager : MonoBehaviour
{
    LobbyManager lobbyManager;
    bool modelMoveToggleBool;
    bool modelFlagToggleBool;

    private void Start()
    {
        modelMoveToggleBool = false;
        modelFlagToggleBool = false;
    }

    public void SetActiveModelText(GameObject titleText)
    {
        GameObject activeModel = GameObject.FindGameObjectWithTag("activeModel");
        if (activeModel != null)
        {
            titleText.GetComponent<TextMeshPro>().text = "Active model: " + activeModel.GetComponent<FetchAssetBundle>().modelName;
        }
        else
        {
            titleText.GetComponent<TextMeshPro>().text = "";
        }
    }

    public void ModelMoveToggle()
    {
        if (!modelMoveToggleBool)
        {
            GameObject asset = GameObject.FindGameObjectWithTag("activeModel");

            asset.AddComponent<ObjectManipulator>();
            asset.GetComponent<ObjectManipulator>().OneHandRotationModeFar = ObjectManipulator.RotateInOneHandType.RotateAboutObjectCenter;
            asset.AddComponent<RotationAxisConstraint>().ConstraintOnRotation = Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.YAxis;

            modelMoveToggleBool = true;
        }
        else
        {
            GameObject asset = GameObject.FindGameObjectWithTag("activeModel");

            Destroy(asset.GetComponent<ObjectManipulator>());
            Destroy(asset.GetComponent<RotationAxisConstraint>());

            modelMoveToggleBool = false;
        }
    }

    public void ModelAddFlag()
    {
        if (!modelFlagToggleBool)
        {
            GameObject activeModel = GameObject.FindGameObjectWithTag("activeModel");
            if (activeModel != null)
            {
                activeModel.GetComponent<OnClickModelInteraction>().flagSelected = true;
            }
            modelFlagToggleBool = true;
        }
        else
        {
            GameObject activeModel = GameObject.FindGameObjectWithTag("activeModel");
            if (activeModel != null)
            {
                activeModel.GetComponent<OnClickModelInteraction>().flagSelected = false;
            }
            modelFlagToggleBool = false;
        }
    }

    public void ModelDelete()
    {
        GameObject activeModel = GameObject.FindGameObjectWithTag("activeModel");

        PhotonView PV = activeModel.GetComponent<PhotonView>();
        if (PV.IsMine)
        {
            GameObject[] toolTips = GameObject.FindGameObjectsWithTag("OutcropTooltip");
            if (toolTips.Length > 0)
            {
                foreach (var tool in toolTips)
                {
                    Destroy(tool);
                }
            }
            lobbyManager.DeleteAssetBundle(activeModel.gameObject);
        }
    }
}
