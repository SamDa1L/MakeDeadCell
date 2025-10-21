using UnityEngine;

namespace DeadCells.Player.StateMachine
{
    /// <summary>
    /// 地面移动状态行为
    /// 处理玩家在地面上的Idle、Walk、Run状态
    ///
    /// 职责：
    /// - OnEnter(): 无需特殊初始化（动画通过Speed参数的BlendTree自动切换）
    /// - OnUpdate(): 可选的轻量级检查
    /// - 物理处理：由PlayerController.HandleLocomotionPhysics()负责
    /// </summary>
    public class PlayerLocomotionStateBehaviour : PlayerStateBehaviour
    {
        /// <summary>
        /// 进入地面移动状态
        /// 无需特殊初始化，速度由Animator的Speed参数驱动BlendTree实现自动切换
        /// </summary>
        protected override void OnEnter()
        {
            // 地面移动状态无需特殊初始化
            // 动画切换由Animator的Speed参数值驱动BlendTree完成
            // 具体流程：
            // Speed = 0 → 播放Idle
            // Speed = 0.4 → 播放Walk
            // Speed = 1 → 播放Run
            // 中间值会自动混合
        }

        /// <summary>
        /// 状态更新（每逻辑帧调用）
        /// 这里可以添加轻量级检查，但状态转换由Animator条件自动处理
        /// </summary>
        protected override void OnUpdate()
        {
            // 状态转换逻辑已在Animator中通过参数条件自动处理
            // 此处为空，便于子类扩展
        }

        /// <summary>
        /// 退出地面移动状态
        /// 无需清理工作
        /// </summary>
        protected override void OnExit()
        {
            // 地面移动状态退出时无需清理工作
        }
    }
}
