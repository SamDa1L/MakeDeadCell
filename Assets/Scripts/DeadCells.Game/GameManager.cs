using UnityEngine;
using UnityEngine.SceneManagement;
using DeadCells.Core;
using DeadCells.Player;
using DeadCells.Rooms;
using DeadCells.Combat;

namespace DeadCells.Game
{
    /// <summary>
    /// 游戏管理器 - 整个游戏框架的核心管理类
    /// 负责初始化和管理所有子系统，提供全局访问接口
    /// 支持传统系统和数据驱动系统两种模式
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        public bool debugMode = false;                // 调试模式开关，启用时显示调试信息
        public bool useDataDrivenSystems = true;     // 是否使用数据驱动系统（CastleDB + 官方LDtk Unity）
        
        /// <summary>
        /// 单例实例，确保全局只有一个GameManager
        /// </summary>
        public static GameManager Instance { get; private set; }
        
        [Header("Core Systems")]
        [SerializeField] private PlayerController playerController;    // 玩家控制器
        [SerializeField] private CombatManager combatManager;          // 战斗管理器
        
        [Header("Room Systems")]
        [SerializeField] private RoomManager roomManager;              // 传统房间管理器
        [SerializeField] private LDtkRoomManager ldtkRoomManager;      // 官方LDtk Unity数据驱动房间管理器
        
        [Header("Data Systems")]
        [SerializeField] private CastleDBManager castleDBManager;      // CastleDB数据管理器
        [SerializeField] private WeaponFactory weaponFactory;          // 武器工厂
        
        // 系统刷新控制
        private bool isDirtyCache = false;                              // 缓存脏标记
        private float lastRefreshTime = 0f;                             // 上次刷新时间
        private const float REFRESH_COOLDOWN = 0.1f;                    // 刷新冷却时间（秒）
        private const int MAX_REFRESH_PER_FRAME = 1;                    // 每帧最大刷新次数
        private int refreshCountThisFrame = 0;                          // 当前帧已刷新次数
        
