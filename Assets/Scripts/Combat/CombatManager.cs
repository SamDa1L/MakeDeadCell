using UnityEngine;

namespace DeadCellsTestFramework.Combat
{
    public class CombatManager : MonoBehaviour
    {
        [Header("Combat Settings")]
        [SerializeField] private LayerMask enemyLayerMask = 1 << 8;
        [SerializeField] private LayerMask playerLayerMask = 1 << 9;
        
        public static CombatManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        public void DealDamage(GameObject attacker, GameObject target, DamageInfo damageInfo)
        {
            Health targetHealth = target.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damageInfo);
            }
        }
        
        public void ApplyKnockback(GameObject target, Vector2 force)
        {
            Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
            if (targetRb != null)
            {
                targetRb.AddForce(force, ForceMode2D.Impulse);
            }
        }
        
        public void ApplyHitstun(GameObject target, float duration)
        {
            HitstunController hitstun = target.GetComponent<HitstunController>();
            if (hitstun != null)
            {
                hitstun.ApplyHitstun(duration);
            }
        }
    }
}