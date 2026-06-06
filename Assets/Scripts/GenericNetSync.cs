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
        
        TableAnchor tableAnchor = FindObjectOfType<TableAnchor>();
        if (tableAnchor != null)
        {
            if (!PV.IsMine)
            {
                transform.parent = tableAnchor.transform;
            }
            else if (PV.IsMine && User)
            {
                transform.parent = tableAnchor.transform;
                GenericNetworkManager.instance.localUser = PV;
            }
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
                if (TableAnchor.instance == null || mainCamera == null)
                {
                    stream.SendNext(transform.localPosition);
                    stream.SendNext(transform.localRotation);
                    stream.SendNext(transform.localScale);
                }
                else
                {
                    stream.SendNext(TableAnchor.instance.transform.InverseTransformPoint(mainCamera.transform.position));
                    stream.SendNext(Quaternion.Inverse(TableAnchor.instance.transform.rotation) * mainCamera.transform.rotation);
                    stream.SendNext(transform.localScale);
                }
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
            Camera activeCamera = mainCamera != null ? mainCamera : Camera.main;
            if (activeCamera == null)
            {
                return;
            }

            if (TableAnchor.instance != null)
            {
                transform.localPosition = TableAnchor.instance.transform.InverseTransformPoint(activeCamera.transform.position);
                transform.localRotation = Quaternion.Inverse(TableAnchor.instance.transform.rotation) * activeCamera.transform.rotation;
            }
            else
            {
                transform.position = activeCamera.transform.position;
                transform.rotation = activeCamera.transform.rotation;
            }
        }
    }


}
