using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RoomListItem : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI roomName;
    RoomInfo info;
    public void SetUp(RoomInfo _info)
    {
        this.info = _info;
        object name;
        info.CustomProperties.TryGetValue("RoomName", out name);
        roomName.text = (string)name;
    }

    public void JoinRoom()
    {
        MainMenu.Instance.JoinRoom(info);
    }
}
