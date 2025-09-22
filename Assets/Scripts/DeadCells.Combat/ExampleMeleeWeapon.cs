using UnityEngine;
using DeadCells.Data;

namespace DeadCells.Combat
{
    /// <summary>
    /// 示例近战武器组件 - 展示如何实现强类型武器配置接口
    /// 替代旧的 SendMessage("ConfigureMeleeStats") 方式
    /// </summary>
    public class ExampleMeleeWeapon : MonoBehaviour, IMeleeWeaponConfigurable
    {
        [Header("Melee Weapon Properties")]
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float knockbackForce = 5f;
        [SerializeField] private float hitstunDuration = 0.2f;
        [SerializeField] private bool isConfigured = false;
        
        // 武器数据引用
        private WeaponData weaponData;
        private WeaponStats weaponStats;
        
        #region IWeaponConfigurable Implementation
        
        public bool ConfigureWeapon(WeaponData weaponData)
        {
            if (weaponData == null)
            {
                Debug.LogError($"ExampleMeleeWeapon: Cannot configure with null WeaponData");
                return false;
            }
            
            this.weaponData = weaponData;
            
            // 配置基础属性
            attackRange = weaponData.range;
            
            Debug.Log($"ExampleMeleeWeapon: Configured base properties for '{weaponData.name}'");
            return true;
        }
        
        public string[] GetSupportedWeaponTypes()
        {
            return new string[] { "melee", "Melee", "MELEE" };
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
        
        #region IMeleeWeaponConfigurable Implementation
        
        public bool ConfigureMeleeStats(WeaponStats weaponStats)
        {
            if (weaponStats == null)
            {
                Debug.LogWarning($"ExampleMeleeWeapon: Received null WeaponStats");
                return false;
            }
            
            this.weaponStats = weaponStats;
            
            // 配置近战特有属性
            knockbackForce = weaponStats.knockback;
            hitstunDuration = weaponStats.hitstunDuration;
            
            isConfigured = true;
            
            Debug.Log($"ExampleMeleeWeapon: Configured melee stats - " +
                     $"Knockback: {knockbackForce}, Hitstun: {hitstunDuration}");
            
            return true;
        }
        
        public void ConfigureAttackRange(float range)
        {
            attackRange = range;
            Debug.Log($"ExampleMeleeWeapon: Attack range set to {range}");
        }
        
        public void ConfigureKnockback(float knockback)
        {
            knockbackForce = knockback;
            Debug.Log($"ExampleMeleeWeapon: Knockback force set to {knockback}");
        }
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// 检查武器是否已配置
        /// </summary>
        public bool IsConfigured => isConfigured && weaponData != null;
        
        /// <summary>
        /// 获取攻击范围
        /// </summary>
        public float AttackRange => attackRange;
        
        /// <summary>
        /// 获取击退力度
        /// </summary>
        public float KnockbackForce => knockbackForce;
        
        /// <summary>
        /// 获取击晕时长
        /// </summary>
        public float HitstunDuration => hitstunDuration;
        
        /// <summary>
        /// 获取武器数据
        /// </summary>
        public WeaponData WeaponData => weaponData;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            if (!isConfigured)
            {
                Debug.LogWarning($"ExampleMeleeWeapon: '{gameObject.name}' has not been configured. " +
                               "Make sure WeaponFactory properly configures this weapon.");
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
            string status = $"ExampleMeleeWeapon Configuration Status:\n" +
                          $"- Configured: {isConfigured}\n" +
                          $"- Weapon Data: {(weaponData != null ? weaponData.name : "null")}\n" +
                          $"- Attack Range: {attackRange}\n" +
                          $"- Knockback Force: {knockbackForce}\n" +
                          $"- Hitstun Duration: {hitstunDuration}";
            
            Debug.Log(status);
        }
        
        #endregion
    }
}