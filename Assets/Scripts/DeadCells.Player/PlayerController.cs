using UnityEngine;

namespace DeadCells.Player
{
    /// <summary>
    /// 玩家控制器 - 处理玩家的移动、跳跃和基础交互
    /// 采用状态机模式管理玩家行为，支持精确的2D平台跳跃
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 8f;            // 水平移动速度
        [SerializeField] private float jumpForce = 16f;           // 跳跃力度
        [SerializeField] private float coyoteTime = 0.2f;         // 悬崖时间 - 离开地面后仍可跳跃的时间
        [SerializeField] private float jumpBufferTime = 0.1f;     // 跳跃缓冲 - 提前按跳跃键的有效时间
        
        [Header("Ground Detection")]
        [SerializeField] private Transform groundCheck;           // 地面检测点
        [SerializeField] private LayerMask groundLayerMask = 1;   // 地面图层遮罩
        [SerializeField] private float groundCheckRadius = 0.2f;  // 地面检测半径
        
        // 组件引用
        private Rigidbody2D rb;                    // 刚体组件
        private PlayerStateMachine stateMachine;   // 玩家状态机
        private PlayerInput input;                 // 输入处理器
        
        // 移动相关变量
        private bool isGrounded;                   // 是否在地面上
        private float coyoteTimeCounter;           // 悬崖时间计数器
        private float jumpBufferCounter;           // 跳跃缓冲计数器
        private bool facingRight = true;           // 是否面向右侧
        
        #region 属性访问器
        /// <summary>是否在地面上</summary>
        public bool IsGrounded => isGrounded;
        /// <summary>移动速度</summary>
        public float MoveSpeed => moveSpeed;
        /// <summary>跳跃力度</summary>
        public float JumpForce => jumpForce;
        /// <summary>是否面向右侧</summary>
        public bool FacingRight => facingRight;
        /// <summary>刚体组件</summary>
        public Rigidbody2D Rigidbody => rb;
        /// <summary>输入处理器</summary>
        public PlayerInput Input => input;
        #endregion
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            input = new PlayerInput();
            stateMachine = new PlayerStateMachine(this);
        }
        
        private void Start()
        {
            stateMachine.Initialize();
        }
        
        private void Update()
        {
            HandleInput();
            CheckGrounded();
            UpdateTimers();
            stateMachine.Update();
        }
        
        private void FixedUpdate()
        {
            stateMachine.FixedUpdate();
        }
        
        private void HandleInput()
        {
            input.Update();
        }
        
        private void CheckGrounded()
        {
            bool wasGrounded = isGrounded;
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayerMask);
            
            if (!wasGrounded && isGrounded)
            {
                OnLanded();
            }
        }
        
        private void UpdateTimers()
        {
            if (isGrounded)
                coyoteTimeCounter = coyoteTime;
            else
                coyoteTimeCounter -= Time.deltaTime;
                
            if (input.JumpPressed)
                jumpBufferCounter = jumpBufferTime;
            else
                jumpBufferCounter -= Time.deltaTime;
        }
        
        public bool CanJump()
        {
            return coyoteTimeCounter > 0 && jumpBufferCounter > 0;
        }
        
        public void Jump()
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpBufferCounter = 0;
            coyoteTimeCounter = 0;
        }
        
        public void Move(float direction)
        {
            rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
            
            if (direction > 0 && !facingRight)
                Flip();
            else if (direction < 0 && facingRight)
                Flip();
        }
        
        private void Flip()
        {
            facingRight = !facingRight;
            transform.Rotate(0f, 180f, 0f);
        }
        
        private void OnLanded()
        {
            // Handle landing effects, sound, etc.
        }
        
        private void OnDrawGizmos()
        {
            if (groundCheck != null)
            {
                Gizmos.color = isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }
        }
    }
}