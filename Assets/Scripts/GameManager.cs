using Assets.Scripts;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEditor.PlayerSettings;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;
    public GameObject ExplosionEffectPrefab;
    public GameObject CombatTextPrefab;
    public bool InGame = false;
    public bool GameEnded = false;
    public int difficultly = 0;

    private void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        PhotonView photonView = gameObject.AddComponent<PhotonView>();
        photonView.ViewID = 999;
        photonView.FindObservables(true);
        Instance = this;
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        for (short i = 0; i < weapons.Length; i++)
            weapons[i].wepIndex = i;

        foreach (WeaponData weapon in weapons)
            foreach (Upgradable upgrade in weapon.upgrades)
                if (PlayerPrefs.GetFloat(weapon.name + upgrade.name, upgrade.defaultValue) > upgrade.maxValue)
                    PlayerPrefs.SetFloat(weapon.name + upgrade.name, upgrade.maxValue);

        foreach (WeaponData minion in weapons)
            foreach (Upgradable upgrade in minion.upgrades)
                if (PlayerPrefs.GetFloat(minion.name + upgrade.name, upgrade.defaultValue) > upgrade.maxValue)
                    PlayerPrefs.SetFloat(minion.name + upgrade.name, upgrade.maxValue);
    }

    public override void OnLeftRoom()
    {
        if (InGame && !GameEnded)
        {
            MusicManager.Instance.StopSonic();
            SceneManager.LoadScene("Main Menu");
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        InGame = false;
    }

    private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        Wave = 0;
        GameEnded = false;
        WaveOngoing = false;
        EnemysDowned = 0;
        CashGained = 0;
        InGame = scene.buildIndex == 1;
        if (InGame)
        {
            PlayerController playerController = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Player"),
                  new Vector3(PhotonNetwork.LocalPlayer.ActorNumber * 3 - 6, 0),
                  Quaternion.identity).GetComponent<PlayerController>();
            List<string> spawnedMinions = new List<string>();
            for (int i = 0; i < PlayerPrefs.GetFloat("Minion Slots", 1f); i++)
            {
                string equippedMinion = PlayerPrefs.GetString("m" + i, "None");
                for (int I = 0; I < minions.Length; I++)
                {
                    if (minions[I].name == equippedMinion &&
                        !spawnedMinions.Contains(minions[I].name))
                    {
                        spawnedMinions.Add(minions[I].name);
                        MinionController minion = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Minion"),
                            new Vector3(PhotonNetwork.LocalPlayer.ActorNumber * 3 - 6, 3 * (i+1)),
                            Quaternion.identity).GetComponent<MinionController>();
                        minion.addedMoveSpeed = PlayerPrefs.GetFloat(minions[I].name + "MoveSpeed");
                        minion.addedDamage = (int)PlayerPrefs.GetFloat(minions[I].name + "Damage");
                        minion.ApplySettings(I, (byte)PlayerPrefs.GetFloat(minions[I].name + "RespawnTime"));
                        playerController.minionControllers.Add(minion);
                    }
                }
            }
            playerController.addedAllMinions = true;
            string mapName = (string)PhotonNetwork.CurrentRoom.CustomProperties["map"];
            difficultly = (int)PhotonNetwork.CurrentRoom.CustomProperties["dif"];
            foreach (Transform map in GameObject.Find("Maps").GetComponentInChildren<Transform>(true))
                map.gameObject.SetActive(map.gameObject.name == mapName);
            GameObject.Find("Pathfinding").GetComponent<AstarPath>().graphs[0].Scan();
        }
    }

    //Wave Manager

    public short Wave = 0;
    public bool WaveOngoing = false;
    public int EnemysDowned = 0;
    public int CashGained = 0;
    public int BossModWave = 5;

    public List<Enemy> enemies;
    public List<Enemy> bosses;

    private bool CanSpawn(Vector2 point, float size)
    {
        bool canSpawn = true;
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (((Vector2)go.transform.position - point).magnitude <= 20)
                canSpawn = false;
        }
        foreach(Collider2D co in Physics2D.OverlapCircleAll(point, size * 0.55f))
        {
            if (!co.isTrigger && Physics2D.GetIgnoreLayerCollision(co.gameObject.layer, gameObject.layer))
                canSpawn = false;
        }
        return canSpawn;
    }


    public void QuitToMainMenu()
    {
        PhotonNetwork.DestroyPlayerObjects(PhotonNetwork.LocalPlayer);
        PhotonNetwork.LeaveRoom();
    }

    public void EnemyKilled(int cashGained)
    {
        EnemysDowned++;
        CashGained += cashGained;
        photonView.RPC("RPC_UpdateCashGained", RpcTarget.Others, cashGained);
    }

    public float getDifficultlyMutli()
    {
        float multi = 1f;
        switch (difficultly)
        {
            case 0:
                multi = 1;
                break;
            case 1:
                multi = 1.5f;
                break;
            case 2:
                multi = 2;
                break;
        }
        multi *= Mathf.Pow(1.013f, Instance.Wave);
        return multi;
    }

    private float timeTillNextWave = 0.0f;
    private bool waitingForNextWave = false;
    private void FixedUpdate()
    {
        if (PhotonNetwork.IsMasterClient && InGame && !GameEnded)
        {
            if (!WaveOngoing) StartNewWave();
            else if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
            {
                if (waitingForNextWave)
                {
                    timeTillNextWave -= Time.fixedDeltaTime;
                    if (timeTillNextWave <= 0.0f)
                    {
                        timeTillNextWave = 0.0f;
                        WaveOngoing = false;
                        waitingForNextWave = false;
                        photonView.RPC("RPC_UpdateWaveStarted", RpcTarget.Others, WaveOngoing);
                    }
                }
                else
                {
                    timeTillNextWave = 10.0f;
                    waitingForNextWave = true;
                }
            }
        }
    }

    private void StartNewWave()
    {
        Wave++;
        WaveOngoing = true;
        photonView.RPC("RPC_UpdateWave", RpcTarget.Others, Wave);
        photonView.RPC("RPC_UpdateWaveStarted", RpcTarget.Others, WaveOngoing);
        for (int e = 0; e < Mathf.Min(Wave + 4, 50) - (Wave % BossModWave == 0 ? 1 : 0); e++)
        {
            float poolSize = 0;
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i].spawnsAfterWave <= Wave)
                    poolSize += enemies[i].spawnChance;
            float randomNumber = Random.Range(0, poolSize) + 1; float accumulatedProbability = 0;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].spawnsAfterWave > Wave)
                    continue;
                accumulatedProbability += enemies[i].spawnChance;
                if (randomNumber <= accumulatedProbability)
                {
                    Vector3 position = Vector3.zero;
                    do
                    {
                        position = new Vector3(Random.Range(-45, 45), Random.Range(-45, 45));
                    } while (!CanSpawn(position, enemies[i].size));
                    GameObject go = PhotonNetwork.InstantiateRoomObject(Path.Combine("PhotonPrefabs", "Enemy"), position, Quaternion.identity);
                    go.GetComponent<EnemyController>().ApplySettings(i, false);
                    if (enemies[i].sprites[0].name.Contains("Sonic"))
                    {
                        photonView.RPC("RPC_SonicTime", RpcTarget.All);
                    }
                    break;
                }
            }
        }
        if (Wave % BossModWave == 0)
        {
            if (bosses.Count > 1)
            {
                float poolSize = 0;
                for (int i = 0; i < bosses.Count; i++) { poolSize += bosses[i].spawnChance; }
                float randomNumber = Random.Range(0, poolSize) + 1; float accumulatedProbability = 0;

                for (int i = 0; i < bosses.Count; i++)
                {
                    accumulatedProbability += bosses[i].spawnChance;
                    if (randomNumber <= accumulatedProbability)
                    {
                        Vector3 position = Vector3.zero;
                        do
                        {
                            position = new Vector3(Random.Range(-45, 45), Random.Range(-45, 45));
                        } while (!CanSpawn(position, bosses[i].size));

                        GameObject go = PhotonNetwork.InstantiateRoomObject(Path.Combine("PhotonPrefabs", "Enemy"), position, Quaternion.identity);
                        go.GetComponent<EnemyController>().ApplySettings(i, true);
                        break;
                    }
                }
            }
            else
            {
                Vector3 position = Vector3.zero;
                do
                {
                    position = new Vector3(Random.Range(-45, 45), Random.Range(-45, 45));
                } while (!CanSpawn(position, bosses[0].size));

                GameObject go = PhotonNetwork.InstantiateRoomObject(Path.Combine("PhotonPrefabs", "Enemy"), position, Quaternion.identity);
                go.GetComponent<EnemyController>().ApplySettings(0, true);
            }
        }
    }

    public void CreateExplosion(Vector2 point, float radius, int damage, params string[] damageTargets)
    {
        GameObject effect = Instantiate(ExplosionEffectPrefab);
        effect.transform.position = point;
        Destroy(effect, 1f);
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(point, radius))
            if (collider.gameObject != null && collider.GetComponent<LivingBase>() != null &&
                collider.GetComponent<PhotonView>() != null)
                for (int i = 0; i < damageTargets.Length; i++)
                    if (collider.gameObject.CompareTag(damageTargets[i]) && collider.GetComponent<PhotonView>().IsMine)
                        collider.GetComponent<LivingBase>().TakeDamage(damage);
    }

    private void CombatText(float dmg, Vector2 pos)
    {
        GameObject go = GameObject.Instantiate(CombatTextPrefab);
        go.transform.position = new Vector2(pos.x - 13, pos.y);
        go.transform.Find("Canvas").GetComponentInChildren<TextMeshProUGUI>().text = "-" + dmg.ToString("0");
        Destroy(go, 1);
    }

    public void CombatText(float dmg, Vector2 pos, bool clientOnly)
    {
        if(clientOnly) CombatText(dmg, pos);
        else GetComponent<PhotonView>().RPC("RPC_CombatText", RpcTarget.All, dmg, (short)(pos.x * 655), (short)(pos.y * 655));
    }


    [PunRPC]
    public void RPC_UpdateCashGained(int value)
    {
        CashGained += value;
        PlayerPrefs.SetInt("Cash", PlayerPrefs.GetInt("Cash") + value);
        PlayerPrefs.Save();
    }

    [PunRPC]
    public void RPC_UpdateWave(short Wave)
    {
        this.Wave = Wave;
    }
    [PunRPC]
    public void RPC_UpdateWaveStarted(bool WaveStarted)
    {
        this.WaveOngoing = WaveStarted;
    }

    [PunRPC]
    public void RPC_SonicTime()
    {
        MusicManager.Instance.PlaySonicSong();
    }

    [PunRPC]
    public void RPC_CombatText(float dmg, short x, short y)
    {
        CombatText(dmg, new Vector2((x / 655) - 13, (y / 655)));
    }

    //Shop 

    public Minion[] minions;
    public WeaponData[] weapons;
}
