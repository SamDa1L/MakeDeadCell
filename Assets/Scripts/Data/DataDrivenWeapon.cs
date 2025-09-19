using UnityEngine;
using DeadCellsTestFramework.Weapons;
using DeadCellsTestFramework.Combat;

namespace DeadCellsTestFramework.Data
{
    public class DataDrivenWeapon : Weapon
    {
        [Header("Data-Driven Properties")]
        [SerializeField] private WeaponData weaponData;
        [SerializeField] private bool useDataValues = true;
        
        public WeaponData WeaponData => weaponData;
        
        public void Initialize(WeaponData data)
        {
            weaponData = data;
            
            if (useDataValues && weaponData != null)
            {
                ApplyWeaponData();
            }
        }
        
        private void ApplyWeaponData()
        {
            // Override base weapon properties with data values
            weaponName = weaponData.name;
            baseDamage = weaponData.baseDamage;
            attackSpeed = weaponData.attackSpeed;
            range = weaponData.range;
            
            // Set weapon type
            switch (weaponData.weaponType.ToLower())
            {
                case "melee":
                    weaponType = WeaponType.Melee;
                    break;
                case "ranged":
                    weaponType = WeaponType.Ranged;
                    break;
                case "magic":
                    weaponType = WeaponType.Magic;
                    break;
                default:
                    weaponType = WeaponType.Melee;
                    break;
            }
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
            
            switch (weaponType)
            {
                case WeaponType.Melee:
                    PerformMeleeAttack(attackDirection);
                    break;
                case WeaponType.Ranged:
                    PerformRangedAttack(attackDirection);
                    break;
                case WeaponType.Magic:
                    PerformMagicAttack(attackDirection);
                    break;
            }
        }
        
        private void PerformMeleeAttack(Vector2 attackDirection)
        {
            // Get attack point and radius from data or use defaults
            Transform attackPoint = transform.Find("AttackPoint") ?? transform;
            float attackRadius = weaponData?.stats?.knockback ?? 1f;
            
            // Detect targets in attack range
            Collider2D[] hitTargets = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, targetLayerMask);
            
            foreach (var target in hitTargets)
            {
                if (target.gameObject == gameObject) continue;
                
                Vector2 knockbackDirection = (target.transform.position - transform.position).normalized;
                DamageInfo damageInfo = CreateDataDrivenDamageInfo(knockbackDirection);
                
                CombatManager.Instance?.DealDamage(gameObject, target.gameObject, damageInfo);
            }
        }
        
        private void PerformRangedAttack(Vector2 attackDirection)
        {
            Transform firePoint = transform.Find("FirePoint") ?? transform;
            
            // Load projectile prefab based on data
            GameObject projectilePrefab = LoadProjectilePrefab();
            if (projectilePrefab != null)
            {
                GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
                
                Projectile projComponent = projectile.GetComponent<Projectile>();
                if (projComponent == null)
                    projComponent = projectile.AddComponent<Projectile>();
                
                DamageInfo damageInfo = CreateDataDrivenDamageInfo(attackDirection);
                float projectileSpeed = 10f; // Could be in weapon data
                
                projComponent.Initialize(attackDirection, projectileSpeed, range, damageInfo, targetLayerMask);
            }
        }
        
        private void PerformMagicAttack(Vector2 attackDirection)
        {
            // Implement magic attack logic
            // This could spawn spell effects, area damage, etc.
            PerformRangedAttack(attackDirection); // Fallback to ranged for now
        }
        
        protected override DamageInfo CreateDamageInfo(Vector2 knockbackDirection)
        {
            return CreateDataDrivenDamageInfo(knockbackDirection);
        }
        
        private DamageInfo CreateDataDrivenDamageInfo(Vector2 knockbackDirection)
        {
            var damageInfo = new DamageInfo
            {
                baseDamage = weaponData?.baseDamage ?? baseDamage,
                canCrit = true,
                critChance = weaponData?.critChance ?? 0.05f,
                critMultiplier = weaponData?.critMultiplier ?? 2f,
                knockback = knockbackDirection * (weaponData?.stats?.knockback ?? 5f),
                hitstunDuration = weaponData?.stats?.hitstunDuration ?? 0.2f,
                source = gameObject
            };
            
            // Set damage type from data
            damageInfo.damageType = ParseDamageType(weaponData?.damageType ?? "Physical");
            
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
        
        private GameObject LoadProjectilePrefab()
        {
            // Try to load from the prefab path specified in data
            if (!string.IsNullOrEmpty(weaponData?.prefabPath))
            {
                return Resources.Load<GameObject>(weaponData.prefabPath);
            }
            
            // Fallback to default projectile
            return Resources.Load<GameObject>("Weapons/DefaultProjectile");
        }
        
        // Expose data properties for UI and other systems
        public string GetDescription() => weaponData?.description ?? "";
        public string GetIconPath() => weaponData?.iconPath ?? "";
        public float GetCritChance() => weaponData?.critChance ?? 0f;
        public float GetCritMultiplier() => weaponData?.critMultiplier ?? 1f;
    }
}