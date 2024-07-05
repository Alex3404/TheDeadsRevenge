using Assets.Scripts;
using Pathfinding.Examples;
using Photon.Pun;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.UI;
using Pathfinding;

public class EnemyController : LivingBase
{
    public Enemy enemy;
    public int CashDroppedWhenKilled, DamagePerHit;
    public float nextWaypointDistance = 3f;
    bool usingPathfinding = true;
    public AudioClip[] idleSounds;
    public AudioClip[] shortAttackSounds;
    public AudioClip[] longAttackSounds;

    Pathfinding.Path path;
    int currentWaypoint = 0;
    bool reachEndOfPath = false;

    AudioSource audioSource;
    Rigidbody2D rb;
    Seeker seeker;
    GameObject target;
    GameObject healthBarCanvas;
    public Light2D leftEye, rightEye;
    Slider healthBar;

    private void Awake()
    {
        healthBarCanvas = GetComponentInChildren<Canvas>().gameObject;
        healthBar = healthBarCanvas.GetComponentInChildren<Slider>();
        audioSource = GetComponent<AudioSource>();
        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();
        InvokeRepeating("UpdatePath", 0f, .5f);
        InvokeRepeating("UpdatePos", 0f, 5f);
    }

    void UpdatePath()
    {
        if (seeker.IsDone() && target!=null && usingPathfinding)
            seeker.StartPath(transform.position, target.transform.position, OnPathComplete);
    }

    void UpdatePos()
    {
        if (PhotonNetwork.IsMasterClient)
            GetComponent<PhotonView>().RPC("RCP_UpdatePos", RpcTarget.Others,(Vector2)transform.position);
    }

    void OnPathComplete(Pathfinding.Path p)
    {
        if (!p.error)
        {
            path = p;
            currentWaypoint = 0;
        }
    }

    public void Update()
    {
        healthBarCanvas.transform.rotation = Quaternion.identity;
        if(enemy!=null)
            healthBarCanvas.transform.position = transform.position + new Vector3(0, 1.2f + ((enemy.size - 1) * 0.8f));
        else
            healthBarCanvas.transform.position = transform.position + new Vector3(0, 1.2f + (0 * 0.8f));
    }

