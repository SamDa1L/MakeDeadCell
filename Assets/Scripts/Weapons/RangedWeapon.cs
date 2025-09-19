using UnityEngine;
using DeadCellsTestFramework.Combat;

namespace DeadCellsTestFramework.Weapons
{
    public class RangedWeapon : Weapon
    {
        [Header("Ranged Settings")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 10f;
        [SerializeField] private int maxAmmo = 30;
        [SerializeField] private float reloadTime = 2f;
        
        private int currentAmmo;
        private bool isReloading = false;
        
        private void Awake()
        {
            currentAmmo = maxAmmo;
            
            if (firePoint == null)
                firePoint = transform;
        }
        
        public override bool TryAttack(Vector2 attackDirection)
        {
            if (!canAttack || isReloading || currentAmmo <= 0) 
            {
                if (currentAmmo <= 0 && !isReloading)
                    StartReload();
                return false;
            }
            
            PerformAttack(attackDirection);
            return true;
        }
        
        protected override void PerformAttack(Vector2 attackDirection)
        {
            RegisterAttack();
            currentAmmo--;
            
            // Spawn projectile
            if (projectilePrefab != null)
            {
                GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
                
                Projectile projComponent = projectile.GetComponent<Projectile>();
                if (projComponent == null)
                    projComponent = projectile.AddComponent<Projectile>();
                
                DamageInfo damageInfo = CreateDamageInfo(attackDirection);
                projComponent.Initialize(attackDirection, projectileSpeed, range, damageInfo, targetLayerMask);
            }
            
            PlayAttackEffects();
        }
        
        private void StartReload()
        {
            if (isReloading) return;
            
            isReloading = true;
            Invoke(nameof(CompleteReload), reloadTime);
        }
        
        private void CompleteReload()
        {
            isReloading = false;
            currentAmmo = maxAmmo;
        }
        
        private void PlayAttackEffects()
        {
            // Muzzle flash
            // Firing sound
            // Weapon recoil animation
        }
        
        public int GetCurrentAmmo() => currentAmmo;
        public int GetMaxAmmo() => maxAmmo;
        public bool IsReloading() => isReloading;
    }
}