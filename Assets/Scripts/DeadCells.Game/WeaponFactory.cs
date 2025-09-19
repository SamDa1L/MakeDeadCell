using UnityEngine;
using DeadCells.Core;
using DeadCells.Player;
using DeadCells.Combat;
using DeadCells.Data;

namespace DeadCells.Game
{
    public class WeaponFactory : MonoBehaviour
    {
        [Header("Weapon Prefabs")]
        [SerializeField] private GameObject meleeWeaponPrefab;
        [SerializeField] private GameObject rangedWeaponPrefab;
        [SerializeField] private Transform weaponParent;
        
        public static WeaponFactory Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
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
            var jsonData = CastleDBManager.Instance?.GetRawJsonData("weapon", weaponId);
            if (string.IsNullOrEmpty(jsonData))
                return null;
                
            return CastleDBManager.Instance.DeserializeData<WeaponData>(jsonData);
        }
        
        public GameObject CreateWeapon(WeaponData weaponData, Transform parent = null)
        {
            if (parent == null)
                parent = weaponParent;
            
            GameObject weaponPrefab = GetWeaponPrefab(weaponData.weaponType);
            if (weaponPrefab == null)
            {
                Debug.LogError($"No prefab found for weapon type: {weaponData.weaponType}");
                return null;
            }
            
            GameObject weaponInstance = Instantiate(weaponPrefab, parent);
            ConfigureWeapon(weaponInstance, weaponData);
            
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
            
            // Configure specific weapon type
            if (data.weaponType.ToLower() == "melee")
            {
                ConfigureMeleeWeapon(weaponInstance, data);
            }
            else if (data.weaponType.ToLower() == "ranged")
            {
                ConfigureRangedWeapon(weaponInstance, data);
            }
            
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
        
        private void ConfigureMeleeWeapon(GameObject weaponInstance, WeaponData data)
        {
            // Use message-based approach to configure melee weapon
            if (data.stats != null)
            {
                weaponInstance.SendMessage("ConfigureMeleeStats", data.stats, SendMessageOptions.DontRequireReceiver);
            }
        }
        
        private void ConfigureRangedWeapon(GameObject weaponInstance, WeaponData data)
        {
            // Use message-based approach to configure ranged weapon
            if (data.stats != null)
            {
                weaponInstance.SendMessage("ConfigureRangedStats", data.stats, SendMessageOptions.DontRequireReceiver);
            }
        }
        
        public WeaponData[] GetAllWeapons()
        {
            if (CastleDBManager.Instance?.WeaponJsonData == null)
                return new WeaponData[0];
            
            var weaponJsonData = CastleDBManager.Instance.WeaponJsonData;
            var weaponList = new System.Collections.Generic.List<WeaponData>();
            
            foreach (var kvp in weaponJsonData)
            {
                var weaponData = CastleDBManager.Instance.DeserializeData<WeaponData>(kvp.Value);
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