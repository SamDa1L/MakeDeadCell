using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DeadCells.Data;

namespace DeadCells.Combat
{
    /// <summary>
    /// 武器配置管理器 - 统一管理武器配置流程
    /// 替代不安全的 SendMessage 机制
    /// </summary>
    public static class WeaponConfigurationManager
    {
        /// <summary>
        /// 配置武器的所有组件
        /// </summary>
        /// <param name="weaponGameObject">武器游戏对象</param>
        /// <param name="weaponData">武器数据</param>
        /// <returns>配置结果列表</returns>
        public static List<WeaponConfigurationResult> ConfigureWeapon(GameObject weaponGameObject, WeaponData weaponData)
        {
            var results = new List<WeaponConfigurationResult>();
            
            if (weaponGameObject == null)
            {
                results.Add(WeaponConfigurationResult.CreateFailure("GameObject", "Weapon GameObject is null"));
                return results;
            }
            
            if (weaponData == null)
            {
                results.Add(WeaponConfigurationResult.CreateFailure("WeaponData", "WeaponData is null"));
                return results;
            }
            
            // 获取所有实现了武器配置接口的组件
            var configurableComponents = weaponGameObject.GetComponents<IWeaponConfigurable>();
            
            if (configurableComponents.Length == 0)
            {
                results.Add(WeaponConfigurationResult.CreateFailure("Components", "No IWeaponConfigurable components found"));
                return results;
            }
            
            // 配置每个组件
            foreach (var component in configurableComponents)
            {
                var result = ConfigureComponent(component, weaponData);
                results.Add(result);
            }
            
            return results;
        }
        
        /// <summary>
        /// 配置单个组件
        /// </summary>
        /// <param name="component">实现配置接口的组件</param>
        /// <param name="weaponData">武器数据</param>
        /// <returns>配置结果</returns>
        private static WeaponConfigurationResult ConfigureComponent(IWeaponConfigurable component, WeaponData weaponData)
        {
            string componentName = component.GetType().Name;
            
            try
            {
                // 检查组件是否支持该武器类型
                if (!component.SupportsWeaponType(weaponData.weaponType))
                {
                    return WeaponConfigurationResult.CreateFailure(componentName, 
                        $"Component does not support weapon type: {weaponData.weaponType}");
                }
                
                // 基础配置
                bool baseConfigSuccess = component.ConfigureWeapon(weaponData);
                if (!baseConfigSuccess)
                {
                    return WeaponConfigurationResult.CreateFailure(componentName, "Base configuration failed");
                }
                
                // 特化配置
                bool specializedConfigSuccess = ConfigureSpecializedWeapon(component, weaponData);
                if (!specializedConfigSuccess)
                {
                    return WeaponConfigurationResult.CreateFailure(componentName, "Specialized configuration failed");
                }
                
                return WeaponConfigurationResult.CreateSuccess(componentName);
            }
            catch (System.Exception ex)
            {
                return WeaponConfigurationResult.CreateFailure(componentName, 
                    $"Configuration exception: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 配置特化武器类型
        /// </summary>
        /// <param name="component">武器组件</param>
        /// <param name="weaponData">武器数据</param>
        /// <returns>配置是否成功</returns>
        private static bool ConfigureSpecializedWeapon(IWeaponConfigurable component, WeaponData weaponData)
        {
            string weaponType = weaponData.weaponType?.ToLower();
            
            switch (weaponType)
            {
                case "melee":
                    return ConfigureMeleeWeapon(component, weaponData);
                case "ranged":
                    return ConfigureRangedWeapon(component, weaponData);
                default:
                    Debug.LogWarning($"Unknown weapon type: {weaponData.weaponType}. Skipping specialized configuration.");
                    return true; // 不是错误，只是跳过特化配置
            }
        }
        
        /// <summary>
        /// 配置近战武器
        /// </summary>
        /// <param name="component">武器组件</param>
        /// <param name="weaponData">武器数据</param>
        /// <returns>配置是否成功</returns>
        private static bool ConfigureMeleeWeapon(IWeaponConfigurable component, WeaponData weaponData)
        {
            if (component is IMeleeWeaponConfigurable meleeComponent)
            {
                bool statsConfigured = true;
                
                // 配置统计数据
                if (weaponData.stats != null)
                {
                    statsConfigured = meleeComponent.ConfigureMeleeStats(weaponData.stats);
                }
                
                // 配置攻击范围
                meleeComponent.ConfigureAttackRange(weaponData.range);
                
                // 配置击退
                if (weaponData.stats != null)
                {
                    meleeComponent.ConfigureKnockback(weaponData.stats.knockback);
                }
                
                return statsConfigured;
            }
            
            return true; // 组件不支持近战特化，但不是错误
        }
        
        /// <summary>
        /// 配置远程武器
        /// </summary>
        /// <param name="component">武器组件</param>
        /// <param name="weaponData">武器数据</param>
        /// <returns>配置是否成功</returns>
        private static bool ConfigureRangedWeapon(IWeaponConfigurable component, WeaponData weaponData)
        {
            if (component is IRangedWeaponConfigurable rangedComponent)
            {
                bool statsConfigured = true;
                
                // 配置统计数据
                if (weaponData.stats != null)
                {
                    statsConfigured = rangedComponent.ConfigureRangedStats(weaponData.stats);
                    
                    // 配置弹药系统
                    rangedComponent.ConfigureAmmo(weaponData.stats.maxAmmo, weaponData.stats.reloadTime);
                    
                    // 配置穿透
                    rangedComponent.ConfigurePiercing(weaponData.stats.piercing, weaponData.stats.maxPierceCount);
                }
                
                return statsConfigured;
            }
            
            return true; // 组件不支持远程特化，但不是错误
        }
        
        /// <summary>
        /// 获取武器配置状态摘要
        /// </summary>
        /// <param name="results">配置结果列表</param>
        /// <returns>状态摘要</returns>
        public static string GetConfigurationSummary(List<WeaponConfigurationResult> results)
        {
            if (results == null || results.Count == 0)
            {
                return "No configuration results available";
            }
            
            int successCount = results.Count(r => r.Success);
            int failureCount = results.Count(r => !r.Success);
            
            var summary = $"Configuration Summary: {successCount} succeeded, {failureCount} failed";
            
            if (failureCount > 0)
            {
                var failures = results.Where(r => !r.Success).Select(r => $"{r.ComponentName}: {r.ErrorMessage}");
                summary += $"\nFailures:\n{string.Join("\n", failures)}";
            }
            
            return summary;
        }
        
        /// <summary>
        /// 检查武器是否正确配置
        /// </summary>
        /// <param name="weaponGameObject">武器游戏对象</param>
        /// <param name="weaponType">预期武器类型</param>
        /// <returns>是否正确配置</returns>
        public static bool IsWeaponProperlyConfigured(GameObject weaponGameObject, string weaponType)
        {
            if (weaponGameObject == null) return false;
            
            var configurableComponents = weaponGameObject.GetComponents<IWeaponConfigurable>();
            if (configurableComponents.Length == 0) return false;
            
            // 检查是否至少有一个组件支持该武器类型
            return configurableComponents.Any(c => c.SupportsWeaponType(weaponType));
        }
    }
}