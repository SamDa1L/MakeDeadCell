using UnityEngine;
using System.Collections.Generic;
using DeadCells.Core;
using DeadCells.Player;
using DeadCells.Combat;
using DeadCells.Data;
using LDtkUnity;

namespace DeadCells.Rooms
{
    /// <summary>
    /// LDtk数据驱动房间管理器
    /// 负责从LDtk项目加载房间和实体
    /// 目前使用临时fallback方法，需要安装LDtk Unity包后启用完整功能
    /// </summary>
    public class LDtkRoomManager : MonoBehaviour
    {
        [Header("LDtk Integration")]
        [SerializeField] private LDtkComponentProject ldtkProject;
        
        [Header("Room Settings")]
        [SerializeField] private Room currentRoom;
        [SerializeField] private string startingLevelId = "Level_0";
        
        public Room CurrentRoom => currentRoom;
        public static LDtkRoomManager Instance { get; private set; }
        
        private Dictionary<string, RoomData> roomDataMap = new Dictionary<string, RoomData>();
        
        public delegate void RoomChangedDelegate(Room newRoom, Room previousRoom);
        public event RoomChangedDelegate OnRoomChanged;
        
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
        
        private void Start()
        {
            InitializeLDtkIntegration();
        }
        
        private void InitializeLDtkIntegration()
        {
            if (ldtkProject == null)
                ldtkProject = FindObjectOfType<LDtkComponentProject>();
            
            LoadRoomData();
            
            if (ldtkProject != null)
            {
                Debug.Log("LDtk Unity package detected. Using LDtk integration for room loading.");
                LoadStartingRoom();
            }
            else
            {
                Debug.LogWarning("LDtkComponentProject not found! Using fallback room loading. " +
                               "Please add an LDtk project to the scene or assign it to ldtkProject field.");
                LoadStartingRoom();
            }
        }
        
        private void LoadRoomData()
        {
            // Get room data from CastleDB
            if (CastleDBManager.Instance?.RoomJsonData != null)
            {
                var roomJsonData = CastleDBManager.Instance.RoomJsonData;
                foreach (var kvp in roomJsonData)
                {
                    var roomData = CastleDBManager.Instance.DeserializeData<RoomData>(kvp.Value);
                    if (roomData != null)
                        roomDataMap[kvp.Key] = roomData;
                }
            }
        }
        
        /// <summary>
        /// 从CastleDBManager获取敌人数据的适配器方法
        /// </summary>
        private EnemyData GetEnemyData(string enemyId)
        {
            var jsonData = CastleDBManager.Instance?.GetRawJsonData("enemy", enemyId);
            if (string.IsNullOrEmpty(jsonData))
                return null;
                
            return CastleDBManager.Instance.DeserializeData<EnemyData>(jsonData);
        }
        
        private void LoadStartingRoom()
        {
            LoadRoom(startingLevelId);
        }
        
        public bool LoadRoom(string levelId)
        {
            Room roomToLoad = null;
            
            if (ldtkProject != null)
            {
                // Use LDtk integration
                LDtkComponentLevel levelComponent = FindLevelComponent(levelId);
                if (levelComponent != null)
                {
                    Debug.Log($"Loading room {levelId} using LDtk integration");
                    roomToLoad = CreateRoomFromLDtk(levelId, levelComponent);
                    
                    // Position player at spawn point
                    PositionPlayerAtSpawn(levelComponent);
                }
                else
                {
                    Debug.LogWarning($"LDtk level not found: {levelId}. Using fallback method.");
                }
            }
            
            // Use fallback method if LDtk loading failed
            if (roomToLoad == null)
            {
                Debug.LogWarning($"Loading room {levelId} with fallback method (LDtk not available)");
                roomToLoad = CreateBasicRoom(levelId);
            }
            
            SetCurrentRoom(roomToLoad);
            return true;
        }
        
        private Room CreateBasicRoom(string levelId)
        {
            // Find existing room or create new one
            Room room = FindObjectOfType<Room>();
            if (room == null)
            {
                GameObject roomObject = new GameObject($"Room_{levelId}");
                room = roomObject.AddComponent<Room>();
            }
            
            // Configure room with data from CastleDB if available
            if (roomDataMap.TryGetValue(levelId, out RoomData roomData))
            {
                ConfigureRoomFromData(room, roomData);
            }
            else
            {
                // Use default configuration
                room.Initialize(Vector2.zero, RoomType.Combat);
            }
            
            // Set the level ID for reference
            room.name = $"Room_{levelId}";
            
            return room;
        }
        
        private void ConfigureRoomFromData(Room room, RoomData roomData)
        {
            // Parse room type from string
            RoomType roomType = ParseRoomType(roomData.roomType);
            room.Initialize(Vector2.zero, roomType);
            
            // Spawn enemies based on room data
            if (roomData.enemySpawns != null)
            {
                SpawnEnemiesFromData(roomData.enemySpawns);
            }
        }
        
