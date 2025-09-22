using UnityEngine;
using System.Collections.Generic;
using DeadCells.Core;
using DeadCells.Player;
using DeadCells.Combat;
using DeadCells.Data;

namespace DeadCells.Game
{
    public class WeaponFactory : SystemComponent
    {
        [Header("Weapon Prefabs")]
        [SerializeField] private GameObject meleeWeaponPrefab;
        [SerializeField] private GameObject rangedWeaponPrefab;
        [SerializeField] private Transform weaponParent;
        
        public static WeaponFactory Instance { get; private set; }
        
        protected override void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                base.Awake(); // 调用 SystemComponent 的 Awake 进行自动注册
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        protected override void OnDestroy()
        {
            // 清空静态引用，防止悬空引用
            if (Instance == this)
            {
                Instance = null;
            }
            
            base.OnDestroy(); // 调用 SystemComponent 的 OnDestroy 进行注销
        }
        
        /// <summary>
        /// 安全获取WeaponFactory实例，自动处理空引用
        /// </summary>
        public static WeaponFactory GetInstance()
        {
            if (Instance == null || Instance.gameObject == null)
            {
                Instance = FindObjectOfType<WeaponFactory>();
            }
            return Instance;
        }
        
        public GameObject CreateWeapon(string weaponId, Transform parent = null)
        {
            var weaponData = GetWeaponData(weaponId);
            if (weaponData == null)
            {
                Debug.LogError($"Weapon data not found for ID: {weaponId}");
                return null;
            }
            
            return CreateWeapon(weaponData, parent);
        }
        
        /// <summary>
        /// 从CastleDBManager获取武器数据的适配器方法
        /// </summary>
        private WeaponData GetWeaponData(string weaponId)
        {
            var castleDB = CastleDBManager.Instance;
            if (castleDB == null || castleDB.gameObject == null)
            {
                Debug.LogError("CastleDBManager not available! Cannot load weapon data.");
                return null;
            }
            
            if (!castleDB.IsDataLoaded)
            {
                Debug.LogError("CastleDB data not loaded! Make sure CastleDB file is assigned and loaded.");
                return null;
            }
                
            var jsonData = castleDB.GetRawJsonData("weapon", weaponId);
            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.LogWarning($"Weapon data not found for id: {weaponId}. Check if id exists in CastleDB.");
                return null;
            }
                
            return castleDB.DeserializeData<WeaponData>(jsonData);
        }
        
        public GameObject CreateWeapon(WeaponData weaponData, Transform parent = null)
        {
            if (parent == null)
                parent = weaponParent;
            
            GameObject weaponPrefab = GetWeaponPrefab(weaponData.weaponType);
            if (weaponPrefab == null)
            {
                Debug.LogError($"WeaponFactory: No prefab found for weapon type: {weaponData.weaponType}");
                return null;
            }
            
            GameObject weaponInstance = Instantiate(weaponPrefab, parent);
            
            // Configure the weapon using the new system
            ConfigureWeapon(weaponInstance, weaponData);
            
            // Validate configuration
            bool isValid = ValidateWeaponConfiguration(weaponInstance, weaponData.weaponType);
            if (!isValid)
            {
                Debug.LogWarning($"WeaponFactory: Weapon '{weaponData.name}' was created but may not be fully functional");
            }
            
            return weaponInstance;
        }
        
        private GameObject GetWeaponPrefab(string weaponType)
        {
            switch (weaponType.ToLower())
            {
                case "melee":
                    return meleeWeaponPrefab;
                case "ranged":
                    return rangedWeaponPrefab;
                default:
                    return meleeWeaponPrefab; // Default to melee
            }
        }
        
        private void ConfigureWeapon(GameObject weaponInstance, WeaponData data)
        {
            // Configure base weapon properties using DataDrivenWeapon
            ConfigureBaseWeapon(weaponInstance, data);
            
            // Use the new strongly-typed configuration system
            var configResults = WeaponConfigurationManager.ConfigureWeapon(weaponInstance, data);
            
            // Log configuration results
            LogConfigurationResults(configResults, data.name);
            
            // Set name and description
            weaponInstance.name = data.name;
        }
        
