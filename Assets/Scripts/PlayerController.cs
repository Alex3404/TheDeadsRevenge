using Assets.Scripts;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class PlayerController : LivingBase, IPunObservable
{
    private float Shield = 50;
    public float MaxShield = 50;

    public float moveSpeed = 5f;
    public float respawnTime = 30;
    public float timeSinceHit = 0;
    public int weaponIndex = 0;
    public string[] equippedWeapons;
    public int[] weaponDamage;
    public Dictionary<string, Weapon> weapons = new Dictionary<string, Weapon>();
    public AudioMixerGroup SFXMixer;
    public List<MinionController> minionControllers = new List<MinionController>();
    public bool addedAllMinions = false;
    public bool usingKnife = false;

    Rigidbody2D rb;
    Camera camera;
    PhotonView PV;
    GameObject playerInfoCanvas, playerSprite, playerDead, minimapSprite;
    PlayerUIController playerUI;
    Slider healthInfoSlider;
    Slider shieldInfoSlider;
    GameManager roomManager;
    Vector2 movement, look;

    private void Awake()
    {
        PV = GetComponent<PhotonView>();
        camera = GetComponentInChildren<Camera>();
        playerInfoCanvas = gameObject.transform.Find("PlayerInfo").gameObject;
        healthInfoSlider = playerInfoCanvas.transform.GetChild(0).GetComponent<Slider>();
        shieldInfoSlider = playerInfoCanvas.transform.GetChild(1).GetComponent<Slider>();
        minimapSprite = gameObject.transform.Find("Minimap Arrow").gameObject;
        playerSprite = gameObject.transform.Find("Player Sprite").gameObject;
        playerDead = gameObject.transform.Find("Player Dead").gameObject;
        playerUI = GetComponent<PlayerUIController>();
        rb = GetComponent<Rigidbody2D>();
        roomManager = GameManager.Instance;
    }

    private void Start()
    {
        playerInfoCanvas.SetActive(!PV.IsMine);
        if (!PV.IsMine)
        {
            playerInfoCanvas.GetComponentInChildren<TextMeshProUGUI>().text = PV.Owner.NickName;
            Destroy(camera.gameObject);
            Destroy(minimapSprite);
            Destroy(GetComponentInChildren<EventSystem>().gameObject);
            Destroy(GetComponent<AudioListener>());
            Destroy(GetComponent<PlayerUIController>());
            Destroy(rb);
        }
        else
        {
            Destroy(gameObject.transform.Find("Minimap Point").gameObject);
            moveSpeed = moveSpeed + PlayerPrefs.GetFloat("PlayerMove Speed", 0f);
        }
        ExitGames.Client.Photon.Hashtable properties = PV.Owner.CustomProperties;

        equippedWeapons = new string[]
        {
            (string) properties["0"],
            (string) properties["1"],
            (string) properties["2"]
        };

        weaponDamage = new int[]
        {
            (int) properties["0d"],
            (int) properties["1d"],
            (int) properties["2d"]
        };

        MaxHealth = (int)properties["ph"];
        MaxShield = (int)properties["ps"];

        if (equippedWeapons[0] == "None")
            weaponIndex = 1;
        foreach (WeaponData wep in GameManager.Instance.weapons)
        {
            for (int i = 0; i < equippedWeapons.Length; i++)
                if (equippedWeapons[i] == wep.name)
                {
                    Weapon created = Instantiate(wep.weaponPrefab).GetComponent<Weapon>();
                    created.transform.parent = playerSprite.transform;
                    created.transform.position = playerSprite.transform.position;
                    created.transform.rotation = playerSprite.transform.rotation;
                    created.name = wep.name;
                    created.weaponData = wep.copyClass();
                    if (GetComponent<PhotonView>().IsMine)
                    {
                        created.weaponData.FireRate += PlayerPrefs.GetFloat(wep.name + "FireRate");
                        created.weaponData.ValueMaxAmmo += (int)PlayerPrefs.GetFloat(wep.name + "MaxBullets");
                        created.weaponData.ClipSize += (int)PlayerPrefs.GetFloat(wep.name + "ClipSize");
                        created.ClipAmmo = created.weaponData.ClipSize;
                        created.Ammo = created.weaponData.ValueMaxAmmo;
                    }
                    try
                    {
                        created.firePoint = created.transform.Find("FirePoint");
                    }
                    catch { }
                    weapons[wep.name] = created;
                }
        }
        foreach (KeyValuePair<string, Weapon> wep in weapons)
            wep.Value.gameObject.SetActive(wep.Key == equippedWeapons[weaponIndex]);
        Shield = MaxShield;
        Health = MaxHealth;
    }

    // Loops through all the enemys and check if the player is 20m away,
    // Then checks if the player is overlaping a building.
    private bool CanSpawn(Vector2 point)
    {
        bool canSpawn = true;
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Enemy"))
            if (((Vector2)go.transform.position - point).magnitude <= 20)
                canSpawn = false;
        foreach (Collider2D co in Physics2D.OverlapCircleAll(point, 0.75f))
            if (!co.isTrigger && Physics2D.GetIgnoreLayerCollision(co.gameObject.layer, gameObject.layer))
                canSpawn = false;
        return canSpawn;
    }

    public void Respawn()
    {
        isDead = false;
        HealPlayer();
        // Randomly picks a location on the map and checks if the player can spawn there,
        // if not then it will pick another spot till a spot was found.
        Vector3 position = Vector3.zero;
        do
        {
            position = new Vector3(Random.Range(-45, 45), Random.Range(-45, 45));
        } while (!CanSpawn(position));
        transform.position = position;
    }

    float oldHealth = 0;
    float oldShield = 0;
    void FixedUpdate()
    {
        isDead = Health <= 0;
        // Update player sprite to fit if the player is dead or alive.
        playerSprite.SetActive(!isDead);
        playerDead.SetActive(isDead);
        if (PV.IsMine)
        {
            // Check if all the players died
            AllPlayersDied();

            timeSinceHit = Time.time - lastHit;

            // Updates the others players the players health
            if (oldHealth != Health || oldShield != Shield)
                GetComponent<PhotonView>().RPC("RPC_UpdateHealth", RpcTarget.Others, (short)Health, (short)Shield);
            oldHealth = Health;
            oldShield = Shield;

            // Check if the player can respawn
            if (isDead && !GameManager.Instance.GameEnded && timeSinceHit >= respawnTime)
                Respawn();

            if (!isDead)
            {
                // Move the players position.
                rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);

                // Regens the shield if the player has not been hit in 10 seconds.
                if (timeSinceHit >= 10f && Shield < MaxShield && !isDead)
                    Shield++;
            }
        }
        if (!PV.IsMine)
        {
            // Updates the Player info that is above the players
            healthInfoSlider.gameObject.SetActive(Health < MaxHealth);
            shieldInfoSlider.gameObject.SetActive(Shield < MaxShield);
            healthInfoSlider.value = (float)Health / MaxHealth;
            shieldInfoSlider.value = (float)Shield / MaxShield;
            return;
        }
        // (Un)Freezes the postion of the player if the game has (not) ended or the player is (not) dead.
        rb.constraints = isDead || roomManager.GameEnded ? RigidbodyConstraints2D.FreezeAll : RigidbodyConstraints2D.FreezeRotation;
        gameObject.layer = isDead || roomManager.GameEnded ? 10 : 8;
    }

    Vector3 oldMouse = Vector3.zero;
    void Update()
    {
        // Clamps the health/shield of the player to the max health/shield
        Health = Mathf.Min(Health, MaxHealth);
        Shield = Mathf.Min(Shield, MaxShield);

        if (!roomManager.GameEnded && PV.IsMine)
        {
            if (!playerUI.paused && !isDead)
            {
                // Player Movement
                movement.x = Input.GetAxisRaw("Horizontal"); movement.y = Input.GetAxisRaw("Vertical");
                if(movement.magnitude>=1)
                    movement.Normalize();

                // Player Looking
                Vector2 stickPosition = Vector2.zero;
                stickPosition.x = Input.GetAxisRaw("LHorizontal"); stickPosition.y = Input.GetAxisRaw("LVertical");
                if (stickPosition != Vector2.zero)
                {
                    look = stickPosition;
                }
                if (oldMouse != Input.mousePosition)
                {
                    Vector2 mouseWorldPosition = camera.ScreenToWorldPoint(Input.mousePosition);
                    look = (mouseWorldPosition - (Vector2)transform.position).normalized;
                }
                playerSprite.transform.up = -look;
                minimapSprite.transform.right = look;
                oldMouse = Input.mousePosition;
                // Weapon Input
                if (!usingKnife)
                {
                    // Weapon Switching Guns
                    if (Input.GetKeyDown(KeyCode.Alpha1))
                        WeaponSwitch(0);
                    if (Input.GetKeyDown(KeyCode.Alpha2))
                        WeaponSwitch(1);
                    // Weapon Firing
                    if ((weapons[equippedWeapons[weaponIndex]].weaponData.isAutomatic && Input.GetAxisRaw("Fire") > 0) || (!weapons[equippedWeapons[weaponIndex]].weaponData.isAutomatic && Input.GetAxisRaw("Fire") > 0))
                    {
                        if (weapons[equippedWeapons[weaponIndex]].Use())
                        {
                            Weapon wep = weapons[equippedWeapons[weaponIndex]];
                            PlaySound(wep.weaponData.fireSound);
                        }
                    }
                    // Reload
                    if (Input.GetAxisRaw("Reload") > 0)
                    {
                        weapons[equippedWeapons[weaponIndex]].Reload();
                    }
                    // Use Knife
                    if (Input.GetAxisRaw("Knife") > 0 && Time.time - weapons[equippedWeapons[2]].weaponData.lastFired > 1f /
                        weapons[equippedWeapons[2]].weaponData.FireRate)
                    {
                        base.StartCoroutine(CoUseKnife());
                        PV.RPC("RPC_UseKnife", RpcTarget.Others);
                    }
                }
            }
        }
    }

    public IEnumerator CoUseKnife()
    {
        // Checks if the player is already using the knife
        if (!usingKnife)
        {
            usingKnife = true;
            // Loops through all the weapons and sets them inactive,
            //and stops all reloading to prevent infinte reloading.
            foreach (KeyValuePair<string, Weapon> wep in weapons)
            {
                wep.Value.gameObject.SetActive(wep.Key == equippedWeapons[2]);
                wep.Value.isreloading = false;
            }
            // Plays the knife animation.
            weapons[equippedWeapons[2]].GetComponent<Animator>().SetTrigger("useKnife");
            // Does damage to the enemys.
            if (PV.IsMine)
            {
                EnemyController closest = null;
                float distance = 0;
                foreach (Collider2D co in Physics2D.OverlapCircleAll((Vector2)playerSprite.transform.position -
                    (Vector2)playerSprite.transform.up*0.8f, 1.2f))
                {
                    if (!co.isTrigger && co.gameObject.GetComponent<EnemyController>() != null)
                    {
                        float curDistance = Vector2.Distance(rb.position, co.gameObject.transform.position);
                        if (closest==null||(curDistance < distance && !closest.isDead))
                        {
                            closest = co.gameObject.GetComponent<EnemyController>();
                            distance = curDistance;
                        }
                    }
                }
                if (closest != null)
                {
                    int Damage = (int)(weapons[equippedWeapons[2]].weaponData.Damage * Random.Range(0.7f,1.2f));
                    GameManager.Instance.CombatText(
                        Damage,
                        closest.gameObject.transform.position + (-transform.up * 0.77f * closest.enemy.size),
                        false
                    );
                    closest.TakeDamage(Damage);
                    PlaySound(weapons[equippedWeapons[2]].weaponData.fireSound);
                }
                weapons[equippedWeapons[weaponIndex]].weaponData.lastFired = Time.time;
            }
            yield return new WaitForSeconds(0.6f);
            // Loops through all the and sets the current equipped weapon active and all others inactive.
            foreach (KeyValuePair<string, Weapon> wep in weapons)
                wep.Value.gameObject.SetActive(wep.Key == equippedWeapons[weaponIndex]);
            usingKnife = false;
        }
        yield break;
    }

    public void WeaponSwitch(short index)
    {
        if(equippedWeapons[index]!= "None")
        {
            short count = 0;
            weaponIndex = index;
            foreach (KeyValuePair<string, Weapon> wep in weapons)
            {
                wep.Value.gameObject.SetActive(wep.Key == equippedWeapons[weaponIndex]);
                wep.Value.isreloading = false;
                if (wep.Key == equippedWeapons[weaponIndex])
                {
                    PhotonNetwork.CleanRpcBufferIfMine(PV);
                    PV.RPC("RPC_SwitchGun", RpcTarget.OthersBuffered, count);
                }
            }
        }
    }

    public float GetHealth() { return this.Health; }

    public float GetShield() { return this.Shield; }

    public override void TakeDamage(float Damage)
    {
        if (!isDead)
        {
            lastHit = Time.time;
            if (Shield > 0){Shield -= Damage;if (Shield < 0) { Health += Shield; Shield = 0; }}
            else if (Shield <= 0){Health -= Damage;Shield = 0;}
            if (Health <= 0){
                Health = 0;
                isDead = true;
            }
            if (isDead)
                weapons[equippedWeapons[weaponIndex]].isreloading = false;
        }
    }

    // Called when the game is ended on the client's player.
    public void GameEnded()
    {
        // Gives the player the cash.
        int newCash = PlayerPrefs.GetInt("Cash") + GameManager.Instance.CashGained;
        PlayerPrefs.SetInt("Cash", newCash);
        PlayerPrefs.Save();
        Destroy(GetComponent<PhotonView>());
        Destroy(GetComponent<Photon2DTransformView>());
        PhotonNetwork.LeaveRoom();
    }

    // Checks if all the players and minions are not dead.
    public void AllPlayersDied()
    {
        if (!roomManager.GameEnded)
        {
            bool allPlayersDied = true;
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player"))
                if (!go.GetComponent<LivingBase>().isDead)
                    allPlayersDied = false;
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("Minion"))
                if (!go.GetComponent<LivingBase>().isDead)
                    allPlayersDied = false;
            if (allPlayersDied)
            {
                roomManager.GameEnded = true;
                playerUI.GameEnded();
                GameEnded();
            }
        }
    }
    
    // Heals the player to the max health.
    public void HealPlayer()
    {
        Health = MaxHealth;
        Shield = MaxShield;
        GetComponent<PhotonView>().RPC("RPC_UpdateHealth", RpcTarget.Others, (short)Health, (short)Shield);
    }

    public void FillGunAmmo()
    {
        try { weapons[equippedWeapons[1]].FillAmmo(); } catch { }
        try { weapons[equippedWeapons[0]].FillAmmo(); } catch { }
    }

    public static float Clamp0360(float eulerAngles)
    {
        float result = eulerAngles - Mathf.CeilToInt(eulerAngles / 360f) * 360f;
        if (result < 0)
        {
            result += 360f;
        }
        return result;
    }

    public void PlaySound(AudioClip clip)
    {
        GameObject soundObject = new GameObject();
        soundObject.transform.position = transform.position + transform.up*-2;
        AudioSource audio = soundObject.AddComponent<AudioSource>();
        audio.spatialBlend = 1;
        audio.outputAudioMixerGroup = SFXMixer;
        audio.PlayOneShot(clip);
        if(soundObject)
            Destroy(soundObject, clip.length);
    }

    public GameObject bulletPrefab;
    public GameObject musselBlastPrefab;

    public void CreateBullet(bool clientWhoShot, Weapon weaponFired)
    {
        Bullet bullet = Instantiate(bulletPrefab).GetComponent<Bullet>();
        bullet.gameObject.transform.position = weaponFired.firePoint.position;
        bullet.dir = -weaponFired.firePoint.up;
        bullet.clientWhoShot = clientWhoShot;
        if (clientWhoShot)
            GetComponent<PhotonView>().RPC("RPC_FireBullet", RpcTarget.Others);
        bullet.addedDamage = weaponDamage[weaponIndex];
        bullet.index = weapons[equippedWeapons[weaponIndex]].weaponData.wepIndex;
        GameObject go = Instantiate(musselBlastPrefab);
        go.transform.position = weaponFired.firePoint.position;
        go.transform.up = weaponFired.firePoint.up;
        Destroy(go, 0.3f);
        if (!clientWhoShot)
            PlaySound(GameManager.Instance.weapons[weapons[equippedWeapons[weaponIndex]].weaponData.wepIndex].fireSound);
    }

    [PunRPC]
    public void RPC_FireBullet()
    {
        try {
            CreateBullet(false, weapons[equippedWeapons[weaponIndex]]);
        }
        catch { }
    }

    [PunRPC]
    public void RPC_UpdateHealth(short heath, short shield)
    {
        this.Health = heath;
        this.Shield = shield;
        this.isDead = heath <= 0;
    }

    [PunRPC]
    public void RPC_UseKnife()
    {
        try
        {
            base.StartCoroutine(CoUseKnife());
        }
        catch { }
    }

    [PunRPC]
    public void RPC_SwitchGun(short gunIndex)
    {
        short index = 0;
        weaponIndex = gunIndex;
        if (!PV.IsMine)
            foreach (KeyValuePair<string, Weapon> wep in weapons)
            {
                wep.Value.gameObject.SetActive(gunIndex==index);
                index++;
            }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            float angle = playerSprite.transform.eulerAngles.z;
            stream.SendNext((short)((angle + Mathf.Ceil(-angle / 360) * 360)*180f));
        }
        else
        {
            float angle = (short)stream.ReceiveNext();
            playerSprite.transform.eulerAngles = new Vector3(0, 0, angle/180f);
        }
    }
}
