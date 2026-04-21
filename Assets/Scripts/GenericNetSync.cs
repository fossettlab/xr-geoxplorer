using System.Collections;
using System.Collections.Generic;
//using Microsoft.MixedReality.Toolkit.Input;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.EventSystems;

public class GenericNetSync : MonoBehaviourPun, IPunObservable
{


    public bool User;

    public Vector3 startingLocalPosition;
    public Quaternion startingLocalRotation;
    public Vector3 startingScale;

    private Quaternion networkLocalRotation;
    private Vector3 networkLocalPosition;
    private Vector3 networkLocalScale;

    private Camera mainCamera;

    private PhotonView PV;
    
    void Start()
    {
        PV = GetComponent<PhotonView>();
        mainCamera = Camera.main;
        
        if (!PV.IsMine)
        {
            transform.parent = FindObjectOfType<TableAnchor>().transform;
        }
        else if (PV.IsMine  && User)
        {
            transform.parent = FindObjectOfType<TableAnchor>().transform;
            GenericNetworkManager.instance.localUser = PV;
        }
       

        startingLocalPosition = transform.localPosition;
        startingLocalRotation = transform.localRotation;
        startingScale = transform.localScale;
        networkLocalPosition = startingLocalPosition;
        networkLocalRotation = startingLocalRotation;
        networkLocalScale = startingScale;

    }

    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            if (PV.IsMine && User)
            {
                //The user is the camera view point, so we need to Photon User to move with the Camera
                //Moreso since the Camera's movement is what moves the user, getting the users localposition does nothing. 
                //it is the cameras localposition we need.

                //stream.SendNext(mainCamera.transform.localPosition - TableAnchor.instance.transform.position);
                //stream.SendNext(mainCamera.transform.localRotation * Quaternion.Inverse(TableAnchor.instance.transform.localRotation));
                //stream.SendNext(mainCamera.transform.localPosition);
                //stream.SendNext(mainCamera.transform.localRotation);
                stream.SendNext(TableAnchor.instance.transform.InverseTransformPoint(mainCamera.transform.localPosition));
                stream.SendNext(Quaternion.Inverse(TableAnchor.instance.transform.rotation) * mainCamera.transform.rotation);
                stream.SendNext(transform.localScale);
            }
            else
            {
                //Otherwise Objects can just deal with their own localposition.
                stream.SendNext(transform.localPosition);
                stream.SendNext(transform.localRotation);
                stream.SendNext(transform.localScale);
            }

        }
        else
        {
            networkLocalPosition = (Vector3)stream.ReceiveNext();
            networkLocalRotation = (Quaternion)stream.ReceiveNext();
            networkLocalScale = (Vector3)stream.ReceiveNext();

        }
    }

    void FixedUpdate()
    {
        

        if (!PV.IsMine)
        {
            transform.localPosition = networkLocalPosition;
            transform.localRotation = networkLocalRotation;
            transform.localScale = networkLocalScale;
        }

        if (PV.IsMine && User)
        {
            transform.position = Camera.main.transform.localPosition;
            transform.rotation = Camera.main.transform.localRotation;
        }
    }


}
