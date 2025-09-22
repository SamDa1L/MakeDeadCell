using UnityEngine;
using DeadCells.Data;

namespace DeadCells.Combat
{
    /// <summary>
    /// 示例远程武器组件 - 展示如何实现强类型武器配置接口
    /// 替代旧的 SendMessage("ConfigureRangedStats") 方式
    /// </summary>
    public class ExampleRangedWeapon : MonoBehaviour, IRangedWeaponConfigurable
    {
        [Header("Ranged Weapon Properties")]
        [SerializeField] private int maxAmmo = 30;
        [SerializeField] private float reloadTime = 2f;
        [SerializeField] private bool piercing = false;
        [SerializeField] private int maxPierceCount = 1;
        [SerializeField] private bool isConfigured = false;
        
        // 武器数据引用
        private WeaponData weaponData;
        private WeaponStats weaponStats;
        
        // 运行时状态
        [Header("Runtime Status")]
        [SerializeField] private int currentAmmo;
        [SerializeField] private bool isReloading = false;
        
        #region IWeaponConfigurable Implementation
        
        public bool ConfigureWeapon(WeaponData weaponData)
        {
            if (weaponData == null)
            {
                Debug.LogError($"ExampleRangedWeapon: Cannot configure with null WeaponData");
                return false;
            }
            
            this.weaponData = weaponData;
            
            Debug.Log($"ExampleRangedWeapon: Configured base properties for '{weaponData.name}'");
            return true;
        }
        
        public string[] GetSupportedWeaponTypes()
        {
            return new string[] { "ranged", "Ranged", "RANGED" };
        }
        
        public bool SupportsWeaponType(string weaponType)
        {
            if (string.IsNullOrEmpty(weaponType)) return false;
            
            string[] supportedTypes = GetSupportedWeaponTypes();
            foreach (string supportedType in supportedTypes)
            {
                if (string.Equals(weaponType, supportedType, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
        
        #endregion
        
        #region IRangedWeaponConfigurable Implementation
        
        public bool ConfigureRangedStats(WeaponStats weaponStats)
        {
            if (weaponStats == null)
            {
                Debug.LogWarning($"ExampleRangedWeapon: Received null WeaponStats");
                return false;
            }
            
            this.weaponStats = weaponStats;
            
            // 配置远程武器特有属性
            maxAmmo = weaponStats.maxAmmo;
            reloadTime = weaponStats.reloadTime;
            piercing = weaponStats.piercing;
            maxPierceCount = weaponStats.maxPierceCount;
            
            // 初始化弹药
            currentAmmo = maxAmmo;
            
            isConfigured = true;
            
            Debug.Log($"ExampleRangedWeapon: Configured ranged stats - " +
                     $"MaxAmmo: {maxAmmo}, ReloadTime: {reloadTime}, " +
                     $"Piercing: {piercing}, MaxPierceCount: {maxPierceCount}");
            
            return true;
        }
        
        public void ConfigureAmmo(int maxAmmo, float reloadTime)
        {
            this.maxAmmo = maxAmmo;
            this.reloadTime = reloadTime;
            this.currentAmmo = maxAmmo; // 重置当前弹药
            
            Debug.Log($"ExampleRangedWeapon: Ammo configured - Max: {maxAmmo}, ReloadTime: {reloadTime}");
        }
        
        public void ConfigurePiercing(bool piercing, int maxPierceCount)
        {
            this.piercing = piercing;
            this.maxPierceCount = maxPierceCount;
            
            Debug.Log($"ExampleRangedWeapon: Piercing configured - Enabled: {piercing}, MaxCount: {maxPierceCount}");
        }
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// 检查武器是否已配置
        /// </summary>
        public bool IsConfigured => isConfigured && weaponData != null;
        
        /// <summary>
        /// 获取最大弹药数
        /// </summary>
        public int MaxAmmo => maxAmmo;
        
        /// <summary>
        /// 获取当前弹药数
        /// </summary>
        public int CurrentAmmo => currentAmmo;
        
        /// <summary>
        /// 获取装弹时间
        /// </summary>
        public float ReloadTime => reloadTime;
        
        /// <summary>
        /// 检查是否具备穿透能力
        /// </summary>
        public bool CanPierce => piercing;
        
        /// <summary>
        /// 获取最大穿透数量
        /// </summary>
        public int MaxPierceCount => maxPierceCount;
        
        /// <summary>
        /// 检查是否在装弹中
        /// </summary>
        public bool IsReloading => isReloading;
        
        /// <summary>
        /// 获取武器数据
        /// </summary>
        public WeaponData WeaponData => weaponData;
        
        #endregion
        
        #region Weapon Actions
        
        /// <summary>
        /// 射击 - 消耗弹药
        /// </summary>
        public bool TryShoot()
        {
            if (isReloading || currentAmmo <= 0)
            {
                return false;
            }
            
            currentAmmo--;
            Debug.Log($"ExampleRangedWeapon: Shot fired. Ammo remaining: {currentAmmo}/{maxAmmo}");
            
            if (currentAmmo <= 0)
            {
                Debug.Log($"ExampleRangedWeapon: Out of ammo. Reload required.");
            }
            
            return true;
        }
        
        /// <summary>
        /// 装弹
        /// </summary>
        public void StartReload()
        {
            if (isReloading || currentAmmo >= maxAmmo)
            {
                return;
            }
            
            StartCoroutine(ReloadCoroutine());
        }
        
        private System.Collections.IEnumerator ReloadCoroutine()
        {
            isReloading = true;
            Debug.Log($"ExampleRangedWeapon: Reloading started. Duration: {reloadTime}s");
            
            yield return new WaitForSeconds(reloadTime);
            
            currentAmmo = maxAmmo;
            isReloading = false;
            
            Debug.Log($"ExampleRangedWeapon: Reload completed. Ammo: {currentAmmo}/{maxAmmo}");
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            if (!isConfigured)
            {
                Debug.LogWarning($"ExampleRangedWeapon: '{gameObject.name}' has not been configured. " +
                               "Make sure WeaponFactory properly configures this weapon.");
            }
            else
            {
                currentAmmo = maxAmmo; // 确保弹药初始化
            }
        }
        
        #endregion
        
        #region Debug Methods
        
        /// <summary>
        /// 获取配置状态信息
        /// </summary>
        [ContextMenu("Show Configuration Status")]
        public void ShowConfigurationStatus()
        {
            string status = $"ExampleRangedWeapon Configuration Status:\n" +
                          $"- Configured: {isConfigured}\n" +
                          $"- Weapon Data: {(weaponData != null ? weaponData.name : "null")}\n" +
                          $"- Max Ammo: {maxAmmo}\n" +
                          $"- Current Ammo: {currentAmmo}\n" +
                          $"- Reload Time: {reloadTime}\n" +
                          $"- Piercing: {piercing}\n" +
                          $"- Max Pierce Count: {maxPierceCount}\n" +
                          $"- Is Reloading: {isReloading}";
            
            Debug.Log(status);
        }
        
        /// <summary>
        /// 测试射击
        /// </summary>
        [ContextMenu("Test Shoot")]
        public void TestShoot()
        {
            bool success = TryShoot();
            Debug.Log($"ExampleRangedWeapon: Test shoot {(success ? "successful" : "failed")}");
        }
        
        /// <summary>
        /// 测试装弹
        /// </summary>
        [ContextMenu("Test Reload")]
        public void TestReload()
        {
            StartReload();
        }
        
        #endregion
    }
}