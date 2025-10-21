using UnityEngine;

namespace DeadCells.Player.StateMachine
{
    /// <summary>
    /// 攀爬移动状态行为
    /// 处理玩家沿着可攀爬物体上下移动的状态
    ///
    /// 职责：
    /// - OnEnter(): 禁用重力
    /// - 物理处理：由PlayerController.HandleClimbPhysics()负责竖直速度
    /// </summary>
    public class PlayerClimbMoveStateBehaviour : PlayerStateBehaviour
    {
        /// <summary>
        /// 进入攀爬移动状态
        /// 一次性初始化：禁用重力
        ///
        /// 重要说明：
        /// - 重力必须在攀爬时禁用
        /// - 竖直移动由PlayerController.HandleClimbPhysics()处理
        /// </summary>
        protected override void OnEnter()
        {
            // ✅ 禁用重力，使角色可以沿着墙壁移动
            // 而不会因重力下落
            physicsController.SetGravityScale(0);

            // ✅ 通过Animator设置攀爬标志
            animator.SetBool("IsClimbing", true);
        }

        /// <summary>
        /// 状态更新（每逻辑帧调用）
        /// 检查转换条件
        /// </summary>
        protected override void OnUpdate()
        {
            // 状态转换由Animator条件自动处理：
            // - IsClimbing = false → 离开攀爬状态
            // - Speed <= 0.1 → 转换到ClimbIdle（攀爬空闲）
        }

        /// <summary>
        /// 退出攀爬移动状态
        /// 清理工作：恢复重力
        /// </summary>
        protected override void OnExit()
        {
            // ✅ 恢复重力缩放为正常值
            physicsController.SetGravityScale(config.GravityScale);

            // ✅ 清除攀爬标志
            animator.SetBool("IsClimbing", false);
        }
    }
}
