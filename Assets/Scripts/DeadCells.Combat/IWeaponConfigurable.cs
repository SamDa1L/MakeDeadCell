using UnityEngine;
using DeadCells.Data;

namespace DeadCells.Combat
{
    /// <summary>
    /// 武器配置接口 - 强类型替代 SendMessage
    /// 所有需要接受武器数据配置的组件都应该实现此接口
    /// </summary>
    public interface IWeaponConfigurable
    {
        /// <summary>
        /// 配置武器基础属性
        /// </summary>
        /// <param name="weaponData">完整武器数据</param>
        /// <returns>配置是否成功</returns>
        bool ConfigureWeapon(WeaponData weaponData);
        
        /// <summary>
        /// 获取支持的武器类型
        /// </summary>
        /// <returns>支持的武器类型字符串数组</returns>
        string[] GetSupportedWeaponTypes();
        
        /// <summary>
        /// 检查是否支持特定武器类型
        /// </summary>
        /// <param name="weaponType">武器类型</param>
        /// <returns>是否支持</returns>
        bool SupportsWeaponType(string weaponType);
    }
    
    /// <summary>
    /// 近战武器配置接口
    /// </summary>
    public interface IMeleeWeaponConfigurable : IWeaponConfigurable
    {
        /// <summary>
        /// 配置近战武器特有属性
        /// </summary>
        /// <param name="weaponStats">武器统计数据</param>
        /// <returns>配置是否成功</returns>
        bool ConfigureMeleeStats(WeaponStats weaponStats);
        
        /// <summary>
        /// 配置攻击范围
        /// </summary>
        /// <param name="range">攻击范围</param>
        void ConfigureAttackRange(float range);
        
        /// <summary>
        /// 配置击退效果
        /// </summary>
        /// <param name="knockback">击退力度</param>
        void ConfigureKnockback(float knockback);
    }
    
    /// <summary>
    /// 远程武器配置接口
    /// </summary>
    public interface IRangedWeaponConfigurable : IWeaponConfigurable
    {
        /// <summary>
        /// 配置远程武器特有属性
        /// </summary>
        /// <param name="weaponStats">武器统计数据</param>
        /// <returns>配置是否成功</returns>
        bool ConfigureRangedStats(WeaponStats weaponStats);
        
        /// <summary>
        /// 配置弹药系统
        /// </summary>
        /// <param name="maxAmmo">最大弹药数</param>
        /// <param name="reloadTime">装弹时间</param>
        void ConfigureAmmo(int maxAmmo, float reloadTime);
        
        /// <summary>
        /// 配置穿透属性
        /// </summary>
        /// <param name="piercing">是否穿透</param>
        /// <param name="maxPierceCount">最大穿透数量</param>
        void ConfigurePiercing(bool piercing, int maxPierceCount);
    }
    
    /// <summary>
    /// 武器配置结果
    /// </summary>
    public struct WeaponConfigurationResult
    {
        public bool Success;
        public string ErrorMessage;
        public string ComponentName;
        
        public static WeaponConfigurationResult CreateSuccess(string componentName)
        {
            return new WeaponConfigurationResult
            {
                Success = true,
                ComponentName = componentName,
                ErrorMessage = string.Empty
            };
        }
        
        public static WeaponConfigurationResult CreateFailure(string componentName, string error)
        {
            return new WeaponConfigurationResult
            {
                Success = false,
                ComponentName = componentName,
                ErrorMessage = error
            };
        }
    }
}