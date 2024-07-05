using Assets.Scripts;
using UnityEngine;

[CreateAssetMenu(menuName = "TDR/New Minion")]
public class Minion : ScriptableObject
{
    public float moveSpeed, hitsPerSecond;
    public int damagePerHit, maxHealth;
    public int cost = 10000;
    public bool givenByDefault = false;
    public Sprite aliveSprite, deadSprite;
    public bool CanBeSold = false;
    public Upgradable[] upgrades;
}