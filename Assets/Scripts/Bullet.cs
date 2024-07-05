using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public short index;
    public Vector2 dir;
    public bool clientWhoShot = false;
    public int addedDamage;
    public float maxdistance = 100f;
    public GameObject bloodBurst;
    float distance = 0f;
    Vector2 oldPos = Vector2.zero;

    private void Start()
    {
        dir = dir.normalized;
        oldPos = transform.position;
    }

    private void FixedUpdate()
    {
        transform.position = (Vector2)transform.position + (dir * GameManager.Instance.weapons[index].bulletSpeed * Time.fixedDeltaTime);
        float disChanged = Vector2.Distance(transform.position, oldPos);
        distance += disChanged;
        if (distance >= maxdistance)
            Destroy(gameObject);
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(oldPos, dir))
        {
            if (Vector2.Distance(hit.point, oldPos) < disChanged && (
                hit.transform.gameObject.layer == 9 ||
                hit.transform.gameObject.layer == 13))
            {
                if (hit.transform.tag == "Enemy")
                {
                    GameObject blood = Instantiate(bloodBurst);
                    blood.transform.position = hit.point;
                    Destroy(blood, 1);
                    if (clientWhoShot)
                    {
                        hit.transform.gameObject.GetComponent<LivingBase>().TakeDamage(GameManager.Instance.weapons[index].Damage + addedDamage);
                        GameManager.Instance.CombatText(GameManager.Instance.weapons[index].Damage + addedDamage, hit.point, false);
                    }                }
                Destroy(gameObject);
                break;
            }
        }
        oldPos = transform.position;
    }
}
