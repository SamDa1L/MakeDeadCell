using UnityEngine;
using System;

namespace DeadCellsTestFramework.Combat
{
    public class Health : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        [SerializeField] private bool isInvulnerable = false;
        
        [Header("Damage Resistance")]
        [SerializeField] private DamageResistance[] resistances;
        
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsAlive => currentHealth > 0;
        public bool IsInvulnerable => isInvulnerable;
        
        public event Action<float, float> OnHealthChanged;
        public event Action<DamageInfo> OnDamageTaken;
        public event Action OnDeath;
        
        private void Awake()
        {
            currentHealth = maxHealth;
        }
        
        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!IsAlive || isInvulnerable) return;
            
            float finalDamage = damageInfo.CalculateFinalDamage();
            finalDamage = ApplyResistances(finalDamage, damageInfo.damageType);
            
            currentHealth = Mathf.Max(0, currentHealth - finalDamage);
            
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnDamageTaken?.Invoke(damageInfo);
            
            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                // Apply knockback and hitstun
                if (damageInfo.knockback != Vector2.zero)
                {
                    CombatManager.Instance?.ApplyKnockback(gameObject, damageInfo.knockback);
                }
                
                if (damageInfo.hitstunDuration > 0)
                {
                    CombatManager.Instance?.ApplyHitstun(gameObject, damageInfo.hitstunDuration);
                }
            }
        }
        
        public void Heal(float amount)
        {
            if (!IsAlive) return;
            
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
        
        public void SetMaxHealth(float newMaxHealth)
        {
            maxHealth = newMaxHealth;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
        
        public void SetInvulnerable(bool invulnerable)
        {
            isInvulnerable = invulnerable;
        }
        
        private float ApplyResistances(float damage, DamageType damageType)
        {
            if (resistances == null) return damage;
            
            foreach (var resistance in resistances)
            {
                if (resistance.damageType == damageType)
                {
                    return damage * (1f - resistance.resistancePercent);
                }
            }
            
            return damage;
        }
        
        private void Die()
        {
            OnDeath?.Invoke();
            
            // Handle death logic based on object type
            if (CompareTag("Player"))
            {
                HandlePlayerDeath();
            }
            else if (CompareTag("Enemy"))
            {
                HandleEnemyDeath();
            }
        }
        
        private void HandlePlayerDeath()
        {
            // Handle player death (respawn, game over, etc.)
            Debug.Log("Player died!");
        }
        
        private void HandleEnemyDeath()
        {
            // Notify room that enemy was defeated using events or messages
            // Room manager can listen for this event instead of direct coupling
            // SendMessage or BroadcastMessage could be used, or a custom event system
            
            // For now, use a simple approach with GameObject.Find
            GameObject roomManager = GameObject.Find("RoomManager");
            if (roomManager != null)
            {
                roomManager.SendMessage("OnEnemyDefeated", gameObject, SendMessageOptions.DontRequireReceiver);
            }
            
            // Destroy enemy after a delay
            Destroy(gameObject, 0.5f);
        }
    }
    
    [System.Serializable]
    public class DamageResistance
    {
        public DamageType damageType;
        [Range(0f, 1f)]
        public float resistancePercent;
    }
}