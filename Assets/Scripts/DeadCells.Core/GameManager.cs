using UnityEngine;
using DeadCells.Player;
using DeadCells.Rooms;
using DeadCells.Combat;

namespace DeadCells.Core
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
            }
            else
            {
                Destroy(gameObject);            // 销毁重复的GameManager
            }
        }
        
        /// <summary>
        /// 初始化所有子系统，自动查找场景中的组件
        /// </summary>
        private void InitializeSystems()
        {
            // 核心系统初始化
            if (playerController == null)
                playerController = FindObjectOfType<PlayerController>();
                
            if (combatManager == null)
                combatManager = FindObjectOfType<CombatManager>();
            
            // 数据系统初始化
            if (castleDBManager == null)
                castleDBManager = FindObjectOfType<CastleDBManager>();
                
            if (weaponFactory == null)
                weaponFactory = FindObjectOfType<WeaponFactory>();
            
            // 根据设置选择房间系统
            if (useDataDrivenSystems)
            {
                // 使用LDtk数据驱动的房间管理器
                if (ldtkRoomManager == null)
                    ldtkRoomManager = FindObjectOfType<LDtkRoomManager>();
            }
            else
            {
                // 使用传统的程序化房间管理器
                if (roomManager == null)
                    roomManager = FindObjectOfType<RoomManager>();
            }
        }
        
        #region 系统访问器
        /// <summary>获取玩家控制器</summary>
        public PlayerController GetPlayer() => playerController;
        /// <summary>获取战斗管理器</summary>
        public CombatManager GetCombatManager() => combatManager;
        
        /// <summary>获取传统房间管理器</summary>
        public RoomManager GetRoomManager() => roomManager;
        /// <summary>获取LDtk房间管理器</summary>
        public LDtkRoomManager GetLDtkRoomManager() => ldtkRoomManager;
        
        /// <summary>获取CastleDB数据管理器</summary>
        public CastleDBManager GetCastleDBManager() => castleDBManager;
        /// <summary>获取武器工厂</summary>
        public WeaponFactory GetWeaponFactory() => weaponFactory;
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
            
            if (enable)
            {
                // 启用LDtk房间管理器
                if (ldtkRoomManager != null)
                    ldtkRoomManager.gameObject.SetActive(true);
                    
                if (roomManager != null)
                    roomManager.gameObject.SetActive(false);
            }
            else
            {
                // 启用传统房间管理器
                if (roomManager != null)
                    roomManager.gameObject.SetActive(true);
                    
                if (ldtkRoomManager != null)
                    ldtkRoomManager.gameObject.SetActive(false);
            }
        }
        #endregion
    }
}