using UnityEngine;

namespace DeadCells.Combat
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
        
        private void OnDestroy()
        {
            // 清空静态引用，防止悬空引用
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        /// <summary>
        /// 安全获取CombatManager实例，自动处理空引用
        /// </summary>
        public static CombatManager GetInstance()
        {
            if (Instance == null || Instance.gameObject == null)
            {
                Instance = FindObjectOfType<CombatManager>();
            }
            return Instance;
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