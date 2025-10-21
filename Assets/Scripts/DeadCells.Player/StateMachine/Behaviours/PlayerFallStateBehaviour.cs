using UnityEngine;

namespace DeadCells.Player.StateMachine
{
    /// <summary>
    /// 下落状态行为
    /// 处理玩家下落（竖直速度为负）的状态
    ///
    /// 职责：
    /// - 无需OnEnter初始化
    /// - 物理处理：由PlayerController.HandleFallPhysics()负责
    /// </summary>
    public class PlayerFallStateBehaviour : PlayerStateBehaviour
    {
        /// <summary>
        /// 进入下落状态
        /// 无需特殊初始化，重力由Rigidbody2D持续作用
        /// </summary>
        protected override void OnEnter()
        {
            // 下落状态无需特殊初始化
            // Rigidbody2D的重力缩放已为正常值
            // 重力会持续作用于角色
        }

        /// <summary>
        /// 状态更新（每逻辑帧调用）
        /// 此处不做处理，落地检测由PlayerController.FixedUpdate()处理
        /// </summary>
        protected override void OnUpdate()
        {
            // 状态转换由Animator条件自动处理：
            // - IsGrounded == true && IsCrouching == false → 转换到Grounded_Locomotion
            // - IsGrounded == true && IsCrouching == true → 转换到Crouch
        }

        /// <summary>
        /// 退出下落状态
        /// 无需特殊清理
        /// </summary>
        protected override void OnExit()
        {
            // 下落状态退出时无需特殊清理
            // 回到地面状态时由对应状态初始化
        }
    }
}
