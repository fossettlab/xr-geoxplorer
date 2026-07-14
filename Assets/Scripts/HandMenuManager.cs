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
        lobbyManager = LobbyManager.Instance;
        modelMoveToggleBool = false;
        modelFlagToggleBool = false;
    }

    public void SetActiveModelText(GameObject titleText)
    {
        GameObject activeModel = SceneQueries.OneWithTag("activeModel");
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
        GameObject asset = SceneQueries.OneWithTag("activeModel");
        if (asset == null)
        {
            return;
        }

        if (!modelMoveToggleBool)
        {
            asset.AddComponent<ObjectManipulator>();
            asset.GetComponent<ObjectManipulator>().OneHandRotationModeFar = ObjectManipulator.RotateInOneHandType.RotateAboutObjectCenter;
            asset.AddComponent<RotationAxisConstraint>().ConstraintOnRotation = Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.YAxis;

            modelMoveToggleBool = true;
        }
        else
        {
            Destroy(asset.GetComponent<ObjectManipulator>());
            Destroy(asset.GetComponent<RotationAxisConstraint>());

            modelMoveToggleBool = false;
        }
    }

    public void ModelAddFlag()
    {
        if (!modelFlagToggleBool)
        {
            GameObject activeModel = SceneQueries.OneWithTag("activeModel");
            OnClickModelInteraction modelInteraction = activeModel != null ? activeModel.GetComponent<OnClickModelInteraction>() : null;
            if (modelInteraction != null)
            {
                modelInteraction.flagSelected = true;
            }
            modelFlagToggleBool = true;
        }
        else
        {
            GameObject activeModel = SceneQueries.OneWithTag("activeModel");
            OnClickModelInteraction modelInteraction = activeModel != null ? activeModel.GetComponent<OnClickModelInteraction>() : null;
            if (modelInteraction != null)
            {
                modelInteraction.flagSelected = false;
            }
            modelFlagToggleBool = false;
        }
    }

    public void ModelDelete()
    {
        GameObject activeModel = SceneQueries.OneWithTag("activeModel");
        if (activeModel == null || lobbyManager == null)
        {
            return;
        }

        PhotonView PV = activeModel.GetComponent<PhotonView>();
        if (PV.IsMine)
        {
            GameObject[] toolTips = SceneQueries.WithTag("OutcropTooltip");
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
