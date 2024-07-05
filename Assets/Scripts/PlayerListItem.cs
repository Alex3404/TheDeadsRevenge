using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerListItem : MonoBehaviourPunCallbacks
{
    [SerializeField] TextMeshProUGUI text;
    [SerializeField] Button kick;
    Player player;

    public void SetUp(Player player)
    {
        this.player = player;
        text.text = player.NickName;
        if (player == PhotonNetwork.LocalPlayer || !PhotonNetwork.IsMasterClient)
            kick.gameObject.SetActive(false);
        if (player.IsMasterClient)
            text.text = player.NickName + " (Host)";
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        text.text = player.NickName;
        if (player == PhotonNetwork.LocalPlayer || !PhotonNetwork.IsMasterClient)
            kick.gameObject.SetActive(false);
        if (player.IsMasterClient)
            text.text = player.NickName + " (Host)";
    }

    public void KickPlayer()
    {
        PhotonNetwork.CloseConnection(player);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if(player==otherPlayer)
        {
            Destroy(gameObject);
        }
    }

    public override void OnLeftRoom()
    {
        Destroy(gameObject);
    }
}
