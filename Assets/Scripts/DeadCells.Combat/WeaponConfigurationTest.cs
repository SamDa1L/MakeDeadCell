using UnityEngine;
using DeadCells.Data;

namespace DeadCells.Combat
{
    /// <summary>
    /// 武器配置系统测试组件
    /// 用于验证新的强类型接口系统替代 SendMessage
    /// </summary>
    public class WeaponConfigurationTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestOnStart = true;
        [SerializeField] private string testWeaponId = "sword";
        
        private void Start()
        {
            if (runTestOnStart)
            {
                TestWeaponConfiguration();
            }
        }
        
        /// <summary>
        /// 测试武器配置系统
        /// </summary>
        [ContextMenu("Test Weapon Configuration")]
        public void TestWeaponConfiguration()
        {
            Debug.Log("=== 开始武器配置系统测试 ===");
            
            // 测试1: 检查组件接口实现
            TestInterfaceImplementation();
            
            // 测试2: 检查配置管理器
            TestConfigurationManager();
            
            // 测试3: 模拟武器创建流程
            TestWeaponCreationFlow();
            
            Debug.Log("=== 武器配置系统测试完成 ===");
        }
        
        private void TestInterfaceImplementation()
        {
            Debug.Log("测试1: 检查接口实现");
            
            // 测试近战武器组件
            var meleeWeapon = gameObject.GetComponent<ExampleMeleeWeapon>();
            if (meleeWeapon == null)
            {
                meleeWeapon = gameObject.AddComponent<ExampleMeleeWeapon>();
            }
            
            TestWeaponComponent(meleeWeapon, "melee");
            
            // 测试远程武器组件
            var rangedWeapon = gameObject.GetComponent<ExampleRangedWeapon>();
            if (rangedWeapon == null)
            {
                rangedWeapon = gameObject.AddComponent<ExampleRangedWeapon>();
            }
            
            TestWeaponComponent(rangedWeapon, "ranged");
        }
        
        private void TestWeaponComponent(IWeaponConfigurable component, string expectedType)
        {
            if (component == null)
            {
                Debug.LogError($"组件为空，无法测试 {expectedType} 类型");
                return;
            }
            
            // 测试类型支持
            bool supportsType = component.SupportsWeaponType(expectedType);
            Debug.Log($"  {component.GetType().Name} 支持 {expectedType} 类型: {supportsType}");
            
            // 测试获取支持的类型
            var supportedTypes = component.GetSupportedWeaponTypes();
            Debug.Log($"  {component.GetType().Name} 支持的类型: [{string.Join(", ", supportedTypes)}]");
            
            // 测试无效类型
            bool supportsInvalid = component.SupportsWeaponType("invalid");
            Debug.Log($"  {component.GetType().Name} 支持无效类型: {supportsInvalid}");
        }
        
        private void TestConfigurationManager()
        {
            Debug.Log("测试2: 配置管理器验证");
            
            // 测试空对象处理
            var nullResults = WeaponConfigurationManager.ConfigureWeapon(null, null);
            Debug.Log($"  空对象配置结果数量: {nullResults.Count}");
            
            // 测试配置状态检查
            bool isConfigured = WeaponConfigurationManager.IsWeaponProperlyConfigured(gameObject, "melee");
            Debug.Log($"  当前对象配置状态 (melee): {isConfigured}");
            
            isConfigured = WeaponConfigurationManager.IsWeaponProperlyConfigured(gameObject, "ranged");
            Debug.Log($"  当前对象配置状态 (ranged): {isConfigured}");
        }
        
        private void TestWeaponCreationFlow()
        {
            Debug.Log("测试3: 模拟武器创建流程");
            
            // 创建测试武器数据
            var testWeaponData = CreateTestWeaponData();
            if (testWeaponData == null)
            {
                Debug.LogError("  无法创建测试武器数据");
                return;
            }
            
            // 测试配置流程
            var results = WeaponConfigurationManager.ConfigureWeapon(gameObject, testWeaponData);
            
            // 输出结果
            Debug.Log($"  配置结果数量: {results.Count}");
            foreach (var result in results)
            {
                Debug.Log($"    {result.ComponentName}: {(result.Success ? "成功" : $"失败 - {result.ErrorMessage}")}");
            }
            
            // 输出配置摘要
            string summary = WeaponConfigurationManager.GetConfigurationSummary(results);
            Debug.Log($"  配置摘要:\n{summary}");
        }
        
        private WeaponData CreateTestWeaponData()
        {
            return new WeaponData
            {
                id = "test_sword",
                name = "测试剑",
                weaponType = "melee",
                range = 2.0f,
                stats = new WeaponStats
                {
                    damage = 10,
                    knockback = 5.0f,
                    hitstunDuration = 0.3f,
                    maxAmmo = 0,
                    reloadTime = 0,
                    piercing = false,
                    maxPierceCount = 0
                }
            };
        }
        
        /// <summary>
        /// 清理测试组件
        /// </summary>
        [ContextMenu("Clean Test Components")]
        public void CleanTestComponents()
        {
            var meleeWeapon = GetComponent<ExampleMeleeWeapon>();
            if (meleeWeapon != null)
            {
                DestroyImmediate(meleeWeapon);
                Debug.Log("已清理 ExampleMeleeWeapon 组件");
            }
            
            var rangedWeapon = GetComponent<ExampleRangedWeapon>();
            if (rangedWeapon != null)
            {
                DestroyImmediate(rangedWeapon);
                Debug.Log("已清理 ExampleRangedWeapon 组件");
            }
        }
    }
}