using UnityEngine;
using DeadCellsTestFramework.Weapons;
using DeadCellsTestFramework.Combat;

namespace DeadCellsTestFramework.Data
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
            var weaponData = CastleDBManager.Instance?.GetWeapon(weaponId);
            if (weaponData == null)
            {
                Debug.LogError($"Weapon data not found for ID: {weaponId}");
                return null;
            }
            
            return CreateWeapon(weaponData, parent);
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
            // Configure base weapon properties
            Weapon weapon = weaponInstance.GetComponent<Weapon>();
            if (weapon != null)
            {
                // Use reflection or create a data-driven weapon component
                ConfigureBaseWeapon(weapon, data);
            }
            
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
        
        private void ConfigureBaseWeapon(Weapon weapon, WeaponData data)
        {
            // Since we can't directly modify private fields, we'll create a new data-driven weapon component
            var dataWeapon = weapon.GetComponent<DataDrivenWeapon>();
            if (dataWeapon == null)
            {
                dataWeapon = weapon.gameObject.AddComponent<DataDrivenWeapon>();
            }
            
            dataWeapon.Initialize(data);
        }
        
        private void ConfigureMeleeWeapon(GameObject weaponInstance, WeaponData data)
        {
            MeleeWeapon melee = weaponInstance.GetComponent<MeleeWeapon>();
            if (melee != null && data.stats != null)
            {
                // Configure melee-specific properties
                // These would need to be exposed as public properties in MeleeWeapon
            }
        }
        
        private void ConfigureRangedWeapon(GameObject weaponInstance, WeaponData data)
        {
            RangedWeapon ranged = weaponInstance.GetComponent<RangedWeapon>();
            if (ranged != null && data.stats != null)
            {
                // Configure ranged-specific properties
                // These would need to be exposed as public properties in RangedWeapon
            }
        }
        
        public WeaponData[] GetAllWeapons()
        {
            if (CastleDBManager.Instance?.Weapons == null)
                return new WeaponData[0];
            
            var weapons = CastleDBManager.Instance.Weapons.Values;
            var weaponArray = new WeaponData[weapons.Count];
            weapons.CopyTo(weaponArray, 0);
            return weaponArray;
        }
        
        public WeaponData[] GetWeaponsByType(string weaponType)
        {
            var allWeapons = GetAllWeapons();
            return System.Array.FindAll(allWeapons, w => w.weaponType.Equals(weaponType, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}