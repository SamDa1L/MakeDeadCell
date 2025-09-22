using UnityEngine;
using DeadCells.Rooms;

namespace DeadCells.Core
{
    /// <summary>
    /// PrefabRegistry 验证器 - 在运行时检查预制体注册表配置
    /// </summary>
    public class PrefabRegistryValidator : MonoBehaviour
    {
        [Header("Validation Settings")]
        [SerializeField] private bool validateOnStart = true;
        [SerializeField] private bool logMissingPrefabs = true;
        
        private void Start()
        {
            if (validateOnStart)
            {
                ValidateAllRegistries();
            }
        }
        
        /// <summary>
        /// 验证所有 PrefabRegistry 配置
        /// </summary>
        [ContextMenu("Validate All Prefab Registries")]
        public void ValidateAllRegistries()
        {
            Debug.Log("=== PrefabRegistry 验证开始 ===");
            
            // 验证 LDtkRoomManager 的 PrefabRegistry
            ValidateLDtkRoomManagerRegistry();
            
            Debug.Log("=== PrefabRegistry 验证完成 ===");
        }
        
        private void ValidateLDtkRoomManagerRegistry()
        {
            var ldtkManager = FindObjectOfType<LDtkRoomManager>();
            if (ldtkManager == null)
            {
                Debug.LogWarning("PrefabRegistryValidator: 未找到 LDtkRoomManager");
                return;
            }
            
            // 使用反射获取私有字段 prefabRegistry
            var field = typeof(LDtkRoomManager).GetField("prefabRegistry", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field == null)
            {
                Debug.LogError("PrefabRegistryValidator: 无法访问 LDtkRoomManager.prefabRegistry 字段");
                return;
            }
            
            var prefabRegistry = field.GetValue(ldtkManager) as PrefabRegistry;
            
            if (prefabRegistry == null)
            {
                Debug.LogError("LDtkRoomManager: PrefabRegistry 未分配！这将导致运行时错误。");
                Debug.LogError("解决方案: 在 LDtkRoomManager Inspector 中分配 PrefabRegistry ScriptableObject");
                return;
            }
            
            ValidatePrefabRegistry(prefabRegistry, "LDtkRoomManager");
        }
        
        private void ValidatePrefabRegistry(PrefabRegistry registry, string context)
        {
            Debug.Log($"验证 {context} 的 PrefabRegistry: {registry.name}");
            
            // 验证敌人预制体
            var enemyIds = registry.GetRegisteredEnemyIds();
            Debug.Log($"  已注册敌人预制体数量: {enemyIds.Length}");
            
            if (logMissingPrefabs && enemyIds.Length == 0)
            {
                Debug.LogWarning($"  警告: {context} 没有注册任何敌人预制体");
            }
            
            // 验证物品预制体
            var itemIds = registry.GetRegisteredItemIds();
            Debug.Log($"  已注册物品预制体数量: {itemIds.Length}");
            
            if (logMissingPrefabs && itemIds.Length == 0)
            {
                Debug.LogWarning($"  警告: {context} 没有注册任何物品预制体");
            }
            
            // 测试常用预制体
            TestCommonPrefabs(registry, context);
        }
        
        private void TestCommonPrefabs(PrefabRegistry registry, string context)
        {
            // 测试默认敌人预制体
            var defaultEnemy = registry.GetEnemyPrefab("DefaultEnemy");
            if (defaultEnemy == null)
            {
                Debug.LogWarning($"  {context}: 默认敌人预制体不可用");
            }
            else
            {
                Debug.Log($"  {context}: 默认敌人预制体: {defaultEnemy.name}");
            }
            
            // 测试常见物品预制体
            string[] commonItems = { "coin", "Coin", "treasure", "Treasure" };
            foreach (string itemId in commonItems)
            {
                if (registry.HasItemPrefab(itemId))
                {
                    var item = registry.GetItemPrefab(itemId);
                    Debug.Log($"  {context}: 找到物品预制体 '{itemId}': {item.name}");
                    break;
                }
            }
        }
        
        /// <summary>
        /// 创建示例 PrefabRegistry 配置
        /// </summary>
        [ContextMenu("Create Example PrefabRegistry")]
        public void CreateExamplePrefabRegistry()
        {
            Debug.Log("提示: 要创建 PrefabRegistry，请在 Project 窗口中右键点击");
            Debug.Log("选择 Create > DeadCells > Core > Prefab Registry");
            Debug.Log("然后在 Inspector 中配置敌人和物品预制体映射");
        }
    }
}