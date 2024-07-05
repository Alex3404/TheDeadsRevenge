using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Crate : MonoBehaviour
{
    public CrateType crateType;
    int spawnedOnWave = -1;
   
    public static GameObject CreateCrate(CrateType type, Vector2 position)
    {
        return PhotonNetwork.InstantiateRoomObject(System.IO.Path.Combine("PhotonPrefabs", type.ToString()+"Crate"),
            position, Quaternion.identity);
    }

    public static GameObject CreateRandom(Vector2 position)
    {
        return CreateCrate((CrateType)Mathf.RoundToInt(Random.value*(typeof(CrateType).GetEnumValues().Length-1)), position);
    }

    public void Start()
    {
        spawnedOnWave = GameManager.Instance.Wave;
        InvokeRepeating("CheckIfCanDestroy", 0, 1f);
    }

    public void CheckIfCanDestroy()
    {
        if(PhotonNetwork.IsMasterClient && GameManager.Instance.Wave- spawnedOnWave >= 8)
            PhotonNetwork.Destroy(GetComponent<PhotonView>());
    }

    public void OnTriggerEnter2D(Collider2D collider2D)
    {
        if (collider2D.transform != null && collider2D.tag == "Player" && collider2D.GetComponent<PlayerController>() != null)
        {
            switch (crateType)
            {
                case CrateType.Health:
                    collider2D.GetComponent<PlayerController>().HealPlayer();
                    break;
                case CrateType.Ammo:
                    collider2D.GetComponent<PlayerController>().FillGunAmmo();
                    break;
                default:
                    break;
            }
            GetComponent<PhotonView>().RPC("RPC_DestoryObject", RpcTarget.MasterClient);
        }
    }
    [System.Serializable]
    public enum CrateType
    {
        Health,
        Ammo
    }

    [PunRPC]
    public void RPC_DestoryObject()
    {
        PhotonNetwork.Destroy(GetComponent<PhotonView>());
    }
}
