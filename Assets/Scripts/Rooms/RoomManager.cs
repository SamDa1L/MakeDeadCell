using UnityEngine;
using System.Collections.Generic;

namespace DeadCellsTestFramework.Rooms
{
    public class RoomManager : MonoBehaviour
    {
        [Header("Room Generation")]
        [SerializeField] private GameObject[] roomPrefabs;
        [SerializeField] private int maxRooms = 20;
        [SerializeField] private float roomSpacing = 20f;
        
        [Header("Current Room")]
        [SerializeField] private Room currentRoom;
        
        private List<Room> generatedRooms = new List<Room>();
        private Dictionary<Vector2, Room> roomGrid = new Dictionary<Vector2, Room>();
        
        public Room CurrentRoom => currentRoom;
        public List<Room> GeneratedRooms => generatedRooms;
        
        public delegate void RoomChangedDelegate(Room newRoom, Room previousRoom);
        public event RoomChangedDelegate OnRoomChanged;
        
        private void Start()
        {
            GenerateRooms();
        }
        
        private void GenerateRooms()
        {
            if (roomPrefabs.Length == 0) return;
            
            // Generate starting room
            Vector2 startPosition = Vector2.zero;
            Room startRoom = CreateRoom(startPosition, RoomType.Start);
            SetCurrentRoom(startRoom);
            
            // Generate connected rooms
            GenerateConnectedRooms(startRoom, maxRooms - 1);
        }
        
        private void GenerateConnectedRooms(Room startRoom, int remainingRooms)
        {
            Queue<Room> roomQueue = new Queue<Room>();
            roomQueue.Enqueue(startRoom);
            
            while (roomQueue.Count > 0 && remainingRooms > 0)
            {
                Room currentGenRoom = roomQueue.Dequeue();
                
                // Try to generate rooms in all four directions
                Vector2[] directions = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
                
                foreach (Vector2 direction in directions)
                {
                    if (remainingRooms <= 0) break;
                    
                    Vector2 newPosition = currentGenRoom.GridPosition + direction;
                    
                    if (!roomGrid.ContainsKey(newPosition) && Random.value < 0.6f)
                    {
                        RoomType roomType = DetermineRoomType(newPosition, remainingRooms);
                        Room newRoom = CreateRoom(newPosition, roomType);
                        
                        // Connect rooms
                        ConnectRooms(currentGenRoom, newRoom, direction);
                        
                        roomQueue.Enqueue(newRoom);
                        remainingRooms--;
                    }
                }
            }
        }
        
        private Room CreateRoom(Vector2 gridPosition, RoomType roomType)
        {
            Vector3 worldPosition = new Vector3(gridPosition.x * roomSpacing, gridPosition.y * roomSpacing, 0);
            
            GameObject roomPrefab = GetRoomPrefab(roomType);
            GameObject roomObject = Instantiate(roomPrefab, worldPosition, Quaternion.identity, transform);
            
            Room room = roomObject.GetComponent<Room>();
            if (room == null)
                room = roomObject.AddComponent<Room>();
                
            room.Initialize(gridPosition, roomType);
            
            generatedRooms.Add(room);
            roomGrid[gridPosition] = room;
            
            return room;
        }
        
        private GameObject GetRoomPrefab(RoomType roomType)
        {
            if (roomPrefabs.Length > 0)
                return roomPrefabs[Random.Range(0, roomPrefabs.Length)];
            
            // Create default room if no prefabs available
            GameObject defaultRoom = new GameObject("DefaultRoom");
            defaultRoom.AddComponent<Room>();
            return defaultRoom;
        }
        
        private RoomType DetermineRoomType(Vector2 position, int remainingRooms)
        {
            if (remainingRooms <= 3)
                return RoomType.Boss;
            
            float rand = Random.value;
            if (rand < 0.1f) return RoomType.Treasure;
            if (rand < 0.2f) return RoomType.Shop;
            if (rand < 0.3f) return RoomType.Challenge;
            return RoomType.Combat;
        }
        
        private void ConnectRooms(Room room1, Room room2, Vector2 direction)
        {
            room1.AddConnection(direction, room2);
            room2.AddConnection(-direction, room1);
        }
        
        public void SetCurrentRoom(Room newRoom)
        {
            Room previousRoom = currentRoom;
            currentRoom = newRoom;
            
            if (newRoom != null)
                newRoom.OnEnterRoom();
                
            if (previousRoom != null)
                previousRoom.OnExitRoom();
                
            OnRoomChanged?.Invoke(newRoom, previousRoom);
        }
        
        public Room GetRoomAt(Vector2 gridPosition)
        {
            roomGrid.TryGetValue(gridPosition, out Room room);
            return room;
        }
        
        public void MoveToRoom(Vector2 direction)
        {
            if (currentRoom != null)
            {
                Room nextRoom = currentRoom.GetConnection(direction);
                if (nextRoom != null)
                {
                    SetCurrentRoom(nextRoom);
                }
            }
        }
    }
}