        /// <summary>
        /// Unity生命周期 - 初始化单例和系统
        /// </summary>
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);  // 切换场景时不销毁
                InitializeSystems();            // 初始化所有子系统
                SetupSystemRegistryCallbacks(); // 设置系统注册回调
            }
            else
            {
                Destroy(gameObject);            // 销毁重复的GameManager
            }
        }
        
        /// <summary>
        /// Unity生命周期 - 重置每帧刷新计数
        /// </summary>
        private void LateUpdate()
        {
            refreshCountThisFrame = 0;  // 重置每帧的刷新计数
        }
        
        /// <summary>
        /// 设置 SystemRegistry 回调事件
        /// </summary>
        private void SetupSystemRegistryCallbacks()
        {
            if (SystemRegistry.Instance != null)
            {
                SystemRegistry.Instance.OnAllSystemsRefreshed += OnSystemsRefreshed;
            }
            else
            {
                // 延迟设置回调，等待 SystemRegistry 初始化
                StartCoroutine(WaitForSystemRegistry());
            }
        }
        
        /// <summary>
        /// 等待 SystemRegistry 初始化的协程
        /// </summary>
        private System.Collections.IEnumerator WaitForSystemRegistry()
        {
            while (SystemRegistry.Instance == null)
            {
                yield return null;
            }
            SystemRegistry.Instance.OnAllSystemsRefreshed += OnSystemsRefreshed;
        }
        
        /// <summary>
        /// 系统注册表刷新完成回调
        /// </summary>
        private void OnSystemsRefreshed()
        {
            MarkCacheDirty();
            RefreshSystemReferencesFromRegistry();
            if (debugMode)
            {
                Debug.Log("GameManager: Systems refreshed from registry");
            }
        }
        
        /// <summary>
        /// 初始化所有子系统，优先使用 SystemRegistry
        /// </summary>
        private void InitializeSystems()
        {
            // 首先尝试从 SystemRegistry 获取系统引用
            RefreshSystemReferencesFromRegistry();
            
            // 如果 SystemRegistry 还未准备好，回退到传统方式
            if (!HasRequiredSystems())
            {
                RefreshSystemReferencesLegacy(refreshRoomManagersAlways: true);
            }
        }
        
        /// <summary>
        /// 从 SystemRegistry 刷新系统引用 - 智能缓存版本
        /// </summary>
        private void RefreshSystemReferencesFromRegistry()
        {
            if (!CanRefresh()) return;
            
            if (SystemRegistry.Instance == null) 
            {
                // 如果 SystemRegistry 不可用，标记为脏数据等待下次尝试
                MarkCacheDirty();
                return;
            }
            
            UpdateRefreshTimestamp();
            
            // 使用 SystemRegistry 获取系统引用，避免 FindObjectOfType
            if (playerController == null || IsDestroyed(playerController))
                playerController = SystemRegistry.Instance.GetSystem<PlayerController>();

            if (combatManager == null || IsDestroyed(combatManager))
                combatManager = SystemRegistry.Instance.GetSystem<CombatManager>();

            if (castleDBManager == null || IsDestroyed(castleDBManager))
                castleDBManager = SystemRegistry.Instance.GetSystem<CastleDBManager>();

            if (weaponFactory == null || IsDestroyed(weaponFactory))
                weaponFactory = SystemRegistry.Instance.GetSystem<WeaponFactory>();

            // 根据数据驱动模式选择房间管理器
            if (useDataDrivenSystems)
            {
                if (ldtkRoomManager == null || IsDestroyed(ldtkRoomManager))
                    ldtkRoomManager = SystemRegistry.Instance.GetSystem<LDtkRoomManager>();
            }
            else
            {
                if (roomManager == null || IsDestroyed(roomManager))
                    roomManager = SystemRegistry.Instance.GetSystem<RoomManager>();
            }
            
            // 刷新完成，清除脏标记
            isDirtyCache = false;
            
            if (debugMode)
            {
                Debug.Log($"GameManager: Refreshed system references (Frame: {Time.frameCount})");
            }
        }
        
        /// <summary>
        /// 传统方式刷新系统引用 - 作为回退方案
        /// </summary>
        private void RefreshSystemReferencesLegacy(bool refreshRoomManagersAlways = false)
        {
            if (debugMode)
            {
                Debug.Log("GameManager: Using legacy FindObjectOfType fallback");
            }
            
            if (playerController == null || IsDestroyed(playerController))
                playerController = FindObjectOfType<PlayerController>();

            if (combatManager == null || IsDestroyed(combatManager))
                combatManager = FindObjectOfType<CombatManager>();

            if (castleDBManager == null || IsDestroyed(castleDBManager))
                castleDBManager = FindObjectOfType<CastleDBManager>();

            if (weaponFactory == null || IsDestroyed(weaponFactory))
                weaponFactory = FindObjectOfType<WeaponFactory>();

            if (refreshRoomManagersAlways || useDataDrivenSystems)
            {
                if (ldtkRoomManager == null || IsDestroyed(ldtkRoomManager))
                    ldtkRoomManager = FindObjectOfType<LDtkRoomManager>();
            }

            if (refreshRoomManagersAlways || !useDataDrivenSystems)
            {
                if (roomManager == null || IsDestroyed(roomManager))
                    roomManager = FindObjectOfType<RoomManager>();
            }
        }
        
        /// <summary>
        /// 检查是否已有必需的系统引用
        /// </summary>
        private bool HasRequiredSystems()
        {
            return playerController != null && combatManager != null && 
                   castleDBManager != null && weaponFactory != null;
        }

        /// <summary>
        /// 检查组件是否已被销毁
        /// </summary>
        private bool IsDestroyed(Component component)
        {
            return component == null || component.gameObject == null;
        }
        
        /// <summary>
        /// 标记缓存为脏数据
        /// </summary>
        private void MarkCacheDirty()
        {
            isDirtyCache = true;
            if (debugMode)
            {
                Debug.Log("GameManager: Cache marked as dirty");
            }
        }
        
        /// <summary>
        /// 检查是否可以执行刷新操作
        /// </summary>
        private bool CanRefresh()
        {
            // 检查刷新冷却时间
            if (Time.time - lastRefreshTime < REFRESH_COOLDOWN)
            {
                if (debugMode)
                {
                    Debug.Log($"GameManager: Refresh blocked by cooldown ({Time.time - lastRefreshTime:F3}s < {REFRESH_COOLDOWN}s)");
                }
                return false;
            }
            
            // 检查每帧刷新次数限制
            if (refreshCountThisFrame >= MAX_REFRESH_PER_FRAME)
            {
                if (debugMode)
                {
                    Debug.Log($"GameManager: Refresh blocked by frame limit ({refreshCountThisFrame}/{MAX_REFRESH_PER_FRAME})");
                }
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 更新刷新时间戳
        /// </summary>
        private void UpdateRefreshTimestamp()
        {
            lastRefreshTime = Time.time;
            refreshCountThisFrame++;
        }
        
        /// <summary>
        /// 智能获取系统引用 - 优化版本
        /// </summary>
        private T GetSystemSmart<T>() where T : Component
        {
            // 如果缓存是脏的且允许刷新，则刷新缓存
            if (isDirtyCache && CanRefresh())
            {
                RefreshSystemReferencesFromRegistry();
            }
            
            // 从 SystemRegistry 获取
            if (SystemRegistry.Instance != null)
            {
                return SystemRegistry.Instance.GetSystem<T>();
            }
            
            return null;
        }

        #region 系统访问器
        /// <summary>获取玩家控制器，智能处理空引用</summary>
        public PlayerController GetPlayer()
        {
            if (playerController == null || IsDestroyed(playerController))
            {
                // 标记缓存为脏数据，使用智能获取
                MarkCacheDirty();
                playerController = GetSystemSmart<PlayerController>();
            }
            return playerController;
        }

        /// <summary>获取战斗管理器，智能处理空引用</summary>
        public CombatManager GetCombatManager()
        {
            if (combatManager == null || IsDestroyed(combatManager))
            {
                MarkCacheDirty();
                combatManager = GetSystemSmart<CombatManager>();
            }
            return combatManager;
        }

        /// <summary>获取传统房间管理器，智能处理空引用</summary>
        public RoomManager GetRoomManager()
        {
            if (roomManager == null || IsDestroyed(roomManager))
            {
                MarkCacheDirty();
                roomManager = GetSystemSmart<RoomManager>();
            }
            return roomManager;
        }

        /// <summary>获取LDtk房间管理器，智能处理空引用</summary>
        public LDtkRoomManager GetLDtkRoomManager()
        {
            if (ldtkRoomManager == null || IsDestroyed(ldtkRoomManager))
            {
                MarkCacheDirty();
                ldtkRoomManager = GetSystemSmart<LDtkRoomManager>();
            }
            return ldtkRoomManager;
        }

        /// <summary>获取CastleDB数据管理器，智能处理空引用</summary>
        public CastleDBManager GetCastleDBManager()
        {
            if (castleDBManager == null || IsDestroyed(castleDBManager))
            {
                MarkCacheDirty();
                castleDBManager = GetSystemSmart<CastleDBManager>();
            }
            return castleDBManager;
        }

        /// <summary>获取武器工厂，智能处理空引用</summary>
        public WeaponFactory GetWeaponFactory()
        {
            if (weaponFactory == null || IsDestroyed(weaponFactory))
            {
                MarkCacheDirty();
                weaponFactory = GetSystemSmart<WeaponFactory>();
            }
            return weaponFactory;
        }
        #endregion

        #region 便捷方法
        /// <summary>
        /// 检查是否使用数据驱动系统
        /// </summary>
        /// <returns>true表示使用数据驱动系统（CastleDB + 官方LDtk Unity）</returns>
        public bool IsUsingDataDrivenSystems() => useDataDrivenSystems;

        /// <summary>
        /// 切换数据驱动模式
        /// </summary>
        /// <param name="enable">true=启用数据驱动模式，false=使用传统模式</param>
        public void SwitchToDataDrivenMode(bool enable)
        {
            useDataDrivenSystems = enable;

            // 刷新房间管理器引用
            RefreshSystemReferencesFromRegistry();

            var currentLDtkRoomManager = ldtkRoomManager;
            var currentRoomManager = roomManager;

            if (enable)
            {
                if (currentLDtkRoomManager != null)
                    currentLDtkRoomManager.gameObject.SetActive(true);

                if (currentRoomManager != null)
                    currentRoomManager.gameObject.SetActive(false);
            }
            else
            {
                if (currentRoomManager != null)
                    currentRoomManager.gameObject.SetActive(true);

                if (currentLDtkRoomManager != null)
                    currentLDtkRoomManager.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// 调试方法：获取系统注册表统计信息
        /// </summary>
        public string GetSystemStats()
        {
            if (SystemRegistry.Instance != null)
            {
                return SystemRegistry.Instance.GetRegistryStats();
            }
            return "SystemRegistry not available";
        }
        
        /// <summary>
        /// 手动清理无效的系统引用
        /// </summary>
        public void CleanupSystems()
        {
            SystemRegistry.Instance?.CleanupInvalidReferences();
            MarkCacheDirty();  // 清理后标记缓存为脏数据
        }
        
        /// <summary>
        /// 强制刷新所有系统引用 - 忽略冷却限制
        /// </summary>
        public void ForceRefreshSystems()
        {
            lastRefreshTime = 0f;  // 重置冷却时间
            refreshCountThisFrame = 0;  // 重置帧计数
            MarkCacheDirty();
            RefreshSystemReferencesFromRegistry();
            
            if (debugMode)
            {
                Debug.Log("GameManager: Force refreshed all system references");
            }
        }
        
        /// <summary>
        /// 获取刷新状态信息 - 用于调试
        /// </summary>
        public string GetRefreshStatus()
        {
            return $"Dirty: {isDirtyCache}, LastRefresh: {lastRefreshTime:F2}s, " +
                   $"RefreshCount: {refreshCountThisFrame}/{MAX_REFRESH_PER_FRAME}, " +
                   $"Cooldown: {(Time.time - lastRefreshTime):F2}s/{REFRESH_COOLDOWN}s";
        }
        #endregion
    }
}
