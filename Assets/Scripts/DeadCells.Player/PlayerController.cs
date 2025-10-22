using UnityEngine;
using DeadCells.Player.StateMachine;

namespace DeadCells.Player
{
    /// <summary>
    /// 玩家主控制器
    /// 职责：输入采集、参数更新、物理转发
    ///
    /// 架构说明（Animator驱动）：
    /// - Update(): 采集输入，更新Animator参数
    /// - Animator: 根据参数自动转换状态，触发SMB回调
    /// - StateMachineBehaviour: 一次性初始化/清理
    /// - FixedUpdate(): 根据Animator当前状态转发物理逻辑
    ///
    /// 关键原则：
    /// - ❌ 不再调用 PlayerStateMachine.Update/FixedUpdate()
    /// - ✅ 状态由Animator维护，SMB处理初始化/清理
    /// - ✅ 物理逻辑在FixedUpdate中集中分派
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    [RequireComponent(typeof(PlayerPhysicsController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("移动设置")]
        [SerializeField] private PlayerMovementConfig movementConfig;

        [Header("地面检测")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundLayerMask = 1;

        [Header("动画")]
        [SerializeField] private Animator animator;

        // 核心组件引用
        private Rigidbody2D rb;
        private Collider2D coll2D;
        private PlayerInput input;
        private PlayerPhysicsController physicsController;

        // 状态追踪
        private bool isGrounded;
        private float coyoteTimeCounter;
        private float jumpBufferCounter;
        private bool facingRight = true;

        // 缓存 Animator 状态哈希值（平层架构）
        // ⚠️ 重要：所有状态都在 Base Layer 顶层，使用 shortNameHash
        // 格式：简单状态名称
        private static readonly int IDLE_HASH = Animator.StringToHash("Idle");
        private static readonly int WALK_HASH = Animator.StringToHash("Walk");
        private static readonly int RUN_HASH = Animator.StringToHash("Run");
        private static readonly int CROUCH_HASH = Animator.StringToHash("Crouch");
        private static readonly int CROUCH_WALK_HASH = Animator.StringToHash("CrouchWalk");
        private static readonly int JUMP_HASH = Animator.StringToHash("Jump");
        private static readonly int FALL_HASH = Animator.StringToHash("Fall");
        private static readonly int ATTACK_HASH = Animator.StringToHash("Attack");
        private static readonly int ROLL_HASH = Animator.StringToHash("Roll");
        private static readonly int CLIMB_IDLE_HASH = Animator.StringToHash("ClimbIdle");
        private static readonly int CLIMB_MOVE_HASH = Animator.StringToHash("ClimbMove");

        // 缓存 Animator 触发器参数哈希值
        private static readonly int CROUCH_INPUT_TRIGGER = Animator.StringToHash("TryCrouch");
        private static readonly int CROUCH_RELEASE_TRIGGER = Animator.StringToHash("ReleaseCrouch");

        #region 公开属性访问器

        /// <summary>
        /// 角色是否面向右侧
        /// </summary>
        public bool FacingRight => facingRight;

        /// <summary>
        /// Rigidbody2D 组件引用
        /// </summary>
        public Rigidbody2D Rigidbody => rb;

        /// <summary>
        /// Animator 组件引用
        /// </summary>
        public Animator Animator => animator;

        /// <summary>
        /// 玩家输入系统引用
        /// </summary>
        public PlayerInput Input => input;

        /// <summary>
        /// 玩家是否接触地面
        /// </summary>
        public bool IsGrounded => isGrounded;

        /// <summary>
        /// 玩家移动配置引用
        /// </summary>
        public PlayerMovementConfig MovementConfig => movementConfig;

        #endregion

        private void Awake()
        {
            // 获取 Rigidbody2D 组件
            rb = GetComponent<Rigidbody2D>();

            // 获取 Collider2D 组件
            coll2D = GetComponent<Collider2D>();

            // 如果Inspector中未赋值，则尝试获取Animator
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
            }

            // 创建玩家输入系统实例
            input = new PlayerInput();

            // 如果未赋值配置，使用默认配置
            if (movementConfig == null)
            {
                movementConfig = ScriptableObject.CreateInstance<PlayerMovementConfig>();
            }

            // 获取物理控制器，必须已在GameObject上添加
            physicsController = GetComponent<PlayerPhysicsController>();
            if (physicsController == null)
            {
                Debug.LogError("PlayerPhysicsController 未找到！请确保已在 Inspector 中添加该组件。");
            }
        }

        private void Start()
        {
            // 在新的Animator驱动架构中
            // 不需要调用状态机初始化
            // Animator会自动从Entry过渡到初始状态
        }

        private void Update()
        {
            // 更新输入系统
            input.Update();

            // 检查地面状态
            CheckGrounded();

            // 更新计时器（coyote time，jump buffer）
            UpdateTimers();

            // 更新Animator参数，驱动状态转换
            UpdateAnimatorParameters();
        }

        private void FixedUpdate()
        {
            // 根据Animator当前状态转发物理处理
            // 这是新架构的核心：集中式物理转发
            HandlePhysicsForCurrentState();
        }

        /// <summary>
        /// 检查玩家是否接触地面
        /// 每帧调用一次，检测接地状态变化
        /// </summary>
        private void CheckGrounded()
        {
            bool wasGrounded = isGrounded;

            // 通过OverlapCircle检测地面，在GroundCheck位置检测
            isGrounded = groundCheck != null &&
                         Physics2D.OverlapCircle(groundCheck.position, movementConfig.GroundCheckRadius, groundLayerMask);

            // 如果从空中落地，触发OnLanded回调
            if (!wasGrounded && isGrounded)
            {
                OnLanded();
            }
        }

        /// <summary>
        /// 更新Coyote时间和跳跃缓冲计时器
        ///
        /// Coyote时间：允许玩家在离开地面后短时间内仍可跳跃
        /// 跳跃缓冲：允许玩家在未接地时提前按下跳跃键，接地后仍可跳跃
        /// </summary>
        private void UpdateTimers()
        {
            // 更新 Coyote 时间计数器
            if (isGrounded)
            {
                // 在地面上时重置Coyote时间
                coyoteTimeCounter = movementConfig.CoyoteTime;
            }
            else
            {
                // 离开地面时持续减少Coyote时间
                coyoteTimeCounter -= Time.deltaTime;
            }

            // 更新 Jump Buffer 计数器
            if (input.JumpPressed)
            {
                // 按下跳跃键时重置Jump Buffer
                jumpBufferCounter = movementConfig.JumpBufferTime;
            }
            else
            {
                // 没有按下跳跃键时持续减少Jump Buffer
                jumpBufferCounter -= Time.deltaTime;
            }
        }

        /// <summary>
        /// 更新Animator参数，驱动状态机
        /// 每帧Update中调用，将输入和状态转换为Animator参数
        /// </summary>
        private void UpdateAnimatorParameters()
        {
            // 计算归一化速度（0-1）用于BlendTree切换Idle/Walk/Run
            float normalizedSpeed = Mathf.Abs(rb.velocity.x) / (movementConfig.RunSpeed > 0 ? movementConfig.RunSpeed : 1f);
            animator.SetFloat("Speed", normalizedSpeed);

            // 设置竖直速度用于Jump/Fall状态转换判断
            animator.SetFloat("VerticalVelocity", rb.velocity.y);

            // 设置接地状态
            animator.SetBool("IsGrounded", isGrounded);

            // 检查是否可以跳跃（Coyote时间 + Jump Buffer）
            bool canJump = coyoteTimeCounter > 0 && jumpBufferCounter > 0;

            // 处理跳跃输入
            if (canJump && input.JumpPressed)
            {
                // 触发Jump动画
                animator.SetTrigger("Jump");

                // 消耗Coyote时间和Jump Buffer
                coyoteTimeCounter = 0;
                jumpBufferCounter = 0;
            }

            // 处理攻击输入
            if (input.AttackPressed)
            {
                // 触发Attack动画
                animator.SetTrigger("Attack");
            }

            // 处理翻滚输入
            if (input.RollPressed)
            {
                // 触发Roll动画
                animator.SetTrigger("Roll");
            }

            // 处理下蹲输入
            // ✅ 使用沿处理：仅在按下/松开时触发Trigger，避免每帧重复触发
            // TryCrouch：玩家按下C键时触发（GetKeyDown）
            // ReleaseCrouch：玩家松开C键时触发（GetKeyUp）
            if (input.CrouchPressed)
            {
                // 只在按下瞬间触发一次
                animator.SetTrigger(CROUCH_INPUT_TRIGGER);
            }

            if (input.CrouchReleased)
            {
                // 只在松开瞬间触发一次
                animator.SetTrigger(CROUCH_RELEASE_TRIGGER);
            }

            // ⚠️ IsCrouching 和 IsClimbing 由对应的StateMachineBehaviour设置
            // 不在此处设置
        }

        /// <summary>
        /// 根据Animator当前状态转发物理处理
        /// 这是集中式物理转发的核心
        ///
        /// ⚠️ 重要：处理Animator状态转换期间的物理
        /// 在淡入淡出(Blend)期间可能同时处于两个状态，需检查转换
        /// 使用 fullPathHash 进行精确的状态识别
        /// </summary>
        private void HandlePhysicsForCurrentState()
        {
            // 获取当前Animator状态信息（平层架构，所有状态都在顶层）
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            // 使用 shortNameHash 因为所有状态名称在顶层是唯一的
            int currentStateHash = currentState.shortNameHash;

            // 按优先级检查状态
            if (currentStateHash == JUMP_HASH)
            {
                HandleJumpPhysics();
            }
            else if (currentStateHash == FALL_HASH)
            {
                HandleFallPhysics();
            }
            else if (currentStateHash == ATTACK_HASH)
            {
                HandleAttackPhysics();
            }
            else if (currentStateHash == ROLL_HASH)
            {
                HandleRollPhysics();
            }
            else if (currentStateHash == CROUCH_HASH)
            {
                HandleCrouchPhysics();
            }
            else if (currentStateHash == CROUCH_WALK_HASH)
            {
                HandleCrouchWalkPhysics();
            }
            else if (currentStateHash == CLIMB_IDLE_HASH || currentStateHash == CLIMB_MOVE_HASH)
            {
                HandleClimbPhysics(currentStateHash == CLIMB_MOVE_HASH);
            }
            else if (currentStateHash == IDLE_HASH || currentStateHash == WALK_HASH || currentStateHash == RUN_HASH)
            {
                HandleLocomotionPhysics();  // 处理所有地面移动状态
            }
        }

        /// <summary>
        /// 地面移动状态物理处理
        /// 应用水平速度，处理方向翻转
        /// </summary>
        private void HandleLocomotionPhysics()
        {
            // 获取水平输入方向
            float moveDirection = input.Horizontal;

            // 计算目标速度（有输入时为跑步速度，无输入时为0）
            float targetSpeed = moveDirection != 0 ? movementConfig.RunSpeed : 0;

            // 应用水平速度
            physicsController.SetHorizontalVelocity(moveDirection * targetSpeed);

            // 处理翻转 - 根据移动方向调整角色朝向
            // ⚠️ 重要：本地的 facingRight 字段与 physicsController.FacingRight 必须始终一致
            if (moveDirection > 0 && !physicsController.FacingRight)
            {
                physicsController.Flip();
                facingRight = physicsController.FacingRight;  // 同步本地状态
            }
            else if (moveDirection < 0 && physicsController.FacingRight)
            {
                physicsController.Flip();
                facingRight = physicsController.FacingRight;  // 同步本地状态
            }
        }

        /// <summary>
        /// 下蹲状态物理处理
        /// 不按方向键时保持静止，按方向键时以下蹲速度移动（触发到CrouchWalk转换）
        /// </summary>
        private void HandleCrouchPhysics()
        {
            float moveDirection = input.Horizontal;

            if (Mathf.Abs(moveDirection) < 0.01f)
            {
                // 没有水平输入：保持静止
                physicsController.SetHorizontalVelocity(0f);
            }
            else
            {
                // 有水平输入：应用下蹲移动速度
                // 这会让 Speed 参数升高，满足 Crouch → CrouchWalk 的转换条件（IsCrouching && Speed > 0.1）
                physicsController.SetHorizontalVelocity(
                    moveDirection * movementConfig.CrouchSpeed);
            }
        }

        /// <summary>
        /// 下蹲行走状态物理处理
        /// 允许以较低速度移动
        /// </summary>
        private void HandleCrouchWalkPhysics()
        {
            // 获取水平输入方向
            float moveDirection = input.Horizontal;

            // 以下蹲速度移动
            physicsController.SetHorizontalVelocity(moveDirection * movementConfig.CrouchSpeed);

            // 处理翻转 - 必须在每次Flip后同步 facingRight，否则下一帧会反复翻转
            if (moveDirection > 0 && !physicsController.FacingRight)
            {
                physicsController.Flip();
                facingRight = physicsController.FacingRight;  // ⚠️ 关键：立刻同步
            }
            else if (moveDirection < 0 && physicsController.FacingRight)
            {
                physicsController.Flip();
                facingRight = physicsController.FacingRight;  // ⚠️ 关键：立刻同步
            }
        }

        /// <summary>
        /// 跳跃状态物理处理
        /// 允许空中移动控制
        /// </summary>
        private void HandleJumpPhysics()
        {
            // 获取水平输入方向
            float moveDirection = input.Horizontal;

            // 允许在空中进行移动控制，速度为跑步速度的80%
            physicsController.SetHorizontalVelocity(moveDirection * movementConfig.RunSpeed * 0.8f);

            // 处理翻转 - 必须在每次Flip后同步 facingRight
            if (moveDirection > 0 && !physicsController.FacingRight)
            {
                physicsController.Flip();
                facingRight = physicsController.FacingRight;  // ⚠️ 关键：立刻同步
            }
            else if (moveDirection < 0 && physicsController.FacingRight)
            {
                physicsController.Flip();
                facingRight = physicsController.FacingRight;  // ⚠️ 关键：立刻同步
            }
        }

        /// <summary>
        /// 下落状态物理处理
        /// 允许下落过程中的水平移动
        /// </summary>
        private void HandleFallPhysics()
        {
            // 获取水平输入方向
            float moveDirection = input.Horizontal;

            // 允许下落时进行移动控制，速度为跑步速度的80%
            physicsController.SetHorizontalVelocity(moveDirection * movementConfig.RunSpeed * 0.8f);

            // 处理翻转 - 必须在每次Flip后同步 facingRight
            if (moveDirection > 0 && !physicsController.FacingRight)
            {
                physicsController.Flip();
                facingRight = physicsController.FacingRight;  // ⚠️ 关键：立刻同步
            }
            else if (moveDirection < 0 && physicsController.FacingRight)
            {
                physicsController.Flip();
                facingRight = physicsController.FacingRight;  // ⚠️ 关键：立刻同步
            }
        }

        /// <summary>
        /// 攻击状态物理处理
        /// 攻击期间允许有限的空中控制
        /// </summary>
        private void HandleAttackPhysics()
        {
            // 仅在空中时允许少量移动控制
            if (!isGrounded)
            {
                float moveDirection = input.Horizontal;
                physicsController.SetHorizontalVelocity(moveDirection * movementConfig.RunSpeed * 0.5f);
            }
        }

        /// <summary>
        /// 翻滚状态物理处理
        /// 维持固定的翻滚速度
        /// </summary>
        private void HandleRollPhysics()
        {
            // 根据角色面向方向确定翻滚方向
            float rollDirection = facingRight ? 1f : -1f;

            // 维持翻滚速度
            physicsController.SetHorizontalVelocity(rollDirection * movementConfig.RollSpeed);
        }

        /// <summary>
        /// 攀爬状态物理处理
        /// 处理垂直移动
        /// </summary>
        /// <param name="isMoving">是否在移动状态（true=ClimbMove，false=ClimbIdle）</param>
        private void HandleClimbPhysics(bool isMoving)
        {
            if (isMoving)
            {
                // 在攀爬移动状态，设置竖直速度
                // 使用 ClimbAxis 获取竖直输入（W/UpArrow或Vertical轴）
                float climbAxis = input.ClimbAxis;
                physicsController.SetVerticalVelocity(climbAxis * movementConfig.ClimbSpeed);
            }
            else
            {
                // 在攀爬空闲状态，竖直速度为0
                // 这里不需要显式设置，重力已在SMB中禁用
            }
        }

        /// <summary>
        /// 角色翻转，改变朝向
        /// </summary>
        public void Flip()
        {
            facingRight = !facingRight;
            transform.Rotate(0f, 180f, 0f);
        }

        /// <summary>
        /// 玩家落地时调用
        /// 可用于播放落地特效或音效
        /// </summary>
        private void OnLanded()
        {
            // 落地回调，可在此处添加特效/音效处理
            // 例如：粒子特效、落地声音等
        }

        /// <summary>
        /// 绘制地面检测范围的Gizmo
        /// 用于调试目的，在Scene视图中显示
        /// </summary>
        private void OnDrawGizmos()
        {
            if (groundCheck != null && movementConfig != null)
            {
                // 检查当前是否接地
                bool isCurrentlyGrounded = Physics2D.OverlapCircle(
                    groundCheck.position,
                    movementConfig.GroundCheckRadius,
                    groundLayerMask);

                // 根据接地状态设置Gizmo颜色
                Gizmos.color = isCurrentlyGrounded ? Color.green : Color.red;

                // 在GroundCheck位置绘制检测范围的圆形
                Gizmos.DrawWireSphere(groundCheck.position, movementConfig.GroundCheckRadius);
            }
        }
    }
}
