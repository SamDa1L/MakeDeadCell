using UnityEngine;

namespace DeadCells.Player.StateMachine
{
    /// <summary>
    /// 翻滚状态行为
    /// 处理玩家快速翻滚移动的状态
    ///
    /// 职责：
    /// - OnEnter(): 记录翻滚开始
    /// - 物理处理：由PlayerController.HandleRollPhysics()负责维持翻滚速度
    /// </summary>
    public class PlayerRollStateBehaviour : PlayerStateBehaviour
    {
        /// <summary>
        /// 进入翻滚状态
        /// 一次性初始化工作
        /// </summary>
        protected override void OnEnter()
        {
            // 翻滚初始化可以在此处添加
            // 例如：禁用某些碰撞反应、设置音效等
            // 翻滚速度的维持由PlayerController.HandleRollPhysics()处理
        }

        /// <summary>
        /// 状态更新（每逻辑帧调用）
        /// 可选的轻量级检查
        /// </summary>
        protected override void OnUpdate()
        {
            // 状态转换由Animator条件和动画时间处理
            // 动画完成后通过Has Exit Time和转换条件返回合适的状态
        }

        /// <summary>
        /// 退出翻滚状态
        /// 翻滚结束时的清理工作
        /// </summary>
        protected override void OnExit()
        {
            // 翻滚状态退出时的清理
            // 例如：恢复碰撞反应等
        }
    }
}
