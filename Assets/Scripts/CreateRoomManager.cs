using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateRoomManager : MonoBehaviour
{
    public TextMesh buttonLabel;
    
    public void CancelCreateRoom()
    {
        buttonLabel.text = "Create New Room";
    }

    public void AddRoomInfo()
    {
        buttonLabel.text = "Cancel";
    }
}
