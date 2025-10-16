using UnityEngine;
using DeadCells.Core;
using DeadCells.Player;
using DeadCells.Rooms;
using LDtkUnity;

namespace DeadCells.Game
{
    /// <summary>
    /// 玩家出生点管理器
    /// 负责在场景中生成玩家角色,支持从LDtk读取出生点位置
    /// 预留角色皮肤/选择系统接口
    /// </summary>
    public class PlayerSpawnManager : MonoBehaviour
    {
        [Header("Player Prefab Settings")]
        [Tooltip("当前使用的玩家预制体 (支持未来角色切换)")]
        [SerializeField] private GameObject currentPlayerPrefab;

        [Header("Spawn Settings")]
        [Tooltip("是否在场景加载时自动生成玩家")]
        [SerializeField] private bool autoSpawnOnStart = true;

        [Tooltip("如果LDtk中找不到出生点,使用此备用位置")]
        [SerializeField] private Vector3 fallbackSpawnPosition = Vector3.zero;

        [Tooltip("是否强制使用备用位置(调试用)")]
        [SerializeField] private bool useFallbackPosition = false;

        [Header("Character Selection (预留扩展)")]
        [Tooltip("当前选择的角色ID (未来用于角色皮肤系统)")]
        [SerializeField] private string selectedCharacterId = "adventurer";

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // 运行时引用
        private PlayerController spawnedPlayer;
        private LDtkRoomManager roomManager;

        // 单例模式 (可选,便于全局访问)
        public static PlayerSpawnManager Instance { get; private set; }

        // 公共属性
        public PlayerController SpawnedPlayer => spawnedPlayer;
        public string SelectedCharacterId => selectedCharacterId;

        private void Awake()
        {
            // 单例初始化
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("PlayerSpawnManager: 场景中存在多个实例,销毁重复对象!");
                Destroy(gameObject);
                return;
            }

            // 获取LDtkRoomManager引用
            roomManager = LDtkRoomManager.GetInstance();
        }

        private void Start()
        {
            if (autoSpawnOnStart)
            {
                SpawnPlayer();
            }
        }

        /// <summary>
        /// 生成玩家角色 (主要入口方法)
        /// </summary>
        public PlayerController SpawnPlayer()
        {
            if (spawnedPlayer != null)
            {
                if (debugMode)
                    Debug.LogWarning("PlayerSpawnManager: 玩家已存在,跳过生成");
                return spawnedPlayer;
            }

            // 获取出生点位置
            Vector3 spawnPosition = GetSpawnPosition();

            // 获取玩家预制体
            GameObject playerPrefab = GetPlayerPrefab();
            if (playerPrefab == null)
            {
                Debug.LogError("PlayerSpawnManager: 玩家预制体未分配且找不到默认预制体!");
                return null;
            }

            // 实例化玩家
            GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            playerInstance.name = "Player"; // 统一命名

            // 获取PlayerController组件
            spawnedPlayer = playerInstance.GetComponent<PlayerController>();
            if (spawnedPlayer == null)
            {
                Debug.LogError("PlayerSpawnManager: 玩家预制体缺少PlayerController组件!");
                Destroy(playerInstance);
                return null;
            }

            // 注册到SystemRegistry (便于其他系统访问)
            SystemRegistry.Instance?.RegisterSystem(spawnedPlayer);

            if (debugMode)
                Debug.Log($"PlayerSpawnManager: 玩家生成成功 at {spawnPosition}, CharacterID={selectedCharacterId}");

            return spawnedPlayer;
        }

        /// <summary>
        /// 获取出生点位置 - 优先从LDtk读取
        /// </summary>
        private Vector3 GetSpawnPosition()
        {
            // 调试模式: 强制使用备用位置
            if (useFallbackPosition)
            {
                if (debugMode)
                    Debug.Log($"PlayerSpawnManager: 使用备用出生点 {fallbackSpawnPosition}");
                return fallbackSpawnPosition;
            }

            // 尝试从LDtk获取出生点
            Vector3 ldtkSpawnPosition = GetLDtkSpawnPosition();
            if (ldtkSpawnPosition != Vector3.zero)
            {
                if (debugMode)
                    Debug.Log($"PlayerSpawnManager: 从LDtk读取出生点 {ldtkSpawnPosition}");
                return ldtkSpawnPosition;
            }

            // 回退到备用位置
            Debug.LogWarning("PlayerSpawnManager: LDtk中找不到PlayerSpawn实体,使用备用位置");
            return fallbackSpawnPosition;
        }

