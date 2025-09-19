using UnityEngine;
using DeadCellsTestFramework.Combat;

namespace DeadCellsTestFramework.Weapons
{
    public class MeleeWeapon : Weapon
    {
        [Header("Melee Settings")]
        [SerializeField] private Transform attackPoint;
        [SerializeField] private float attackRadius = 1f;
        [SerializeField] private float comboWindow = 0.5f;
        [SerializeField] private int maxComboCount = 3;
        
        private int currentComboCount = 0;
        private float lastComboTime = 0f;
        
        private void Awake()
        {
            if (attackPoint == null)
                attackPoint = transform;
        }
        
        public override bool TryAttack(Vector2 attackDirection)
        {
            if (!canAttack) return false;
            
            PerformAttack(attackDirection);
            return true;
        }
        
        protected override void PerformAttack(Vector2 attackDirection)
        {
            RegisterAttack();
            UpdateCombo();
            
            // Detect targets in attack range
            Collider2D[] hitTargets = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, targetLayerMask);
            
            foreach (var target in hitTargets)
            {
                if (target.gameObject == gameObject) continue;
                
                Vector2 knockbackDirection = (target.transform.position - transform.position).normalized;
                DamageInfo damageInfo = CreateDamageInfo(knockbackDirection);
                
                // Apply combo multiplier
                damageInfo.baseDamage *= GetComboMultiplier();
                
                CombatManager.Instance?.DealDamage(gameObject, target.gameObject, damageInfo);
            }
            
            // Play attack animation/effects
            PlayAttackEffects();
        }
        
        private void UpdateCombo()
        {
            if (Time.time - lastComboTime > comboWindow)
            {
                currentComboCount = 0;
            }
            
            currentComboCount = Mathf.Min(currentComboCount + 1, maxComboCount);
            lastComboTime = Time.time;
        }
        
        private float GetComboMultiplier()
        {
            return 1f + (currentComboCount - 1) * 0.2f; // +20% damage per combo hit
        }
        
        private void PlayAttackEffects()
        {
            // Play swing sound
            // Spawn attack particles
            // Trigger screen shake
        }
        
        private void OnDrawGizmosSelected()
        {
            if (attackPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
            }
        }
    }
}