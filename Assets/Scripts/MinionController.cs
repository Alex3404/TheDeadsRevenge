using Assets.Scripts;
using Pathfinding.Examples;
using Photon.Pun;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.UI;
using Pathfinding;
using TMPro;

public class MinionController : LivingBase, IPunObservable
{
    public Minion minionData;
    public float nextWaypointDistance = 3f;
    public float respawnTime = 20f;
    bool usingPathfinding = true;
    public TextMeshProUGUI respawnCounter;
    public float addedMoveSpeed;
    public int addedDamage;

    Pathfinding.Path path;
    int currentWaypoint = 0;
    bool reachEndOfPath = false;

    Rigidbody2D rb;
    Seeker seeker;
    GameObject target;
    PhotonView photonView;
    GameObject healthBarCanvas;
    Slider healthBar;
    SpriteRenderer spriteRenderer;

    private void Awake()
    {
        healthBarCanvas = GetComponentInChildren<Canvas>().gameObject;
        healthBar = healthBarCanvas.GetComponentInChildren<Slider>();
        seeker = GetComponent<Seeker>();
        photonView = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        InvokeRepeating("UpdatePath", 0f, .5f);
    }

    void UpdatePath()
    {
        if (seeker.IsDone() && target!=null && usingPathfinding)
            seeker.StartPath(transform.position, target.transform.position, OnPathComplete);
    }

    void OnPathComplete(Pathfinding.Path p)
    {
        if (!p.error)
        {
            path = p;
            currentWaypoint = 0;
        }
    }

    private void FixedUpdate()
    {
        if (photonView.IsMine)
            if (Time.time - lastHit >= respawnTime)
                Health = MaxHealth;

        isDead = Health <= 0;
        respawnCounter.gameObject.SetActive(isDead);
        gameObject.layer = isDead ? 10 : 15;
        rb.constraints = isDead || 
            GameManager.Instance.GameEnded ? RigidbodyConstraints2D.FreezeAll : RigidbodyConstraints2D.FreezeRotation;
        spriteRenderer.sprite = isDead ||
            GameManager.Instance.GameEnded ? minionData.deadSprite : minionData.aliveSprite;
        if (Time.time - lastHit < 20)
            respawnCounter.text = string.Format("{0}:{1:00}", (int)(20-(Time.time - lastHit)) / 60,
                (int)(respawnTime - (Time.time - lastHit)) % 60);
        if (GameManager.Instance.GameEnded&& photonView.IsMine)
        {
            PhotonNetwork.Destroy(GetComponent<PhotonView>());
        }
        if (Health < MaxHealth)
        {
            healthBarCanvas.SetActive(true);
            healthBar.value = (float)Health / MaxHealth;
        }
        else
            healthBarCanvas.SetActive(false);
        if (photonView.IsMine && !isDead)
        {
            if (target != null && !isDead)
            {
                usingPathfinding = false;
                bool hitEnemy = false;
                foreach (RaycastHit2D hit2D in Physics2D.CircleCastAll(rb.position, 0.6f,
                    ((Vector2)target.transform.position - rb.position).normalized))
                {
                    if (!hitEnemy)
                    {
                        if (hit2D.transform.gameObject.layer == 9)
                            hitEnemy = true;
                        if (!hitEnemy && hit2D.transform.gameObject != null && (hit2D.transform.gameObject.layer == 13))
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
                        rb.MovePosition(rb.position + direction * (minionData.moveSpeed + addedMoveSpeed) * Time.fixedDeltaTime);
                        float distance = Vector2.Distance(transform.position, path.vectorPath[currentWaypoint]);
                        if (distance < nextWaypointDistance)
                            currentWaypoint++;
                    }
                }
                else
                {
                    Vector2 direction = ((Vector2)target.transform.position - rb.position).normalized;
                    transform.up = -direction;
                    rb.MovePosition(rb.position + direction * (minionData.moveSpeed + addedMoveSpeed) * Time.fixedDeltaTime);
                }
            }
            else if (!isDead)
            {
                GameObject[] targets = GameObject.FindGameObjectsWithTag("Enemy");
                GameObject closest = null;
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
                }
            }
        }
        healthBarCanvas.transform.rotation = Quaternion.identity;
        healthBarCanvas.transform.position = transform.position + new Vector3(0, 2f);
    }

    private void OnTriggerStay2D(Collider2D collider)
    {
        if (photonView.IsMine && collider.tag == "Enemy" && Time.time - lastAttacked > 1f / minionData.hitsPerSecond &&
            collider.GetComponent<EnemyController>() != null && !isDead)
        {
            int damage = minionData.damagePerHit + addedDamage + Mathf.RoundToInt(Random.Range(-1, 1));
            Vector2 position = collider.transform.position + (base.transform.position - collider.transform.position) / 2f;
            collider.GetComponent<EnemyController>().TakeDamage(damage, position);
            lastAttacked = Time.time;
        }
    }

    bool justDied = false;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            float angle = transform.eulerAngles.z + Mathf.Ceil(-transform.eulerAngles.z / 360) * 360;
            stream.SendNext((byte)(Health/MaxHealth*255f));
            stream.SendNext((short)(angle * 180));
        }
        else
        {
            byte health = (byte)stream.ReceiveNext();
            Health = health / 255f;
            float angle = (short)stream.ReceiveNext();
            transform.eulerAngles = new Vector3(0, 0, angle / 180f);
            isDead = health <= 0;
            if(!isDead) justDied = false;
            if (isDead &&!justDied){
                lastHit = Time.time;
                justDied = true;
            }
        }
    }

    public void ApplySettings(int index, byte respawntime)
    {
        GetComponent<PhotonView>().RPC("RCP_Apply", RpcTarget.All, (short)index, respawntime);
    }

    [PunRPC]
    void RCP_Apply(short index, byte respawntime)
    {
        minionData = GameManager.Instance.minions[index];
        GetComponent<SpriteRenderer>().sprite = minionData.aliveSprite;
        MaxHealth = 1;
        Health = 1;
        respawnTime = respawntime;
    }
}