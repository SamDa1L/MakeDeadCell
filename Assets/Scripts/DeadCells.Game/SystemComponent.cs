using UnityEngine;

namespace DeadCells.Core
{
    /// <summary>
    /// 系统组件基类 - 提供自动注册到 SystemRegistry 的功能
    /// 所有需要被 GameManager 管理的系统都应该继承此类
    /// </summary>
    public abstract class SystemComponent : MonoBehaviour
    {
        [Header("System Component")]
        [SerializeField] private bool autoRegister = true;  // 是否自动注册到 SystemRegistry
        
        /// <summary>
        /// 系统是否已注册
        /// </summary>
        public bool IsRegistered { get; private set; } = false;
        
        protected virtual void Awake()
        {
            if (autoRegister)
            {
                RegisterToSystemRegistry();
            }
        }
        
        protected virtual void OnDestroy()
        {
            UnregisterFromSystemRegistry();
        }
        
        /// <summary>
        /// 注册到 SystemRegistry
        /// </summary>
        public void RegisterToSystemRegistry()
        {
            if (SystemRegistry.Instance != null)
            {
                SystemRegistry.Instance.RegisterSystem(this);
                IsRegistered = true;
            }
            else
            {
                // 延迟注册，等待 SystemRegistry 初始化
                StartCoroutine(WaitAndRegister());
            }
        }
        
        /// <summary>
        /// 从 SystemRegistry 注销
        /// </summary>
        public void UnregisterFromSystemRegistry()
        {
            if (IsRegistered && SystemRegistry.Instance != null)
            {
                // 使用反射获取具体类型进行注销
                var concreteType = this.GetType();
                var method = typeof(SystemRegistry).GetMethod("UnregisterSystem");
                var genericMethod = method.MakeGenericMethod(concreteType);
                genericMethod.Invoke(SystemRegistry.Instance, null);
                
                IsRegistered = false;
            }
        }
        
        /// <summary>
        /// 等待 SystemRegistry 初始化并注册
        /// </summary>
        private System.Collections.IEnumerator WaitAndRegister()
        {
            while (SystemRegistry.Instance == null)
            {
                yield return null;
            }
            
            SystemRegistry.Instance.RegisterSystem(this);
            IsRegistered = true;
        }
        
        /// <summary>
        /// 强制重新注册（用于调试）
        /// </summary>
        [ContextMenu("Force Re-register")]
        public void ForceReregister()
        {
            UnregisterFromSystemRegistry();
            RegisterToSystemRegistry();
        }
    }
}