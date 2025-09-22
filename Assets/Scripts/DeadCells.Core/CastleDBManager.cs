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
        
        // 数据加载状态
        public bool IsDataLoaded { get; private set; } = false;
        public bool HasValidDatabase => castleDBFile != null;
        
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
            IsDataLoaded = false;
            
            if (castleDBFile == null)
            {
                Debug.LogError("CastleDB file not assigned! Data-driven systems will not work.");
                return;
            }
            
            try
            {
                string jsonText = castleDBFile.text;
                if (string.IsNullOrEmpty(jsonText))
                {
                    Debug.LogError("CastleDB file is empty!");
                    return;
                }
                
                var database = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonText);
                if (database == null)
                {
                    Debug.LogError("Failed to parse CastleDB JSON!");
                    return;
                }
                
                // 加载各类数据
                LoadWeaponsData(database);
                LoadEnemiesData(database);
                LoadItemsData(database);
                LoadRoomsData(database);
                
                IsDataLoaded = true;
                int totalItems = weaponJsonData.Count + enemyJsonData.Count + itemJsonData.Count + roomJsonData.Count;
                
                if (totalItems == 0)
                {
                    Debug.LogWarning("CastleDB loaded but no data found! Check table names and structure.");
                }
                else
                {
                    Debug.Log($"CastleDB loaded successfully: {weaponJsonData.Count} weapons, {enemyJsonData.Count} enemies, {itemJsonData.Count} items, {roomJsonData.Count} rooms");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load CastleDB: {e.Message}");
                IsDataLoaded = false;
            }
        }
        
        /// <summary>
        /// 获取原始JSON数据（通用方法）
        /// </summary>
        public string GetRawJsonData(string category, string id)
        {
            if (!IsDataLoaded)
            {
                Debug.LogWarning($"CastleDB data not loaded! Cannot get {category} data for id: {id}");
                return null;
            }
            
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"Invalid id provided for category: {category}");
                return null;
            }
            
            switch (category.ToLower())
            {
                case "weapon":
                    if (!weaponJsonData.TryGetValue(id, out var weapon))
                    {
                        Debug.LogWarning($"Weapon with id '{id}' not found in CastleDB");
                        return null;
                    }
                    return weapon;
                case "enemy":
                    if (!enemyJsonData.TryGetValue(id, out var enemy))
                    {
                        Debug.LogWarning($"Enemy with id '{id}' not found in CastleDB");
                        return null;
                    }
                    return enemy;
                case "item":
                    if (!itemJsonData.TryGetValue(id, out var item))
                    {
                        Debug.LogWarning($"Item with id '{id}' not found in CastleDB");
                        return null;
                    }
                    return item;
                case "room":
                    if (!roomJsonData.TryGetValue(id, out var room))
                    {
                        Debug.LogWarning($"Room with id '{id}' not found in CastleDB");
                        return null;
                    }
                    return room;
                default:
                    Debug.LogWarning($"Unknown category: {category}");
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
            weaponJsonData.Clear();
            
            if (database.TryGetValue("weapons", out var weaponsObj) && weaponsObj is Newtonsoft.Json.Linq.JArray weaponsArray)
            {
                foreach (var weaponItem in weaponsArray)
                {
                    var weaponJson = weaponItem.ToString();
                    var weaponDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(weaponJson);
                    
                    if (weaponDict != null && weaponDict.TryGetValue("id", out var idObj))
                    {
                        string id = idObj.ToString();
                        weaponJsonData[id] = weaponJson;
                    }
                }
                Debug.Log($"Loaded {weaponJsonData.Count} weapons from CastleDB");
            }
            else
            {
                Debug.LogWarning("No 'weapons' table found in CastleDB or invalid format");
            }
        }
        
        private void LoadEnemiesData(Dictionary<string, object> database)
        {
            enemyJsonData.Clear();
            
            if (database.TryGetValue("enemies", out var enemiesObj) && enemiesObj is Newtonsoft.Json.Linq.JArray enemiesArray)
            {
                foreach (var enemyItem in enemiesArray)
                {
                    var enemyJson = enemyItem.ToString();
                    var enemyDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(enemyJson);
                    
                    if (enemyDict != null && enemyDict.TryGetValue("id", out var idObj))
                    {
                        string id = idObj.ToString();
                        enemyJsonData[id] = enemyJson;
                    }
                }
                Debug.Log($"Loaded {enemyJsonData.Count} enemies from CastleDB");
            }
            else
            {
                Debug.LogWarning("No 'enemies' table found in CastleDB or invalid format");
            }
        }
        
        private void LoadItemsData(Dictionary<string, object> database)
        {
            itemJsonData.Clear();
            
            if (database.TryGetValue("items", out var itemsObj) && itemsObj is Newtonsoft.Json.Linq.JArray itemsArray)
            {
                foreach (var itemItem in itemsArray)
                {
                    var itemJson = itemItem.ToString();
                    var itemDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(itemJson);
                    
                    if (itemDict != null && itemDict.TryGetValue("id", out var idObj))
                    {
                        string id = idObj.ToString();
                        itemJsonData[id] = itemJson;
                    }
                }
                Debug.Log($"Loaded {itemJsonData.Count} items from CastleDB");
            }
            else
            {
                Debug.LogWarning("No 'items' table found in CastleDB or invalid format");
            }
        }
        
        private void LoadRoomsData(Dictionary<string, object> database)
        {
            roomJsonData.Clear();
            
            if (database.TryGetValue("rooms", out var roomsObj) && roomsObj is Newtonsoft.Json.Linq.JArray roomsArray)
            {
                foreach (var roomItem in roomsArray)
                {
                    var roomJson = roomItem.ToString();
                    var roomDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(roomJson);
                    
                    if (roomDict != null && roomDict.TryGetValue("id", out var idObj))
                    {
                        string id = idObj.ToString();
                        roomJsonData[id] = roomJson;
                    }
                }
                Debug.Log($"Loaded {roomJsonData.Count} rooms from CastleDB");
            }
            else
            {
                Debug.LogWarning("No 'rooms' table found in CastleDB or invalid format");
            }
        }
    }
}
