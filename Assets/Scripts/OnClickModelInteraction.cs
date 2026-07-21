using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using TMPro;
using Photon.Pun;

public class OnClickModelInteraction : MonoBehaviour, IMixedRealityPointerHandler
{
    public GameObject selectionIndicator;
    public GameObject flagObject;
    public bool selected;
    public bool flagSelected;

    private GameObject thisSelectionIndicator;

    void Start()
    {

    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
        if (!selected && !flagSelected)
        {

            print(eventData.Pointer.Result.Details.Point);
            print(eventData.Pointer.Result.Details.Normal);
            print(eventData.Pointer.Result.Details.Object.name);


            MakeSelected();
        }
        else if (selected && !flagSelected)
        {
            MakeUnselected();
        }
        else if (selected && flagSelected)
        {
            GazeProvider gazeProvider = Camera.main != null ? Camera.main.GetComponent<GazeProvider>() : null;
            if (gazeProvider == null || gazeProvider.HitInfo.transform == null)
            {
                Debug.LogWarning("Cannot create model flag because no MRTK gaze hit is available.");
                return;
            }

            Vector3 hitPos = gazeProvider.HitPosition;
            Vector3 hitNorm = gazeProvider.HitNormal;
            GameObject hitObject = gazeProvider.HitInfo.transform.gameObject;
            Vector3 hitPosition = hitObject.transform.InverseTransformPoint(hitPos);
            Vector3 hitNormal = hitObject.transform.InverseTransformPoint(hitNorm);
            PhotonView photonView = hitObject.GetComponentInParent<FetchAssetBundle>().gameObject.GetComponent<PhotonView>();
            OnFlagCreate(hitPosition, hitNormal, hitObject.name, photonView.ViewID);
        }
    }

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        //throw new System.NotImplementedException();
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        //throw new System.NotImplementedException();
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        //throw new System.NotImplementedException();
    }

    public  void MakeSelected()
    {
        GameObject currentSelected = SceneQueries.OneWithTag(Tags.ActiveModel);

        if (currentSelected != null)
        {
            currentSelected.GetComponent<OnClickModelInteraction>().MakeUnselected();
        }

        this.gameObject.tag = Tags.ActiveModel;

        Bounds modelBounds = GetChildRendererBounds(this.gameObject);

        thisSelectionIndicator = Instantiate(selectionIndicator);
        thisSelectionIndicator.transform.position = new Vector3(this.transform.position.x, modelBounds.min.y, this.transform.position.z);
        thisSelectionIndicator.transform.parent = this.transform;
        thisSelectionIndicator.transform.rotation = this.transform.rotation;
        thisSelectionIndicator.transform.localScale = Vector3.one;

        selected = true;
    }
    public void MakeUnselected()
    {
        this.gameObject.tag = Tags.AssetBundleLoader;
        Destroy(thisSelectionIndicator);
        selected = false;
    }

    Bounds GetChildRendererBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();

        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1, ni = renderers.Length; i < ni; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }
        else
        {
            return new Bounds();
        }
    }

    private void OnFlagCreate(Vector3 localHitPoint, Vector3 localHitNormal, string hitObject, int PVid)
    {

        GameObject newFlag = Instantiate(flagObject);
        newFlag.transform.parent = PhotonNetwork.GetPhotonView(PVid).gameObject.FindInChildren(hitObject);
        newFlag.transform.localPosition = localHitPoint;
        //newFlag.transform.position = localHitPoint;
        newFlag.transform.localRotation = Quaternion.FromToRotation(transform.up, localHitNormal) * transform.rotation;
    }
}
