using UnityEngine;

namespace DeadCells.Player.StateMachine
{
    /// <summary>
    /// 跳跃状态行为
    /// 处理玩家向上运动的跳跃状态
    ///
    /// 职责：
    /// - OnEnter(): 应用跳跃力
    /// - 物理处理：由PlayerController.HandleJumpPhysics()负责
    /// </summary>
    public class PlayerJumpStateBehaviour : PlayerStateBehaviour
    {
        /// <summary>
        /// 进入跳跃状态
        /// 一次性初始化：应用跳跃力
        ///
        /// ⚠️ 重要：
        /// - 跳跃力只在状态进入时应用一次
        /// - 不在FixedUpdate中重复应用，否则角色会持续加速上升
        /// - 空中移动控制由PlayerController.HandleJumpPhysics()在FixedUpdate中处理
        /// </summary>
        protected override void OnEnter()
        {
            // ✅ 应用跳跃力（一次性）
            // 这设置竖直速度为跳跃力度值
            physicsController.ApplyJump(config.JumpForce);

            // ✅ 设置Animator参数用于转换条件判断
            // 这确保Animator知道角色已跳起
        }

        /// <summary>
        /// 状态更新（每逻辑帧调用）
        /// 此处不做处理，转换条件由Animator自动处理
        /// </summary>
        protected override void OnUpdate()
        {
            // 状态转换由Animator条件自动处理：
            // - VerticalVelocity <= 0 → 转换到Fall（下落状态）
        }

        /// <summary>
        /// 退出跳跃状态
        /// 无需特殊清理
        /// </summary>
        protected override void OnExit()
        {
            // 跳跃状态退出时无需特殊清理
            // 重力由Rigidbody2D持续作用
        }
    }
}