        private RoomType ParseRoomType(string roomTypeString)
        {
            switch (roomTypeString.ToLower())
            {
                case "combat": return RoomType.Combat;
                case "treasure": return RoomType.Treasure;
                case "shop": return RoomType.Shop;
                case "boss": return RoomType.Boss;
                case "challenge": return RoomType.Challenge;
                default: return RoomType.Combat;
            }
        }
        
        private void SpawnEnemiesFromData(EnemySpawnData[] enemySpawns)
        {
            foreach (var spawnData in enemySpawns)
            {
                if (Random.value <= spawnData.spawnChance)
                {
                    int spawnCount = Random.Range(spawnData.minCount, spawnData.maxCount + 1);
                    
                    for (int i = 0; i < spawnCount; i++)
                    {
                        SpawnEnemy(spawnData.enemyId);
                    }
                }
            }
        }
        
        private void SpawnEnemy(string enemyId)
        {
            // Get enemy data from CastleDB
            var enemyData = GetEnemyData(enemyId);
            if (enemyData == null)
            {
                Debug.LogWarning($"Enemy data not found for ID: {enemyId}");
                return;
            }
            
            // Load enemy prefab
            GameObject enemyPrefab = LoadEnemyPrefab(enemyData);
            if (enemyPrefab == null)
            {
                Debug.LogWarning($"Enemy prefab not found for: {enemyId}");
                return;
            }
            
            // Find spawn position (using random position for fallback)
            Vector3 spawnPosition = GetEnemySpawnPosition();
            
            // Instantiate and configure enemy
            GameObject enemyInstance = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            ConfigureEnemyFromData(enemyInstance, enemyData);
        }
        
        private GameObject LoadEnemyPrefab(EnemyData enemyData)
        {
            if (!string.IsNullOrEmpty(enemyData.prefabPath))
            {
                return Resources.Load<GameObject>(enemyData.prefabPath);
            }
            
            // Fallback to generic enemy prefab
            return Resources.Load<GameObject>("Enemies/DefaultEnemy");
        }
        
        private Vector3 GetEnemySpawnPosition()
        {
            // Use random position for fallback (will be replaced with LDtk spawn points)
            return new Vector3(Random.Range(-8f, 8f), Random.Range(-4f, 4f), 0);
        }
        
        private void ConfigureEnemyFromData(GameObject enemyInstance, EnemyData enemyData)
        {
            // Configure health
            var health = enemyInstance.GetComponent<Combat.Health>();
            if (health != null)
            {
                health.SetMaxHealth(enemyData.maxHealth);
            }
            
            // Configure enemy controller
            var enemyController = enemyInstance.GetComponent<Combat.EnemyController>();
            if (enemyController != null)
            {
                // Apply enemy data properties
                // These would need to be exposed as public properties in EnemyController
            }
            
            enemyInstance.name = enemyData.name;
        }
        
        private void SetCurrentRoom(Room newRoom)
        {
            Room previousRoom = currentRoom;
            currentRoom = newRoom;
            
            if (newRoom != null)
                newRoom.OnEnterRoom();
                
            if (previousRoom != null)
                previousRoom.OnExitRoom();
                
            OnRoomChanged?.Invoke(newRoom, previousRoom);
        }
        
        public void TransitionToRoom(string levelId)
        {
            LoadRoom(levelId);
        }
        
        public RoomData GetRoomData(string roomId)
        {
            roomDataMap.TryGetValue(roomId, out RoomData data);
            return data;
        }
        
        private void PositionPlayerAtSpawn(LDtkComponentLevel levelComponent)
        {
            Vector3 spawnPosition = GetPlayerSpawnPosition(levelComponent);
            
            var playerController = FindObjectOfType<Player.PlayerController>();
            if (playerController != null)
            {
                playerController.transform.position = spawnPosition;
            }
        }
        
        private Vector3 GetPlayerSpawnPosition(LDtkComponentLevel levelComponent)
        {
            // Look for player spawn entity in the level by name
            var entities = levelComponent.GetComponentsInChildren<LDtkComponentEntity>();
            foreach (var entity in entities)
            {
                // Try to identify player spawn by GameObject name (common LDtk pattern)
                if (entity.name.ToLower().Contains("player_spawn") || 
                    entity.name.ToLower().Contains("playerspawn") ||
                    entity.name.ToLower().Contains("spawn"))
                {
                    return entity.transform.position;
                }
            }
            
            // Fallback to center of level if no spawn point found
            return Vector3.zero;
        }
        
        #region LDtk Integration Methods
        
