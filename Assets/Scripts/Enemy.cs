using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    [CreateAssetMenu(menuName = "TDR/New Enemy")]
    public class Enemy : ScriptableObject
    {
        public int spawnsAfterWave = 0;
        public float moveSpeed, hitsPerSecond, chanceToDropCrate, size, spawnChance;
        public int maxHealth, damagePerHit;
        public int cashDroppedWhenKilled;
        public Sprite[] sprites;
        public bool LightEyesLeft;
        public bool LightEyesRight;
        public Color eyeColor;
        public bool explodeOnDeath = false;
        public float explosionRadius = 0f;
        public int explosionDamage = 0;
    }
}
