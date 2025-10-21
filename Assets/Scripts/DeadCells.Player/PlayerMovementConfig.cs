using UnityEngine;

namespace DeadCells.Player
{
    /// <summary>
    /// 玩家移动配置
    /// 集中管理所有玩家运动相关的参数
    /// 包括速度、跳跃、攻击、碰撞体、物理等设置
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerMovementConfig", menuName = "DeadCells/Player/Movement Config")]
    public class PlayerMovementConfig : ScriptableObject
    {
        [Header("移动速度")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float runSpeed = 8f;
        [SerializeField] private float crouchSpeed = 2f;
        [SerializeField] private float climbSpeed = 5f;

        [Header("跳跃设置")]
        [SerializeField] private float jumpForce = 16f;
        [SerializeField] private float coyoteTime = 0.2f;
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("翻滚设置")]
        [SerializeField] private float rollSpeed = 12f;
        [SerializeField] private float rollDuration = 0.4f;

        [Header("攻击设置")]
        [SerializeField] private float attackDuration = 0.3f;

        [Header("碰撞体配置")]
        [SerializeField] private Vector2 normalColliderSize = new Vector2(0.6f, 1.2f);
        [SerializeField] private Vector2 normalColliderOffset = Vector2.zero;
        [SerializeField] private Vector2 crouchColliderSize = new Vector2(0.6f, 0.6f);
        [SerializeField] private Vector2 crouchColliderOffset = new Vector2(0f, -0.3f);

        [Header("地面检测")]
        [SerializeField] private float groundCheckRadius = 0.2f;

        [Header("物理设置")]
        [SerializeField] private float defaultGravityScale = 3f;  // 默认重力缩放，用于攀爬时恢复

        #region 属性访问器

        /// <summary>
        /// 行走速度
        /// </summary>
        public float WalkSpeed => walkSpeed;

        /// <summary>
        /// 跑步速度
        /// </summary>
        public float RunSpeed => runSpeed;

        /// <summary>
        /// 下蹲移动速度
        /// </summary>
        public float CrouchSpeed => crouchSpeed;

        /// <summary>
        /// 攀爬速度
        /// </summary>
        public float ClimbSpeed => climbSpeed;

        /// <summary>
        /// 跳跃力度
        /// </summary>
        public float JumpForce => jumpForce;

        /// <summary>
        /// Coyote时间：离开地面后仍可跳跃的时间
        /// </summary>
        public float CoyoteTime => coyoteTime;

        /// <summary>
        /// 跳跃缓冲时间：提前按下跳跃键后仍可跳跃的时间
        /// </summary>
        public float JumpBufferTime => jumpBufferTime;

        /// <summary>
        /// 翻滚速度
        /// </summary>
        public float RollSpeed => rollSpeed;

        /// <summary>
        /// 翻滚持续时间
        /// </summary>
        public float RollDuration => rollDuration;

        /// <summary>
        /// 攻击持续时间
        /// </summary>
        public float AttackDuration => attackDuration;

        /// <summary>
        /// 正常状态下的碰撞体尺寸
        /// </summary>
        public Vector2 NormalColliderSize => normalColliderSize;

        /// <summary>
        /// 正常状态下的碰撞体偏移
        /// </summary>
        public Vector2 NormalColliderOffset => normalColliderOffset;

        /// <summary>
        /// 下蹲状态下的碰撞体尺寸
        /// </summary>
        public Vector2 CrouchColliderSize => crouchColliderSize;

        /// <summary>
        /// 下蹲状态下的碰撞体偏移
        /// </summary>
        public Vector2 CrouchColliderOffset => crouchColliderOffset;

        /// <summary>
        /// 地面检测的圆形范围半径
        /// </summary>
        public float GroundCheckRadius => groundCheckRadius;

        /// <summary>
        /// 默认重力缩放值
        /// 用于从攀爬、悬挂等特殊状态恢复到正常状态
        /// 值=1为Rigidbody2D的标准重力，值越大下落越快
        /// </summary>
        public float GravityScale => defaultGravityScale;

        #endregion
    }
}
