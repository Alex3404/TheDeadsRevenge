using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    [CreateAssetMenu(menuName = "TDR/New Weapon")]
    public class WeaponData : ScriptableObject
    {
        public int WeaponCost = 100;

        public float FireRate = 0;
        public int Damage = 0;
        public int ClipSize = 10;
        public int ValueMaxAmmo = 50;
        public bool isAutomatic, isSecondary = false;
        public float bulletSpeed = 60f;
        public float lastFired = 0f;
        public bool equipable = true;
        public bool givenByDefault = false;
        public bool canReload = true;
        public GameObject weaponPrefab;
        public AudioClip fireSound;
        public AudioClip reloadSound;
        public short wepIndex = -1;
        public bool CanBeSold = false;
        public Upgradable[] upgrades;

        public WeaponData copyClass()
        {
            WeaponData copy = (WeaponData) CreateInstance("WeaponData");
            foreach (var sourceProperty in typeof(WeaponData).GetProperties())
            {
                var targetProperty = typeof(WeaponData).GetProperty(sourceProperty.Name);
                targetProperty.SetValue(copy, sourceProperty.GetValue(this, null), null);
            }
            foreach (var sourceField in typeof(WeaponData).GetFields())
            {
                var targetField = typeof(WeaponData).GetField(sourceField.Name);
                targetField.SetValue(copy, sourceField.GetValue(this));
            }
            return copy;
        }
    }
}
