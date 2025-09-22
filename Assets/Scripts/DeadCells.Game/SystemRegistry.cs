using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using DeadCells.Player;
using DeadCells.Combat;
using DeadCells.Rooms;
using DeadCells.Game;

namespace DeadCells.Core
{
    /// <summary>
    /// 高性能系统注册器 - 替代 FindObjectOfType 的性能问题
    /// 使用显式注册和场景加载事件来管理系统引用
    /// </summary>
    public class SystemRegistry : MonoBehaviour
    {
        public static SystemRegistry Instance { get; private set; }
        
        // 系统引用注册表
        private Dictionary<Type, Component> registeredSystems = new Dictionary<Type, Component>();
        
        // 事件通知
        public event System.Action<Type, Component> OnSystemRegistered;
        public event System.Action<Type> OnSystemUnregistered;
        public event System.Action OnAllSystemsRefreshed;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                // 监听场景加载事件
                SceneManager.sceneLoaded += OnSceneLoaded;
                SceneManager.sceneUnloaded += OnSceneUnloaded;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                SceneManager.sceneUnloaded -= OnSceneUnloaded;
                Instance = null;
            }
        }
        
        /// <summary>
        /// 显式注册系统组件
        /// </summary>
        public void RegisterSystem<T>(T system) where T : Component
        {
            if (system == null) return;
            
            Type systemType = typeof(T);
            registeredSystems[systemType] = system;
            
            OnSystemRegistered?.Invoke(systemType, system);
            
            Debug.Log($"System registered: {systemType.Name}");
        }
        
        /// <summary>
        /// 注销系统组件
        /// </summary>
        public void UnregisterSystem<T>() where T : Component
        {
            Type systemType = typeof(T);
            if (registeredSystems.Remove(systemType))
            {
                OnSystemUnregistered?.Invoke(systemType);
                Debug.Log($"System unregistered: {systemType.Name}");
            }
        }
        
        /// <summary>
        /// 获取已注册的系统组件
        /// </summary>
        public T GetSystem<T>() where T : Component
        {
            Type systemType = typeof(T);
            if (registeredSystems.TryGetValue(systemType, out Component system))
            {
                // 检查组件是否仍然有效
                if (system != null && system.gameObject != null)
                {
                    return system as T;
                }
                else
                {
                    // 清理无效引用
                    registeredSystems.Remove(systemType);
                    OnSystemUnregistered?.Invoke(systemType);
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 检查系统是否已注册且有效
        /// </summary>
        public bool IsSystemRegistered<T>() where T : Component
        {
            return GetSystem<T>() != null;
        }
        
        /// <summary>
        /// 场景加载时的回调 - 自动重新扫描和注册系统
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"Scene loaded: {scene.name}, refreshing system registry...");
            
            // 使用协程延迟刷新，确保所有对象都已初始化
            StartCoroutine(RefreshSystemsCoroutine());
        }
        
        /// <summary>
        /// 场景卸载时的回调
        /// </summary>
        private void OnSceneUnloaded(Scene scene)
        {
            Debug.Log($"Scene unloaded: {scene.name}");
            // 清理可能已失效的引用将在下次访问时自动处理
        }
        
        /// <summary>
        /// 协程方式刷新系统注册 - 避免时序问题
        /// </summary>
        private System.Collections.IEnumerator RefreshSystemsCoroutine()
        {
            // 等待一帧，确保所有 Awake 都已执行
            yield return null;
            
            RefreshSystemRegistrations();
        }
        
        /// <summary>
        /// 刷新系统注册 - 仅在必要时使用 FindObjectOfType
        /// </summary>
        public void RefreshSystemRegistrations()
        {
            Debug.Log("Refreshing system registrations...");
            
            // 只对未注册或已失效的系统使用 FindObjectOfType
            RefreshSystemIfNeeded<PlayerController>();
            RefreshSystemIfNeeded<CombatManager>();
            RefreshSystemIfNeeded<CastleDBManager>();
            RefreshSystemIfNeeded<WeaponFactory>();
            RefreshSystemIfNeeded<LDtkRoomManager>();
            RefreshSystemIfNeeded<RoomManager>();
            RefreshSystemIfNeeded<EffectsManager>();
            
            OnAllSystemsRefreshed?.Invoke();
            Debug.Log("System registry refresh completed.");
        }
        
        /// <summary>
        /// 仅在需要时刷新特定系统
        /// </summary>
        private void RefreshSystemIfNeeded<T>() where T : Component
        {
            if (!IsSystemRegistered<T>())
            {
                T system = FindObjectOfType<T>();
                if (system != null)
                {
                    RegisterSystem(system);
                }
            }
        }
        
        /// <summary>
        /// 获取所有已注册系统的统计信息
        /// </summary>
        public string GetRegistryStats()
        {
            int validSystems = 0;
            int invalidSystems = 0;
            
            foreach (var kvp in registeredSystems)
            {
                if (kvp.Value != null && kvp.Value.gameObject != null)
                    validSystems++;
                else
                    invalidSystems++;
            }
            
            return $"Registry Stats: {validSystems} valid, {invalidSystems} invalid, {registeredSystems.Count} total";
        }
        
        /// <summary>
        /// 强制清理所有无效引用
        /// </summary>
        public void CleanupInvalidReferences()
        {
            var toRemove = new List<Type>();
            
            foreach (var kvp in registeredSystems)
            {
                if (kvp.Value == null || kvp.Value.gameObject == null)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (var type in toRemove)
            {
                registeredSystems.Remove(type);
                OnSystemUnregistered?.Invoke(type);
            }
            
            if (toRemove.Count > 0)
            {
                Debug.Log($"Cleaned up {toRemove.Count} invalid system references");
            }
        }
    }
}
