using UnityEngine;

namespace DeadCells.Player.StateMachine
{
    /// <summary>
    /// 下蹲行走状态行为
    /// 处理玩家下蹲且移动的状态
    ///
    /// 职责：
    /// - OnEnter(): 调整碰撞体尺寸
    /// - OnExit(): 恢复碰撞体尺寸
    /// - 物理处理：由PlayerController.HandleCrouchWalkPhysics()负责
    /// </summary>
    public class PlayerCrouchWalkStateBehaviour : PlayerStateBehaviour
    {
        /// <summary>
        /// 进入下蹲行走状态
        /// 一次性初始化：调整碰撞体为下蹲大小
        /// </summary>
        protected override void OnEnter()
        {
            // ✅ 调整碰撞体为下蹲状态的较小尺寸
            physicsController.ResizeCollider(
                config.CrouchColliderSize,
                config.CrouchColliderOffset);

            // ✅ 通过Animator设置下蹲标志
            animator.SetBool("IsCrouching", true);
        }

        /// <summary>
        /// 状态更新（每逻辑帧调用）
        /// 可选的轻量级检查
        /// </summary>
        protected override void OnUpdate()
        {
            // 状态转换由Animator条件自动处理：
            // - Speed <= 0.1 → 转换到Crouch（下蹲静止）
            // - IsCrouching = false → 转换到Grounded_Locomotion（站立移动）
        }

        /// <summary>
        /// 退出下蹲行走状态
        /// 清理工作：恢复碰撞体尺寸
        /// </summary>
        protected override void OnExit()
        {
            // ✅ 恢复碰撞体到原始（站立）大小
            physicsController.RestoreColliderSize();

            // ✅ 清除下蹲标志
            animator.SetBool("IsCrouching", false);
        }
    }
}