        /// <summary>
        /// 从LDtk关卡中查找PlayerSpawn实体位置
        /// </summary>
        private Vector3 GetLDtkSpawnPosition()
        {
            // 方法1: 通过LDtkRoomManager的已有逻辑 (推荐)
            if (roomManager != null)
            {
                // 查找场景中所有LDtkComponentEntity
                LDtkComponentEntity[] entities = FindObjectsOfType<LDtkComponentEntity>();
                foreach (var entity in entities)
                {
                    // 匹配PlayerSpawn实体 (支持多种命名方式)
                    string entityName = entity.name.ToLower();
                    if (entityName.Contains("playerspawn") ||
                        entityName.Contains("player_spawn") ||
                        entityName == "playerspawn")
                    {
                        return entity.transform.position;
                    }
                }
            }

            // 方法2: 直接查找名为"PlayerSpawn"的GameObject (备用)
            GameObject spawnObject = GameObject.Find("PlayerSpawn");
            if (spawnObject != null)
            {
                return spawnObject.transform.position;
            }

            return Vector3.zero; // 未找到
        }

        /// <summary>
        /// 获取玩家预制体 - 支持未来角色选择系统
        /// </summary>
        private GameObject GetPlayerPrefab()
        {
            // 优先使用Inspector中分配的预制体
            if (currentPlayerPrefab != null)
            {
                return currentPlayerPrefab;
            }

            // 未来扩展: 根据selectedCharacterId从资源加载对应角色
            // GameObject prefab = Resources.Load<GameObject>($"Characters/{selectedCharacterId}");
            // if (prefab != null) return prefab;

            // 回退: 尝试从场景中找到现有Player对象作为模板 (仅调试用)
            PlayerController existingPlayer = FindObjectOfType<PlayerController>();
            if (existingPlayer != null && !existingPlayer.gameObject.scene.name.Contains("DontDestroyOnLoad"))
            {
                Debug.LogWarning("PlayerSpawnManager: 使用场景中现有Player对象作为预制体模板");
                return existingPlayer.gameObject;
            }

            return null;
        }

        /// <summary>
        /// 重生玩家 (用于死亡后复活等场景)
        /// </summary>
        /// <param name="useCheckpoint">是否使用检查点位置 (未来扩展)</param>
        public void RespawnPlayer(bool useCheckpoint = false)
        {
            if (spawnedPlayer != null)
            {
                Destroy(spawnedPlayer.gameObject);
                spawnedPlayer = null;
            }

            SpawnPlayer();
        }

        /// <summary>
        /// 切换角色 (预留接口,未来实现角色选择功能)
        /// </summary>
        /// <param name="characterId">角色ID</param>
        public void SelectCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                Debug.LogError("PlayerSpawnManager: 无效的角色ID");
                return;
            }

            selectedCharacterId = characterId;

            if (debugMode)
                Debug.Log($"PlayerSpawnManager: 角色已切换为 {characterId}");

            // 未来扩展: 从Resources或Addressables加载对应角色预制体
            // currentPlayerPrefab = Resources.Load<GameObject>($"Characters/{characterId}");
        }

        /// <summary>
        /// 传送玩家到指定位置
        /// </summary>
        public void TeleportPlayer(Vector3 position)
        {
            if (spawnedPlayer != null)
            {
                spawnedPlayer.transform.position = position;
                if (debugMode)
                    Debug.Log($"PlayerSpawnManager: 玩家传送到 {position}");
            }
        }

        /// <summary>
        /// 手动设置备用出生点 (运行时调用)
        /// </summary>
        public void SetFallbackSpawnPosition(Vector3 position)
        {
            fallbackSpawnPosition = position;
            if (debugMode)
                Debug.Log($"PlayerSpawnManager: 备用出生点更新为 {position}");
        }
    }
}
