using UnityEngine;
using DeadCellsTestFramework.Player;
using DeadCellsTestFramework.Rooms;

namespace DeadCellsTestFramework.Player
{
    public class RoomDoor : MonoBehaviour
    {
        [Header("Door Settings")]
        [SerializeField] private bool isLocked = false;
        [SerializeField] private Vector2 direction;
        [SerializeField] private Room targetRoom;
        
        [Header("Visual")]
        [SerializeField] private Collider2D doorCollider;
        [SerializeField] private SpriteRenderer doorRenderer;
        
        public bool IsLocked => isLocked;
        public Room TargetRoom => targetRoom;
        
        private void Awake()
        {
            if (doorCollider == null)
                doorCollider = GetComponent<Collider2D>();
                
            if (doorRenderer == null)
                doorRenderer = GetComponent<SpriteRenderer>();
        }
        
        public void Initialize(Vector2 dir, Room target)
        {
            direction = dir;
            targetRoom = target;
            UpdateVisuals();
        }
        
        public void SetLocked(bool locked)
        {
            isLocked = locked;
            UpdateVisuals();
        }
        
        private void UpdateVisuals()
        {
            if (doorRenderer != null)
            {
                doorRenderer.color = isLocked ? Color.red : Color.white;
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isLocked) return;
            
            if (other.CompareTag("Player"))
            {
                PlayerController player = other.GetComponent<PlayerController>();
                if (player != null)
                {
                    TransitionToRoom();
                }
            }
        }
        
        private void TransitionToRoom()
        {
            if (targetRoom != null)
            {
                RoomManager roomManager = FindObjectOfType<RoomManager>();
                if (roomManager != null)
                {
                    roomManager.SetCurrentRoom(targetRoom);
                    
                    // Move player to the new room
                    PlayerController player = FindObjectOfType<PlayerController>();
                    if (player != null)
                    {
                        Vector3 playerPosition = targetRoom.transform.position - new Vector3(direction.x * 5f, direction.y * 5f, 0);
                        player.transform.position = playerPosition;
                    }
                }
            }
        }
    }
}