        private void ConfigureBaseWeapon(GameObject weaponInstance, WeaponData data)
        {
            // Add DataDrivenWeapon component to provide weapon data
            var dataWeapon = weaponInstance.GetComponent<DataDrivenWeapon>();
            if (dataWeapon == null)
            {
                dataWeapon = weaponInstance.AddComponent<DataDrivenWeapon>();
            }
            
            dataWeapon.Initialize(data);
        }
        
        /// <summary>
        /// 记录武器配置结果
        /// </summary>
        /// <param name="results">配置结果列表</param>
        /// <param name="weaponName">武器名称</param>
        private void LogConfigurationResults(List<WeaponConfigurationResult> results, string weaponName)
        {
            if (results == null || results.Count == 0)
            {
                Debug.LogWarning($"WeaponFactory: No configuration results for weapon '{weaponName}'");
                return;
            }
            
            int successCount = 0;
            int failureCount = 0;
            
            foreach (var result in results)
            {
                if (result.Success)
                {
                    successCount++;
                    Debug.Log($"WeaponFactory: Successfully configured {result.ComponentName} for weapon '{weaponName}'");
                }
                else
                {
                    failureCount++;
                    Debug.LogWarning($"WeaponFactory: Failed to configure {result.ComponentName} for weapon '{weaponName}': {result.ErrorMessage}");
                }
            }
            
            if (failureCount > 0)
            {
                Debug.LogError($"WeaponFactory: Weapon '{weaponName}' configuration completed with {failureCount} failures and {successCount} successes");
            }
            else
            {
                Debug.Log($"WeaponFactory: Weapon '{weaponName}' configured successfully with {successCount} components");
            }
        }
        
        /// <summary>
        /// 验证武器配置完整性
        /// </summary>
        /// <param name="weaponInstance">武器实例</param>
        /// <param name="expectedType">预期武器类型</param>
        /// <returns>验证是否通过</returns>
        private bool ValidateWeaponConfiguration(GameObject weaponInstance, string expectedType)
        {
            if (weaponInstance == null)
            {
                Debug.LogError("WeaponFactory: Cannot validate null weapon instance");
                return false;
            }
            
            bool isConfigured = WeaponConfigurationManager.IsWeaponProperlyConfigured(weaponInstance, expectedType);
            
            if (!isConfigured)
            {
                Debug.LogError($"WeaponFactory: Weapon '{weaponInstance.name}' is not properly configured for type '{expectedType}'");
            }
            
            return isConfigured;
        }
        
        public WeaponData[] GetAllWeapons()
        {
            var castleDB = CastleDBManager.Instance;
            if (castleDB == null || castleDB.gameObject == null)
            {
                Debug.LogError("CastleDBManager not available!");
                return new WeaponData[0];
            }
            
            if (!castleDB.IsDataLoaded)
            {
                Debug.LogError("CastleDB data not loaded!");
                return new WeaponData[0];
            }
            
            var weaponJsonData = castleDB.WeaponJsonData;
            if (weaponJsonData == null || weaponJsonData.Count == 0)
            {
                Debug.LogWarning("No weapon data available in CastleDB");
                return new WeaponData[0];
            }
            
            var weaponList = new System.Collections.Generic.List<WeaponData>();
            
            foreach (var kvp in weaponJsonData)
            {
                var weaponData = castleDB.DeserializeData<WeaponData>(kvp.Value);
                if (weaponData != null)
                    weaponList.Add(weaponData);
            }
            
            return weaponList.ToArray();
        }
        
        public WeaponData[] GetWeaponsByType(string weaponType)
        {
            var allWeapons = GetAllWeapons();
            return System.Array.FindAll(allWeapons, w => w.weaponType.Equals(weaponType, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}