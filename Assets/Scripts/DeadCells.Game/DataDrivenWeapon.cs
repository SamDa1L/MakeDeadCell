using UnityEngine;
using DeadCells.Combat;
using DeadCells.Data;

namespace DeadCells.Game
{
    /// <summary>
    /// Data component that provides weapon statistics from CastleDB
    /// This component should be attached alongside a Weapon component
    /// </summary>
    public class DataDrivenWeapon : MonoBehaviour
    {
        [Header("Data-Driven Properties")]
        [SerializeField] private WeaponData weaponData;
        
        public WeaponData WeaponData => weaponData;
        
        public void Initialize(WeaponData data)
        {
            weaponData = data;
        }
        
        /// <summary>
        /// Get weapon statistics for use by Weapon components
        /// </summary>
        public RuntimeWeaponStats GetWeaponStats()
        {
            if (weaponData == null)
                return new RuntimeWeaponStats();
                
            return new RuntimeWeaponStats
            {
                name = weaponData.name,
                baseDamage = weaponData.baseDamage,
                attackSpeed = weaponData.attackSpeed,
                range = weaponData.range,
                critChance = weaponData.critChance,
                critMultiplier = weaponData.critMultiplier
            };
        }
        
        /// <summary>
        /// Create damage info based on weapon data
        /// </summary>
        public DamageInfo CreateDataDrivenDamageInfo(Vector2 knockbackDirection)
        {
            if (weaponData == null)
                return new DamageInfo { baseDamage = 10f, source = gameObject };
                
            var damageInfo = new DamageInfo
            {
                baseDamage = weaponData.baseDamage,
                canCrit = true,
                critChance = weaponData.critChance,
                critMultiplier = weaponData.critMultiplier,
                knockback = knockbackDirection * (weaponData.stats?.knockback ?? 5f),
                hitstunDuration = weaponData.stats?.hitstunDuration ?? 0.2f,
                source = gameObject
            };
            
            // Set damage type from data
            damageInfo.damageType = ParseDamageType(weaponData.damageType ?? "Physical");
            
            return damageInfo;
        }
        
        private DamageType ParseDamageType(string damageTypeString)
        {
            switch (damageTypeString.ToLower())
            {
                case "physical": return DamageType.Physical;
                case "magic": return DamageType.Magic;
                case "fire": return DamageType.Fire;
                case "ice": return DamageType.Ice;
                case "poison": return DamageType.Poison;
                case "electric": return DamageType.Electric;
                default: return DamageType.Physical;
            }
        }
        
        // Expose data properties for UI and other systems
        public string GetDescription() => weaponData?.description ?? "";
        public string GetIconPath() => weaponData?.iconPath ?? "";
        public float GetCritChance() => weaponData?.critChance ?? 0f;
        public float GetCritMultiplier() => weaponData?.critMultiplier ?? 1f;
    }
    
    [System.Serializable]
    public class RuntimeWeaponStats
    {
        public string name;
        public float baseDamage;
        public float attackSpeed;
        public float range;
        public float critChance;
        public float critMultiplier;
    }
}