    bool justDied = false;
    private void FixedUpdate()
    {
        if (!audioSource.isPlaying)
            audioSource.PlayOneShot(idleSounds[Mathf.RoundToInt(Random.value * (idleSounds.Length - 1))], 1f);
        if (GameManager.Instance.GameEnded && PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(GetComponent<PhotonView>());
        if (Health < MaxHealth)
        {
            healthBarCanvas.SetActive(true);
            healthBar.value = (float)Health / MaxHealth;
        }
        if (Health <= 0 && !justDied)
        {
            Destroy(gameObject);
            justDied = true;
            if(enemy!=null)
                if(enemy.explodeOnDeath)
                    GameManager.Instance.CreateExplosion(transform.position,
                        enemy.explosionRadius,
                        (int)(enemy.explosionDamage * GameManager.Instance.getDifficultlyMutli()),
                        "Minion", "Player");
            if (PhotonNetwork.IsMasterClient)
            {
                GameManager.Instance.EnemyKilled(CashDroppedWhenKilled);
                if(enemy != null)
                    if (Random.value <= enemy.chanceToDropCrate)
                        Crate.CreateRandom(rb.position);
            }
        }
        if (target != null && !target.GetComponent<LivingBase>().isDead)
        {
            usingPathfinding = false;
            bool hitPlayer = false;
            foreach (RaycastHit2D hit2D in Physics2D.CircleCastAll(rb.position, (enemy!=null ? enemy.size : 1) * 0.7f,
                ((Vector2)target.transform.position - rb.position).normalized))
            {
                if (!hitPlayer)
                {
                    if (hit2D.transform.gameObject.layer == 8 || hit2D.transform.gameObject.layer == 15)
                        hitPlayer = true;
                    if (!hitPlayer && hit2D.transform.gameObject != null && (hit2D.transform.gameObject.layer == 13))
                        usingPathfinding = true;
                }
            }
            if (usingPathfinding && path != null)
            {
                reachEndOfPath = currentWaypoint >= path.vectorPath.Count;
                if (!reachEndOfPath)
                {
                    Vector2 direction = ((Vector2)path.vectorPath[currentWaypoint] - rb.position).normalized;
                    transform.up = -direction;
                    if(enemy!=null)
                        rb.MovePosition(rb.position + direction * enemy.moveSpeed * Time.fixedDeltaTime);
                    else
                        rb.MovePosition(rb.position + direction * 4 * Time.fixedDeltaTime);
                    float distance = Vector2.Distance(transform.position, path.vectorPath[currentWaypoint]);
                    if (distance < nextWaypointDistance)
                        currentWaypoint++;
                }
            }
            else
            {
                Vector2 direction = ((Vector2)target.transform.position - rb.position).normalized;
                transform.up = -direction;
                if (enemy != null)
                    rb.MovePosition(rb.position + direction * enemy.moveSpeed * Time.fixedDeltaTime);
                else
                    rb.MovePosition(rb.position + direction * 4 * Time.fixedDeltaTime);
            }
        }
        else if (PhotonNetwork.IsMasterClient)
        {
            GameObject closest = null;
            GameObject[] targets = null;
            if (Random.value <= 0.25)
                targets = GameObject.FindGameObjectsWithTag("Minion");
            else
                targets = GameObject.FindGameObjectsWithTag("Player");
            float distance = Mathf.Infinity;
            Vector2 position = rb.position;
            foreach (GameObject go in targets)
            {
                Vector2 diff = (Vector2)go.transform.position - position;
                float curDistance = diff.sqrMagnitude;
                if (curDistance < distance && !go.GetComponent<LivingBase>().isDead)
                {
                    closest = go;
                    distance = curDistance;
                }
            }
            if (closest != null)
            {
                target = closest;
                gameObject.GetPhotonView().RPC("RPC_ChangeTarget", 
                    RpcTarget.Others, target.GetPhotonView().ViewID);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collider)
    {
        if (Time.time - lastAttacked > 1f / this.enemy.hitsPerSecond && 
            collider.GetComponent<LivingBase>() != null && collider.GetComponent<PhotonView>() != null && Health>0)
        {
            if (collider.GetComponent<PhotonView>().IsMine && (collider.tag == "Player" || collider.tag == "Minion"))
            {
                int damage = DamagePerHit + Mathf.RoundToInt(Random.Range(-1, 1));
                Vector2 position = collider.transform.position + (base.transform.position - collider.transform.position) / 2f;
                GameManager.Instance.CombatText(damage, position, false);
                collider.GetComponent<LivingBase>().TakeDamage(damage);
                lastAttacked = Time.time;
            }
        }
    }

    public override void TakeDamage(float damage)
    {
        GetComponent<PhotonView>().RPC("RCP_TakeDamage", RpcTarget.All, damage);
    }

    public override void TakeDamage(float damage, LivingBase hitby)
    {
        if (hitby.tag == "Minion" && hitby.GetComponent<PhotonView>())
            GetComponent<PhotonView>().RPC("RCP_TakeDamageBy", RpcTarget.All, damage, hitby.GetComponent<PhotonView>().ViewID);
    }

    public override void TakeDamage(float damage, Vector2 hitpos)
    {
        this.TakeDamage(damage);
        GameManager.Instance.CombatText(damage, hitpos, false);
    }

    public void ApplySettings(int index, bool boss)
    {
        GetComponent<PhotonView>().RPC("RCP_Apply", RpcTarget.All, (short)index, boss);
    }

    [PunRPC]
    void RCP_Apply(short index, bool boss)
    {
        if (boss) enemy = GameManager.Instance.bosses[index]; else enemy = GameManager.Instance.enemies[index];
        MaxHealth = (int)(enemy.maxHealth * 
            (GameManager.Instance.getDifficultlyMutli() + (PhotonNetwork.CurrentRoom.PlayerCount-1)*.4f+1f));
        CashDroppedWhenKilled = (int)(enemy.cashDroppedWhenKilled * GameManager.Instance.getDifficultlyMutli());
        DamagePerHit = (int)((float)enemy.damagePerHit * GameManager.Instance.getDifficultlyMutli());
        Health = MaxHealth;
        GetComponent<SpriteRenderer>().sprite = enemy.sprites[Mathf.RoundToInt(Random.value*(enemy.sprites.Length-1))];
        transform.localScale = new Vector2(enemy.size, enemy.size);
        healthBarCanvas.transform.localScale = new Vector2(1/ enemy.size, 1/ enemy.size);
        gameObject.transform.Find("Minimap Point").localScale = new Vector2(2.5f / enemy.size, 2.5f / enemy.size);
        leftEye.pointLightInnerRadius *= enemy.size; rightEye.pointLightInnerRadius *= enemy.size;
        leftEye.pointLightOuterRadius *= enemy.size; rightEye.pointLightOuterRadius *= enemy.size;
        leftEye.enabled = enemy.LightEyesLeft; rightEye.enabled = enemy.LightEyesRight;
        leftEye.color = enemy.eyeColor; rightEye.color = enemy.eyeColor;
    }

    [PunRPC]
    void RPC_ChangeTarget(int viewId)
    {
        target = PhotonNetwork.GetPhotonView(viewId).gameObject;
    }

    [PunRPC]
    void RCP_TakeDamageBy(float damage, int viewId)
    {
        Health -= damage;
        PhotonView view = PhotonNetwork.GetPhotonView(viewId);
        if (view.tag == "Minion" && view.GetComponent<MinionController>()!=null)
            target = view.gameObject;
    }

    [PunRPC]
    void RCP_UpdatePos(Vector2 pos)
    {
        float roundTripTime = PhotonNetwork.NetworkingClient.LoadBalancingPeer.LastRoundTripTime/1000f;
        pos += -(Vector2)transform.up * enemy.moveSpeed * roundTripTime;
        if (Vector2.Distance(pos, transform.position)>=1)
            transform.position = pos;
    }

    [PunRPC]
    void RCP_TakeDamage(float damage)
    {
        Health -= damage;
    }
}
