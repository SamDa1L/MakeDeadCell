using UnityEngine;

namespace DeadCells.Player.StateMachine
{
    /// <summary>
    /// 攀爬空闲状态行为
    /// 处理玩家抓住可攀爬物体但不移动的状态
    ///
    /// 职责：
    /// - OnEnter(): 禁用重力、锁定水平速度
    /// - OnExit(): 恢复重力
    /// - 物理处理：由PlayerController.HandleClimbPhysics()负责
    /// </summary>
    public class PlayerClimbIdleStateBehaviour : PlayerStateBehaviour
    {
        /// <summary>
        /// 进入攀爬空闲状态
        /// 一次性初始化：禁用重力，锁定水平速度
        ///
        /// 重要说明：攀爬时需要禁用重力，使角色可以稳定地抓住墙壁或绳子
        /// </summary>
        protected override void OnEnter()
        {
            // ✅ 禁用重力，使角色停留在攀爬物体上
            physicsController.SetGravityScale(0);

            // ✅ 锁定水平速度，攀爬时不允许水平移动
            physicsController.SetHorizontalVelocity(0);

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
            // - Speed > 0.1 → 转换到ClimbMove（攀爬移动）
        }

        /// <summary>
        /// 退出攀爬空闲状态
        /// 清理工作：恢复重力
        /// </summary>
        protected override void OnExit()
        {
            // ✅ 恢复重力缩放为正常值
            // 这样角色再次受到重力影响
            physicsController.SetGravityScale(config.GravityScale);

            // ✅ 清除攀爬标志
            animator.SetBool("IsClimbing", false);
        }
    }
}
