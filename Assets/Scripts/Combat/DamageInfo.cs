using UnityEngine;

namespace DeadCellsTestFramework.Combat
{
    [System.Serializable]
    public class DamageInfo
    {
        [Header("Damage")]
        public float baseDamage = 10f;
        public DamageType damageType = DamageType.Physical;
        public bool canCrit = true;
        public float critChance = 0.05f;
        public float critMultiplier = 2f;
        
        [Header("Effects")]
        public Vector2 knockback = Vector2.zero;
        public float hitstunDuration = 0.1f;
        public StatusEffect[] statusEffects;
        
        [Header("Source")]
        public GameObject source;
        public Transform hitPoint;
        
        public float CalculateFinalDamage()
        {
            float finalDamage = baseDamage;
            
            // Apply crit
            if (canCrit && Random.value <= critChance)
            {
                finalDamage *= critMultiplier;
            }
            
            return finalDamage;
        }
    }
    
    public enum DamageType
    {
        Physical,
        Magic,
        Fire,
        Ice,
        Poison,
        Electric
    }
    
    [System.Serializable]
    public class StatusEffect
    {
        public StatusEffectType type;
        public float duration;
        public float intensity;
    }
    
    public enum StatusEffectType
    {
        Poison,
        Burn,
        Freeze,
        Stun,
        Slow,
        Bleed
    }
}