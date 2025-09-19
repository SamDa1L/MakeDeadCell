using UnityEngine;
using System.Collections.Generic;
using DeadCells.Player;
using DeadCells.Combat;

namespace DeadCells.Rooms
{
    public enum RoomType
    {
        Start,
        Combat,
        Treasure,
        Shop,
        Challenge,
        Boss
    }
    
    public class Room : MonoBehaviour
    {
        [Header("Room Properties")]
        [SerializeField] private RoomType roomType;
        [SerializeField] private Vector2 gridPosition;
        [SerializeField] private bool isCleared = false;
        
        [Header("Room Connections")]
        [SerializeField] private Transform[] doorPositions = new Transform[4]; // Up, Right, Down, Left
        [SerializeField] private GameObject doorPrefab;
        
        [Header("Enemies")]
        [SerializeField] private Transform[] enemySpawnPoints;
        [SerializeField] private GameObject[] enemyPrefabs;
        
        private Dictionary<Vector2, Room> connections = new Dictionary<Vector2, Room>();
        private List<GameObject> activeDoors = new List<GameObject>();
        private List<GameObject> spawnedEnemies = new List<GameObject>();
        
        public RoomType RoomType => roomType;
        public Vector2 GridPosition => gridPosition;
        public bool IsCleared => isCleared;
        
        public void Initialize(Vector2 gridPos, RoomType type)
        {
            gridPosition = gridPos;
            roomType = type;
            gameObject.name = $"Room_{type}_{gridPos.x}_{gridPos.y}";
        }
        
        public void AddConnection(Vector2 direction, Room connectedRoom)
        {
            connections[direction] = connectedRoom;
            CreateDoor(direction);
        }
        
        public Room GetConnection(Vector2 direction)
        {
            connections.TryGetValue(direction, out Room room);
            return room;
        }
        
        private void CreateDoor(Vector2 direction)
        {
            if (doorPrefab == null) return;
            
            int directionIndex = GetDirectionIndex(direction);
            if (directionIndex >= 0 && directionIndex < doorPositions.Length && doorPositions[directionIndex] != null)
            {
                GameObject door = Instantiate(doorPrefab, doorPositions[directionIndex].position, doorPositions[directionIndex].rotation);
                door.transform.SetParent(transform);
                
                RoomDoor doorComponent = door.GetComponent<RoomDoor>();
                if (doorComponent == null)
                    doorComponent = door.AddComponent<RoomDoor>();
                    
                doorComponent.Initialize(direction, connections[direction]);
                activeDoors.Add(door);
            }
        }
        
        private int GetDirectionIndex(Vector2 direction)
        {
            if (direction == Vector2.up) return 0;
            if (direction == Vector2.right) return 1;
            if (direction == Vector2.down) return 2;
            if (direction == Vector2.left) return 3;
            return -1;
        }
        
        public virtual void OnEnterRoom()
        {
            if (!isCleared && roomType == RoomType.Combat)
            {
                SpawnEnemies();
                LockDoors();
            }
        }
        
        public virtual void OnExitRoom()
        {
            // Handle any cleanup when leaving room
        }
        
        private void SpawnEnemies()
        {
            if (enemyPrefabs.Length == 0 || enemySpawnPoints.Length == 0) return;
            
            int enemyCount = Mathf.Min(Random.Range(1, 4), enemySpawnPoints.Length);
            
            for (int i = 0; i < enemyCount; i++)
            {
                GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                Transform spawnPoint = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length)];
                
                GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
                spawnedEnemies.Add(enemy);
                
                // Subscribe to enemy death event
                // Enemy component should have OnDeath event
            }
        }
        
        public void OnEnemyDefeated(GameObject enemy)
        {
            if (spawnedEnemies.Contains(enemy))
            {
                spawnedEnemies.Remove(enemy);
                
                if (spawnedEnemies.Count == 0)
                {
                    ClearRoom();
                }
            }
        }
        
        private void ClearRoom()
        {
            isCleared = true;
            UnlockDoors();
            SpawnRewards();
        }
        
        private void LockDoors()
        {
            foreach (GameObject door in activeDoors)
            {
                RoomDoor doorComponent = door.GetComponent<RoomDoor>();
                if (doorComponent != null)
                    doorComponent.SetLocked(true);
            }
        }
        
        private void UnlockDoors()
        {
            foreach (GameObject door in activeDoors)
            {
                RoomDoor doorComponent = door.GetComponent<RoomDoor>();
                if (doorComponent != null)
                    doorComponent.SetLocked(false);
            }
        }
        
        private void SpawnRewards()
        {
            // Spawn gold, items, etc.
            switch (roomType)
            {
                case RoomType.Combat:
                    // Spawn gold and maybe items
                    break;
                case RoomType.Treasure:
                    // Spawn valuable items
                    break;
                case RoomType.Boss:
                    // Spawn boss rewards
                    break;
            }
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = GetRoomColor();
            Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);
            
            // Draw connections
            foreach (var connection in connections)
            {
                if (connection.Value != null)
                {
                    Gizmos.color = Color.yellow;
                    Vector3 direction = new Vector3(connection.Key.x, connection.Key.y, 0) * 1.5f;
                    Gizmos.DrawLine(transform.position, transform.position + direction);
                }
            }
        }
        
        private Color GetRoomColor()
        {
            switch (roomType)
            {
                case RoomType.Start: return Color.green;
                case RoomType.Combat: return Color.red;
                case RoomType.Treasure: return Color.yellow;
                case RoomType.Shop: return Color.blue;
                case RoomType.Challenge: return Color.magenta;
                case RoomType.Boss: return Color.black;
                default: return Color.gray;
            }
        }
    }
}