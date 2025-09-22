using UnityEngine;
using System.Collections.Generic;

namespace DeadCells.Core
{
    /// <summary>
    /// 预制体注册表 - 替代 Resources.Load 的类型安全解决方案
    /// 通过直接引用和注册表避免运行时资源加载失败
    /// </summary>
    [CreateAssetMenu(fileName = "PrefabRegistry", menuName = "DeadCells/Core/Prefab Registry")]
    public class PrefabRegistry : ScriptableObject
    {
        [Header("Enemy Prefabs")]
        [SerializeField] private PrefabEntry[] enemyPrefabs;
        
        [Header("Item Prefabs")]
        [SerializeField] private PrefabEntry[] itemPrefabs;
        
        [Header("Fallback Prefabs")]
        [SerializeField] private GameObject defaultEnemyPrefab;
        [SerializeField] private GameObject defaultItemPrefab;
        
        private Dictionary<string, GameObject> enemyPrefabMap;
        private Dictionary<string, GameObject> itemPrefabMap;
        
        private void OnEnable()
        {
            BuildPrefabMaps();
        }
        
        private void BuildPrefabMaps()
        {
            // 构建敌人预制体映射
            enemyPrefabMap = new Dictionary<string, GameObject>();
            if (enemyPrefabs != null)
            {
                foreach (var entry in enemyPrefabs)
                {
                    if (!string.IsNullOrEmpty(entry.id) && entry.prefab != null)
                    {
                        enemyPrefabMap[entry.id] = entry.prefab;
                    }
                }
            }
            
            // 构建物品预制体映射
            itemPrefabMap = new Dictionary<string, GameObject>();
            if (itemPrefabs != null)
            {
                foreach (var entry in itemPrefabs)
                {
                    if (!string.IsNullOrEmpty(entry.id) && entry.prefab != null)
                    {
                        itemPrefabMap[entry.id] = entry.prefab;
                    }
                }
            }
        }
        
        /// <summary>
        /// 获取敌人预制体
        /// </summary>
        /// <param name="enemyId">敌人ID或路径</param>
        /// <returns>敌人预制体，找不到时返回默认预制体</returns>
        public GameObject GetEnemyPrefab(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId))
            {
                return defaultEnemyPrefab;
            }
            
            // 确保映射已构建
            if (enemyPrefabMap == null)
            {
                BuildPrefabMaps();
            }
            
            // 尝试通过ID查找
            if (enemyPrefabMap.TryGetValue(enemyId, out GameObject prefab))
            {
                return prefab;
            }
            
            // 尝试通过路径匹配 (如 "Enemies/Goblin" 匹配 "Goblin")
            string filename = System.IO.Path.GetFileNameWithoutExtension(enemyId);
            if (enemyPrefabMap.TryGetValue(filename, out prefab))
            {
                return prefab;
            }
            
            Debug.LogWarning($"PrefabRegistry: Enemy prefab not found for id '{enemyId}', using default");
            return defaultEnemyPrefab;
        }
        
        /// <summary>
        /// 获取物品预制体
        /// </summary>
        /// <param name="itemId">物品ID或路径</param>
        /// <returns>物品预制体，找不到时返回默认预制体</returns>
        public GameObject GetItemPrefab(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return defaultItemPrefab;
            }
            
            // 确保映射已构建
            if (itemPrefabMap == null)
            {
                BuildPrefabMaps();
            }
            
            // 尝试通过ID查找
            if (itemPrefabMap.TryGetValue(itemId, out GameObject prefab))
            {
                return prefab;
            }
            
            // 尝试通过路径匹配 (如 "Items/Coin" 匹配 "Coin")
            string filename = System.IO.Path.GetFileNameWithoutExtension(itemId);
            if (itemPrefabMap.TryGetValue(filename, out prefab))
            {
                return prefab;
            }
            
            Debug.LogWarning($"PrefabRegistry: Item prefab not found for id '{itemId}', using default");
            return defaultItemPrefab;
        }
        
        /// <summary>
        /// 检查敌人预制体是否存在
        /// </summary>
        /// <param name="enemyId">敌人ID</param>
        /// <returns>是否存在</returns>
        public bool HasEnemyPrefab(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId) || enemyPrefabMap == null)
                return false;
                
            return enemyPrefabMap.ContainsKey(enemyId) || 
                   enemyPrefabMap.ContainsKey(System.IO.Path.GetFileNameWithoutExtension(enemyId));
        }
        
        /// <summary>
        /// 检查物品预制体是否存在
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <returns>是否存在</returns>
        public bool HasItemPrefab(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || itemPrefabMap == null)
                return false;
                
            return itemPrefabMap.ContainsKey(itemId) || 
                   itemPrefabMap.ContainsKey(System.IO.Path.GetFileNameWithoutExtension(itemId));
        }
        
        /// <summary>
        /// 获取所有注册的敌人ID
        /// </summary>
        /// <returns>敌人ID数组</returns>
        public string[] GetRegisteredEnemyIds()
        {
            if (enemyPrefabMap == null)
                BuildPrefabMaps();
                
            var ids = new string[enemyPrefabMap.Count];
            enemyPrefabMap.Keys.CopyTo(ids, 0);
            return ids;
        }
        
        /// <summary>
        /// 获取所有注册的物品ID
        /// </summary>
        /// <returns>物品ID数组</returns>
        public string[] GetRegisteredItemIds()
        {
            if (itemPrefabMap == null)
                BuildPrefabMaps();
                
            var ids = new string[itemPrefabMap.Count];
            itemPrefabMap.Keys.CopyTo(ids, 0);
            return ids;
        }
    }
    
    /// <summary>
    /// 预制体条目 - 用于Inspector中配置ID到预制体的映射
    /// </summary>
    [System.Serializable]
    public class PrefabEntry
    {
        [Tooltip("预制体的唯一标识符")]
        public string id;
        
        [Tooltip("对应的预制体引用")]
        public GameObject prefab;
    }
}