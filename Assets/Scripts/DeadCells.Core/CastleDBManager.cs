using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace DeadCells.Core
{
    /// <summary>
    /// 基础数据管理器 - 只处理原始JSON数据，不依赖具体数据类型
    /// </summary>
    public class CastleDBManager : MonoBehaviour
    {
        [Header("Database Settings")]
        [SerializeField] private TextAsset castleDBFile;
        [SerializeField] private bool loadOnStart = true;
        
        public static CastleDBManager Instance { get; private set; }
        
        // 原始JSON数据集合
        private Dictionary<string, string> weaponJsonData = new Dictionary<string, string>();
        private Dictionary<string, string> enemyJsonData = new Dictionary<string, string>();
        private Dictionary<string, string> itemJsonData = new Dictionary<string, string>();
        private Dictionary<string, string> roomJsonData = new Dictionary<string, string>();
        
        // 公开访问接口
        public Dictionary<string, string> WeaponJsonData => weaponJsonData;
        public Dictionary<string, string> EnemyJsonData => enemyJsonData;
        public Dictionary<string, string> ItemJsonData => itemJsonData;
        public Dictionary<string, string> RoomJsonData => roomJsonData;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                if (loadOnStart)
                    LoadDatabase();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 加载数据库
        /// </summary>
        public void LoadDatabase()
        {
            if (castleDBFile == null)
            {
                Debug.LogWarning("CastleDB file not assigned!");
                return;
            }
            
            try
            {
                string jsonText = castleDBFile.text;
                var database = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonText);
                
                // 加载各类数据
                LoadWeaponsData(database);
                LoadEnemiesData(database);
                LoadItemsData(database);
                LoadRoomsData(database);
                
                Debug.Log($"CastleDB loaded: {weaponJsonData.Count} weapons, {enemyJsonData.Count} enemies, {itemJsonData.Count} items, {roomJsonData.Count} rooms");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load CastleDB: {e.Message}");
            }
        }
        
        /// <summary>
        /// 获取原始JSON数据（通用方法）
        /// </summary>
        public string GetRawJsonData(string category, string id)
        {
            switch (category.ToLower())
            {
                case "weapon":
                    return weaponJsonData.TryGetValue(id, out var weapon) ? weapon : null;
                case "enemy":
                    return enemyJsonData.TryGetValue(id, out var enemy) ? enemy : null;
                case "item":
                    return itemJsonData.TryGetValue(id, out var item) ? item : null;
                case "room":
                    return roomJsonData.TryGetValue(id, out var room) ? room : null;
                default:
                    return null;
            }
        }
        
        /// <summary>
        /// 将JSON字符串转换为指定类型（由上层模块调用）
        /// </summary>
        public T DeserializeData<T>(string jsonData) where T : class
        {
            if (string.IsNullOrEmpty(jsonData))
                return null;
                
            try
            {
                return JsonConvert.DeserializeObject<T>(jsonData);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to deserialize data: {e.Message}");
                return null;
            }
        }
        
        private void LoadWeaponsData(Dictionary<string, object> database)
        {
            // 实现武器数据加载的逻辑，存储为JSON字符串
            // 具体实现省略，存储原始JSON数据
        }
        
        private void LoadEnemiesData(Dictionary<string, object> database)
        {
            // 实现敌人数据加载的逻辑，存储为JSON字符串
        }
        
        private void LoadItemsData(Dictionary<string, object> database)
        {
            // 实现道具数据加载的逻辑，存储为JSON字符串
        }
        
        private void LoadRoomsData(Dictionary<string, object> database)
        {
            // 实现房间数据加载的逻辑，存储为JSON字符串
        }
    }
}