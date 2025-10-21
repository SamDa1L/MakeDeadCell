using UnityEngine;

namespace DeadCells.Player.StateMachine
{
    /// <summary>
    /// 攻击状态行为
    /// 处理玩家发动攻击的状态
    ///
    /// 职责：
    /// - OnEnter(): 触发攻击动画
    /// - OnExit(): 攻击完成后的清理
    /// - 物理处理：由PlayerController.HandleAttackPhysics()负责
    /// </summary>
    public class PlayerAttackStateBehaviour : PlayerStateBehaviour
    {
        /// <summary>
        /// 进入攻击状态
        /// 无需特殊初始化（动画已由Animator触发）
        /// </summary>
        protected override void OnEnter()
        {
            // 攻击动画已由Animator通过Attack trigger触发
            // 此处无需额外处理
        }

        /// <summary>
        /// 状态更新（每逻辑帧调用）
        /// 可选的轻量级检查
        /// </summary>
        protected override void OnUpdate()
        {
            // 状态转换由Animator条件和动画事件处理
            // 动画末帧应设置动画事件，通知PlayerController攻击完成
        }

        /// <summary>
        /// 退出攻击状态
        /// 无需特殊清理
        /// </summary>
        protected override void OnExit()
        {
            // 攻击状态退出时无需特殊清理
            // 转换到下一个状态（站立、下蹲或空中）由Animator条件处理
        }
    }
}
