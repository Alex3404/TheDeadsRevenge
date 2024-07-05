using UnityEngine;

namespace Assets.Scripts
{
    public class LivingBase : MonoBehaviour
    {
        public float MaxHealth = 0;
        public float lastAttacked;
        public float lastHit;
        public bool isDead = false;
        public float Health = 0;

        public virtual void TakeDamage(float Damage)
        {
            lastHit = Time.time;
            Health -= Damage;
            Health = Mathf.Clamp(Health, 0, MaxHealth);
            isDead = Health <= 0;
        }

        public virtual void TakeDamage(float damage, LivingBase hitby)
        {
            TakeDamage(damage);
        }

        public virtual void TakeDamage(float damage, Vector2 pos)
        {
            TakeDamage(damage);
        }
    }
}
