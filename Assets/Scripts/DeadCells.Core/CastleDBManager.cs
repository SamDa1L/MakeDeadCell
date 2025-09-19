using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace DeadCells.Core
{
    public class CastleDBManager : MonoBehaviour
    {
        [Header("Database Settings")]
        [SerializeField] private TextAsset castleDBFile;
        [SerializeField] private bool loadOnStart = true;
        
        public static CastleDBManager Instance { get; private set; }
        
        // Data collections
        private Dictionary<string, WeaponData> weapons = new Dictionary<string, WeaponData>();
        private Dictionary<string, EnemyData> enemies = new Dictionary<string, EnemyData>();
        private Dictionary<string, ItemData> items = new Dictionary<string, ItemData>();
        private Dictionary<string, RoomData> rooms = new Dictionary<string, RoomData>();
        
        public Dictionary<string, WeaponData> Weapons => weapons;
        public Dictionary<string, EnemyData> Enemies => enemies;
        public Dictionary<string, ItemData> Items => items;
        public Dictionary<string, RoomData> Rooms => rooms;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            if (loadOnStart)
            {
                LoadDatabase();
            }
        }
        
        public void LoadDatabase()
        {
            if (castleDBFile == null)
            {
                Debug.LogError("CastleDB file not assigned!");
                return;
            }
            
            try
            {
                var database = JsonConvert.DeserializeObject<CastleDBRoot>(castleDBFile.text);
                ParseDatabase(database);
                Debug.Log("CastleDB loaded successfully!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load CastleDB: {e.Message}");
            }
        }
        
        private void ParseDatabase(CastleDBRoot database)
        {
            // Parse weapons
            if (database.sheets != null)
            {
                foreach (var sheet in database.sheets)
                {
                    switch (sheet.name)
                    {
                        case "weapons":
                            ParseWeapons(sheet);
                            break;
                        case "enemies":
                            ParseEnemies(sheet);
                            break;
                        case "items":
                            ParseItems(sheet);
                            break;
                        case "rooms":
                            ParseRooms(sheet);
                            break;
                    }
                }
            }
        }
        
        private void ParseWeapons(CastleDBSheet sheet)
        {
            weapons.Clear();
            if (sheet.lines == null) return;
            
            foreach (var line in sheet.lines)
            {
                var weapon = JsonConvert.DeserializeObject<WeaponData>(line.ToString());
                if (weapon != null && !string.IsNullOrEmpty(weapon.id))
                {
                    weapons[weapon.id] = weapon;
                }
            }
        }
        
        private void ParseEnemies(CastleDBSheet sheet)
        {
            enemies.Clear();
            if (sheet.lines == null) return;
            
            foreach (var line in sheet.lines)
            {
                var enemy = JsonConvert.DeserializeObject<EnemyData>(line.ToString());
                if (enemy != null && !string.IsNullOrEmpty(enemy.id))
                {
                    enemies[enemy.id] = enemy;
                }
            }
        }
        
        private void ParseItems(CastleDBSheet sheet)
        {
            items.Clear();
            if (sheet.lines == null) return;
            
            foreach (var line in sheet.lines)
            {
                var item = JsonConvert.DeserializeObject<ItemData>(line.ToString());
                if (item != null && !string.IsNullOrEmpty(item.id))
                {
                    items[item.id] = item;
                }
            }
        }
        
        private void ParseRooms(CastleDBSheet sheet)
        {
            rooms.Clear();
            if (sheet.lines == null) return;
            
            foreach (var line in sheet.lines)
            {
                var room = JsonConvert.DeserializeObject<RoomData>(line.ToString());
                if (room != null && !string.IsNullOrEmpty(room.id))
                {
                    rooms[room.id] = room;
                }
            }
        }
        
        // Getter methods
        public WeaponData GetWeapon(string id) => weapons.TryGetValue(id, out var weapon) ? weapon : null;
        public EnemyData GetEnemy(string id) => enemies.TryGetValue(id, out var enemy) ? enemy : null;
        public ItemData GetItem(string id) => items.TryGetValue(id, out var item) ? item : null;
        public RoomData GetRoom(string id) => rooms.TryGetValue(id, out var room) ? room : null;
    }
    
    // CastleDB JSON structure classes
    [System.Serializable]
    public class CastleDBRoot
    {
        public CastleDBSheet[] sheets;
    }
    
    [System.Serializable]
    public class CastleDBSheet
    {
        public string name;
        public object[] lines;
    }
}