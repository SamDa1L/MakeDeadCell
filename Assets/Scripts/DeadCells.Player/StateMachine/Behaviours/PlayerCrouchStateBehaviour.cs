using UnityEngine;

namespace DeadCells.Player.StateMachine
{
    /// <summary>
    /// 下蹲状态行为
    /// 处理玩家下蹲且不移动的状态
    ///
    /// 职责：
    /// - OnEnter(): 调整碰撞体尺寸，清空水平速度
    /// - OnExit(): 恢复碰撞体尺寸
    /// - 物理处理：由PlayerController.HandleCrouchPhysics()负责
    /// </summary>
    public class PlayerCrouchStateBehaviour : PlayerStateBehaviour
    {
        /// <summary>
        /// 进入下蹲状态
        /// 一次性初始化：
        /// 1. 调整碰撞体为下蹲大小
        /// 2. 停止水平移动
        /// </summary>
        protected override void OnEnter()
        {
            // ✅ 调整碰撞体为下蹲状态的较小尺寸
            // 这只需要执行一次，之后由PlayerController维持速度
            physicsController.ResizeCollider(
                config.CrouchColliderSize,
                config.CrouchColliderOffset);

            // ✅ 清空水平速度，下蹲时保持静止
            physicsController.SetHorizontalVelocity(0);

            // ✅ 通过Animator设置下蹲标志
            // 这用于Animator中的状态转换条件判断
            animator.SetBool("IsCrouching", true);
        }

        /// <summary>
        /// 状态更新（每逻辑帧调用）
        /// 检查转换条件或轻量级处理
        /// </summary>
        protected override void OnUpdate()
        {
            // 状态转换由Animator条件自动处理
            // 例如：松开下蹲键且有足够空间 → 转换到站立
            // HasHeadroom检查和状态转换在Animator中配置
        }

        /// <summary>
        /// 退出下蹲状态
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
