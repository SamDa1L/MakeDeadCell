using UnityEngine;
using UnityEngine.Tilemaps;
using DeadCellsTestFramework.Core;
using DeadCellsTestFramework.Data;
using DeadCellsTestFramework.Rooms;
using DeadCellsTestFramework.Combat;
using DeadCellsTestFramework.Effects;

namespace DeadCellsTestFramework.Tools
{
    public class GameSetupWizard : MonoBehaviour
    {
        [Header("Auto Setup Options")]
        [SerializeField] private bool createManagers = true;
        [SerializeField] private bool createTilemapStructure = true;
        [SerializeField] private bool createUICanvas = true;
        [SerializeField] private bool setupDataDrivenSystems = true;
        
        [Header("Prefab References")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private TileBase defaultTile;
        
        [ContextMenu("Setup Game Framework")]
        public void SetupGameFramework()
        {
            Debug.Log("🚀 Setting up Dead Cells Test Framework...");
            
            if (createManagers)
                CreateManagerObjects();
                
            if (createTilemapStructure)
                CreateTilemapStructure();
                
            if (createUICanvas)
                CreateUICanvas();
                
            if (setupDataDrivenSystems)
                SetupDataDrivenSystems();
                
            CreatePlayer();
            
            Debug.Log("✅ Game framework setup complete!");
        }
        
        private void CreateManagerObjects()
        {
            Debug.Log("Creating manager objects...");
            
            // Create main GameManager
            if (FindObjectOfType<GameManager>() == null)
            {
                GameObject gameManager = new GameObject("GameManager");
                gameManager.AddComponent<GameManager>();
            }
            
            // Create CastleDBManager
            if (FindObjectOfType<CastleDBManager>() == null)
            {
                GameObject castleDBManager = new GameObject("CastleDBManager");
                castleDBManager.AddComponent<CastleDBManager>();
            }
            
            // Note: Official LDtk Unity package components are created through the LDtk project asset
            
            // Create WeaponFactory
            if (FindObjectOfType<WeaponFactory>() == null)
            {
                GameObject weaponFactory = new GameObject("WeaponFactory");
                weaponFactory.AddComponent<WeaponFactory>();
            }
            
            // Create CombatManager
            if (FindObjectOfType<CombatManager>() == null)
            {
                GameObject combatManager = new GameObject("CombatManager");
                combatManager.AddComponent<CombatManager>();
            }
            
            // Create EffectsManager
            if (FindObjectOfType<EffectsManager>() == null)
            {
                GameObject effectsManager = new GameObject("EffectsManager");
                effectsManager.AddComponent<EffectsManager>();
            }
            
            // Create room managers
            if (FindObjectOfType<RoomManager>() == null)
            {
                GameObject roomManager = new GameObject("RoomManager");
                roomManager.AddComponent<RoomManager>();
            }
            
            if (FindObjectOfType<LDtkRoomManager>() == null)
            {
                GameObject ldtkRoomManager = new GameObject("LDtkRoomManager");
                ldtkRoomManager.AddComponent<LDtkRoomManager>();
            }
        }
        
        private void CreateTilemapStructure()
        {
            Debug.Log("Creating tilemap structure...");
            
            // Find or create Grid
            Grid grid = FindObjectOfType<Grid>();
            if (grid == null)
            {
                GameObject gridObject = new GameObject("Grid");
                grid = gridObject.AddComponent<Grid>();
            }
            
            // Create Background Tilemap
            Transform backgroundTransform = grid.transform.Find("Background");
            if (backgroundTransform == null)
            {
                GameObject backgroundObject = new GameObject("Background");
                backgroundObject.transform.SetParent(grid.transform);
                
                Tilemap backgroundTilemap = backgroundObject.AddComponent<Tilemap>();
                TilemapRenderer backgroundRenderer = backgroundObject.AddComponent<TilemapRenderer>();
                backgroundRenderer.sortingLayerName = "Background";
            }
            
            // Create Collision Tilemap
            Transform collisionTransform = grid.transform.Find("Collision");
            if (collisionTransform == null)
            {
                GameObject collisionObject = new GameObject("Collision");
                collisionObject.transform.SetParent(grid.transform);
                
                Tilemap collisionTilemap = collisionObject.AddComponent<Tilemap>();
                TilemapRenderer collisionRenderer = collisionObject.AddComponent<TilemapRenderer>();
                TilemapCollider2D collisionCollider = collisionObject.AddComponent<TilemapCollider2D>();
                collisionRenderer.sortingLayerName = "Default";
            }
            
            // Create Entity Parent
            Transform entityParent = grid.transform.Find("EntityParent");
            if (entityParent == null)
            {
                GameObject entityParentObject = new GameObject("EntityParent");
                entityParentObject.transform.SetParent(grid.transform);
            }
            
            // Note: With official LDtk Unity package, tilemap references are handled automatically
            Debug.Log("Tilemap structure created. LDtk project will handle tilemap generation automatically.");
        }
        
        private void CreateUICanvas()
        {
            Debug.Log("Creating UI canvas...");
            
            if (FindObjectOfType<Canvas>() == null)
            {
                GameObject canvasObject = new GameObject("Canvas");
                Canvas canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                
                // Create screen flash for effects
                GameObject screenFlashObject = new GameObject("ScreenFlash");
                screenFlashObject.transform.SetParent(canvasObject.transform, false);
                
                UnityEngine.UI.Image flashImage = screenFlashObject.AddComponent<UnityEngine.UI.Image>();
                flashImage.color = new Color(1, 1, 1, 0);
                
                // Make it fullscreen
                RectTransform rectTransform = screenFlashObject.GetComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                
                screenFlashObject.AddComponent<ScreenFlash>();
                
                // Assign to EffectsManager
                EffectsManager effectsManager = FindObjectOfType<EffectsManager>();
                if (effectsManager != null)
                {
                    Debug.Log("Please manually assign ScreenFlash to EffectsManager");
                }
            }
        }
        
        private void SetupDataDrivenSystems()
        {
            Debug.Log("Setting up data-driven systems...");
            
            // Create StreamingAssets folders
            string streamingAssetsPath = Application.streamingAssetsPath;
            
            if (!System.IO.Directory.Exists(streamingAssetsPath))
            {
                System.IO.Directory.CreateDirectory(streamingAssetsPath);
            }
            
            string dataPath = System.IO.Path.Combine(streamingAssetsPath, "Data");
            if (!System.IO.Directory.Exists(dataPath))
            {
                System.IO.Directory.CreateDirectory(dataPath);
            }
            
            string levelsPath = System.IO.Path.Combine(streamingAssetsPath, "Levels");
            if (!System.IO.Directory.Exists(levelsPath))
            {
                System.IO.Directory.CreateDirectory(levelsPath);
            }
            
            // Create Resources folders
            string resourcesPath = "Assets/Resources";
            
            if (!System.IO.Directory.Exists(resourcesPath))
            {
                System.IO.Directory.CreateDirectory(resourcesPath);
            }
            
            string[] resourceFolders = { "Weapons", "Enemies", "Items", "LDtk/Entities" };
            
            foreach (string folder in resourceFolders)
            {
                string folderPath = System.IO.Path.Combine(resourcesPath, folder);
                if (!System.IO.Directory.Exists(folderPath))
                {
                    System.IO.Directory.CreateDirectory(folderPath);
                }
            }
            
            Debug.Log("✅ Folder structure created. Check README_CastleDB_LDtk.md for setup instructions.");
        }
        
        private void CreatePlayer()
        {
            if (FindObjectOfType<Player.PlayerController>() == null)
            {
                GameObject player;
                
                if (playerPrefab != null)
                {
                    player = Instantiate(playerPrefab);
                }
                else
                {
                    player = new GameObject("Player");
                    player.AddComponent<Player.PlayerController>();
                    player.AddComponent<Rigidbody2D>();
                    player.AddComponent<BoxCollider2D>();
                    player.AddComponent<Combat.Health>();
                    player.AddComponent<Combat.HitstunController>();
                    player.AddComponent<Animation.AnimationController>();
                    
                    // Create GroundCheck
                    GameObject groundCheck = new GameObject("GroundCheck");
                    groundCheck.transform.SetParent(player.transform);
                    groundCheck.transform.localPosition = new Vector3(0, -0.5f, 0);
                    
                    player.tag = "Player";
                }
                
                Debug.Log("Player created. Don't forget to assign GroundCheck reference!");
            }
        }
        
        [ContextMenu("Create Example Data")]
        public void CreateExampleData()
        {
            Debug.Log("Creating example CastleDB data...");
            
            // The example CastleDB file is already created in StreamingAssets
            Debug.Log("✅ Example data available at Assets/StreamingAssets/Data/castle_db_example.cdb");
            Debug.Log("📖 Read README_CastleDB_LDtk.md for complete setup instructions");
        }
    }
}