using UnityEngine;
using DeadCells.Combat;

namespace DeadCells.Combat
{
    public abstract class Weapon : MonoBehaviour
    {
        [Header("Weapon Stats")]
        [SerializeField] protected string weaponName = "Basic Weapon";
        [SerializeField] protected WeaponType weaponType = WeaponType.Melee;
        [SerializeField] protected float baseDamage = 10f;
        [SerializeField] protected float attackSpeed = 1f;
        [SerializeField] protected float range = 1f;
        
        [Header("Attack Settings")]
        [SerializeField] protected float attackCooldown = 1f;
        [SerializeField] protected LayerMask targetLayerMask = 1 << 8;
        
        protected float lastAttackTime;
        protected bool canAttack => Time.time >= lastAttackTime + (1f / attackSpeed);
        
        public string WeaponName => weaponName;
        public WeaponType Type => weaponType;
        public float BaseDamage => baseDamage;
        public float AttackSpeed => attackSpeed;
        public float Range => range;
        
        public abstract bool TryAttack(Vector2 attackDirection);
        protected abstract void PerformAttack(Vector2 attackDirection);
        
        protected virtual DamageInfo CreateDamageInfo(Vector2 knockbackDirection)
        {
            var damageInfo = new DamageInfo
            {
                baseDamage = baseDamage,
                damageType = DamageType.Physical,
                knockback = knockbackDirection * 5f,
                hitstunDuration = 0.2f,
                source = gameObject
            };
            
            return damageInfo;
        }
        
        protected void RegisterAttack()
        {
            lastAttackTime = Time.time;
        }
    }
    
    public enum WeaponType
    {
        Melee,
        Ranged,
        Magic,
        Shield
    }
}