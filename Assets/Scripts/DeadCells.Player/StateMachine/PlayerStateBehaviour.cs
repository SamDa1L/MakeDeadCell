using UnityEngine;

namespace DeadCells.Player.StateMachine
{
    /// <summary>
    /// StateMachineBehaviour 基类
    /// 所有玩家状态的Animator回调处理基类
    ///
    /// 重要说明：
    /// - ❌ 不存在 OnStateFixedUpdate() 回调，这是Unity 2022的限制
    /// - ✅ 物理逻辑应在 PlayerController.FixedUpdate() 中集中处理
    /// - 本类仅处理状态初始化和清理，具体物理由PlayerController转发
    /// </summary>
    public abstract class PlayerStateBehaviour : StateMachineBehaviour
    {
        // 玩家主控制器引用
        protected PlayerController playerController;

        // 玩家物理控制器引用
        protected PlayerPhysicsController physicsController;

        // 玩家输入系统引用
        protected PlayerInput input;

        // 玩家移动配置引用
        protected PlayerMovementConfig config;

        // Animator组件引用
        protected Animator animator;

        /// <summary>
        /// 状态进入回调
        /// 在Animator切换到此状态时调用（仅调用一次）
        ///
        /// 职责：一次性初始化
        /// - 调整碰撞体尺寸（如进入下蹲状态）
        /// - 改变重力缩放（如进入攀爬状态）
        /// - 清空速度（如某些特定状态）
        /// - 设置标志或缓存数据
        /// </summary>
        public sealed override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // 首次初始化时缓存所有组件引用
            if (playerController == null)
            {
                this.animator = animator;
                playerController = animator.GetComponent<PlayerController>();
                physicsController = animator.GetComponent<PlayerPhysicsController>();
                input = playerController.Input;
                config = playerController.MovementConfig;
            }

            // 调用子类的自定义进入逻辑
            OnEnter();
        }

        /// <summary>
        /// 状态更新回调
        /// 在逻辑帧中调用（每帧）
        ///
        /// 职责：轻量级状态逻辑
        /// - 检查状态转换条件（可选）
        /// - 动画事件的轻量级处理
        /// - 不处理持续的物理更新
        /// </summary>
        public sealed override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // 调用子类的自定义更新逻辑
            OnUpdate();
        }

        /// <summary>
        /// 状态移动回调
        /// 在Animator评估期间调用
        ///
        /// ⚠️ 重要：此回调用于根运动（Root Motion）处理
        /// ❌ 不要在此处放置任何物理逻辑 - 物理由 PlayerController.FixedUpdate() 集中处理
        /// 此项目暂不使用根运动，保留此方法以供未来扩展
        /// </summary>
        public sealed override void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // 仅为根运动预留，当前项目不使用
            OnPhysicsMove();
        }

        /// <summary>
        /// 状态退出回调
        /// 在Animator离开此状态时调用（仅调用一次）
        ///
        /// 职责：状态清理
        /// - 恢复碰撞体尺寸（如从下蹲恢复）
        /// - 恢复重力缩放（如从攀爬恢复）
        /// - 清理临时标志
        /// </summary>
        public sealed override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // 调用子类的自定义退出逻辑
            OnExit();
        }

        #region 子类可覆写的虚方法

        /// <summary>
        /// 状态进入时调用（子类覆写）
        /// 实现一次性初始化逻辑
        /// </summary>
        protected virtual void OnEnter() { }

        /// <summary>
        /// 状态更新时调用（子类覆写）
        /// 实现轻量级逻辑判断
        /// </summary>
        protected virtual void OnUpdate() { }

        /// <summary>
        /// 根运动相关回调（子类覆写）
        /// 当项目需要使用根运动时实现此方法
        /// 当前项目不使用，保留以供未来扩展
        /// </summary>
        protected virtual void OnPhysicsMove() { }

        /// <summary>
        /// 状态退出时调用（子类覆写）
        /// 实现状态清理逻辑
        /// </summary>
        protected virtual void OnExit() { }

        #endregion
    }
}
