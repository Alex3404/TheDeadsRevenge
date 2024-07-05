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
    public class Weapon : MonoBehaviour
    {
        public WeaponData weaponData;
        public bool canReload = true;
        public bool isreloading = false;
        public int Ammo, ClipAmmo = 0;
        public Transform firePoint;

        PlayerController playerController;

        public void Awake()
        {
            playerController = GetComponentInParent<PlayerController>();
            Ammo = weaponData.ValueMaxAmmo;
            ClipAmmo = weaponData.ClipSize;
        }

        public virtual bool Use()
        {
            bool gunWasShot = false;
            if (Time.time - weaponData.lastFired > 1f / weaponData.FireRate && ClipAmmo > 0 && !isreloading)
            {
                ClipAmmo--;
                weaponData.lastFired = Time.time;
                playerController.CreateBullet(true, this);
                gunWasShot = true;
            }
            if (Ammo > 0 && ClipAmmo <= 0)
                Reload();
            return gunWasShot;
        }

        public void Reload()
        {
            if (Ammo <= 0 || !canReload)
                return;
            if (!isreloading)
                base.StartCoroutine(CoReload());
        }

        private IEnumerator CoReload()
        {
            if (!canReload)
                yield break;
            playerController.PlaySound(weaponData.reloadSound);
            if (ClipAmmo <= 0 && Ammo <= 0)
            {
                ClipAmmo = 0;
                Ammo = 0;
            }
            else
            {
                isreloading = true;
                yield return new WaitForSeconds(2f);
                Ammo -= weaponData.ClipSize - ClipAmmo;
                ClipAmmo = weaponData.ClipSize;
                if (Ammo < 0)
                {
                    ClipAmmo += Ammo;
                    Ammo = 0;
                }
                isreloading = false;
            }
            yield break;
        }
        public void FillAmmo() { Ammo = weaponData.ValueMaxAmmo; ClipAmmo = weaponData.ClipSize; }
        public int getMaxAmmo() { return Ammo; }
        public int getAmmo() { return ClipAmmo; }

        public bool isReloading() { return this.isreloading; }
    }
}