        private LDtkComponentLevel FindLevelComponent(string levelId)
        {
            // Search through all level components in the project
            LDtkComponentLevel[] levels = ldtkProject.GetComponentsInChildren<LDtkComponentLevel>();
            foreach (var level in levels)
            {
                if (level.Identifier == levelId)
                {
                    return level;
                }
            }
            return null;
        }
        
        private Room CreateRoomFromLDtk(string levelId, LDtkComponentLevel levelComponent)
        {
            // Find existing room or create new one
            Room room = FindObjectOfType<Room>();
            if (room == null)
            {
                GameObject roomObject = new GameObject($"Room_{levelId}");
                room = roomObject.AddComponent<Room>();
            }
            
            // Configure room with data from CastleDB if available
            if (roomDataMap.TryGetValue(levelId, out RoomData roomData))
            {
                ConfigureRoomFromData(room, roomData);
            }
            else
            {
                // Use default configuration
                room.Initialize(Vector2.zero, RoomType.Combat);
            }
            
            // Set the level ID for reference
            room.name = $"Room_{levelId}";
            
            // Spawn enemies from LDtk entities
            SpawnEntitiesFromLevel(levelComponent);
            
            return room;
        }
        
        private void SpawnEntitiesFromLevel(LDtkComponentLevel levelComponent)
        {
            // Find entity layers and spawn entities
            var entityLayers = levelComponent.GetComponentsInChildren<LDtkComponentLayer>();
            foreach (var layer in entityLayers)
            {
                var entities = layer.GetComponentsInChildren<LDtkComponentEntity>();
                foreach (var entity in entities)
                {
                    SpawnEntityFromLDtk(entity);
                }
            }
        }
        
        private void SpawnEntityFromLDtk(LDtkComponentEntity entity)
        {
            // Handle different entity types by GameObject name
            string entityName = entity.name.ToLower();
            
            if (entityName.Contains("player_spawn") || entityName.Contains("playerspawn"))
            {
                // Mark as player spawn point, don't instantiate
            }
            else if (entityName.Contains("enemy"))
            {
                SpawnEnemyEntity(entity);
            }
            else if (entityName.Contains("treasure") || entityName.Contains("item"))
            {
                SpawnTreasureEntity(entity);
            }
            else
            {
                Debug.LogWarning($"Unknown entity type: {entity.name}");
            }
        }
        
        private void SpawnEnemyEntity(LDtkComponentEntity entity)
        {
            // Get enemy type from entity fields
            string enemyType = GetEntityFieldValue(entity, "EnemyType", "default");
            
            // Get enemy data from CastleDB
            var enemyData = GetEnemyData(enemyType);
            if (enemyData == null)
            {
                Debug.LogWarning($"Enemy data not found for type: {enemyType}");
                return;
            }
            
            // Load and instantiate enemy prefab
            GameObject enemyPrefab = LoadEnemyPrefab(enemyData);
            if (enemyPrefab != null)
            {
                Vector3 worldPos = entity.transform.position;
                GameObject enemyInstance = Instantiate(enemyPrefab, worldPos, Quaternion.identity);
                ConfigureEnemyFromData(enemyInstance, enemyData);
            }
        }
        
        private void SpawnTreasureEntity(LDtkComponentEntity entity)
        {
            // Get treasure type from entity fields
            string treasureType = GetEntityFieldValue(entity, "TreasureType", "coin");
            
            // Load and instantiate treasure prefab
            GameObject treasurePrefab = Resources.Load<GameObject>($"Items/{treasureType}");
            if (treasurePrefab != null)
            {
                Vector3 worldPos = entity.transform.position;
                Instantiate(treasurePrefab, worldPos, Quaternion.identity);
            }
        }
        
        private string GetEntityFieldValue(LDtkComponentEntity entity, string fieldName, string defaultValue)
        {
            // Try to get field value from LDtk entity fields
            var fields = entity.GetComponent<LDtkFields>();
            if (fields != null)
            {
                // Try to get the field value as string
                try
                {
                    var fieldValue = fields.GetString(fieldName);
                    return !string.IsNullOrEmpty(fieldValue) ? fieldValue : defaultValue;
                }
                catch
                {
                    // Field not found or wrong type, try fallback methods
                }
            }
            
            // Fallback: try to extract value from GameObject name or tags
            if (fieldName.ToLower() == "enemytype")
            {
                string name = entity.name.ToLower();
                if (name.Contains("zombie")) return "zombie";
                if (name.Contains("skeleton")) return "skeleton"; 
                if (name.Contains("orc")) return "orc";
                // Add more enemy types as needed
            }
            
            if (fieldName.ToLower() == "treasuretype")
            {
                string name = entity.name.ToLower();
                if (name.Contains("gold") || name.Contains("coin")) return "coin";
                if (name.Contains("gem") || name.Contains("diamond")) return "gem";
                if (name.Contains("chest")) return "chest";
                // Add more treasure types as needed
            }
            
            return defaultValue;
        }
        #endregion
    }
}