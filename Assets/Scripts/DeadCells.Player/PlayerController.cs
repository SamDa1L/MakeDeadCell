using UnityEngine;

namespace DeadCells.Player
{
    /// <summary>
    /// Player controller handling movement, jumping and base interactions.
    /// Driven by a state machine to support tight 2D platforming behaviour.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : MonoBehaviour
    {
        public enum PlayerAnimationState
        {
            Idle = 0,
            Run = 1,
            Jump = 2,
            Fall = 3,
            Attack = 4,
            Roll = 5
        }

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float jumpForce = 16f;
        [SerializeField] private float coyoteTime = 0.2f;         // Time allowed to jump after leaving ground
        [SerializeField] private float jumpBufferTime = 0.1f;     // Time input is buffered before landing

        [Header("Ground Detection")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundLayerMask = 1;
        [SerializeField] private float groundCheckRadius = 0.2f;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField, Tooltip("Blend duration when switching animation states")]
        private float animationCrossFadeTime = 0.08f;

        private Rigidbody2D rb;
        private PlayerStateMachine stateMachine;
        private PlayerInput input;

        private bool isGrounded;
        private float coyoteTimeCounter;
        private float jumpBufferCounter;
        private bool facingRight = true;

        private PlayerAnimationState currentAnimationState = PlayerAnimationState.Idle;
        private static readonly string[] AnimationStateNames =
        {
            "Player_Idle",
            "Player_Run",
            "Player_Jump",
            "Player_Fall",
            "Player_Attack",
            "Player_Roll"
        };

        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int VerticalVelocityParam = Animator.StringToHash("VerticalVelocity");
        private static readonly int IsGroundedParam = Animator.StringToHash("IsGrounded");
        private static readonly int AttackTriggerParam = Animator.StringToHash("Attack");
        private static readonly int RollTriggerParam = Animator.StringToHash("Roll");

        #region Public accessors
        public bool IsGrounded => isGrounded;
        public float MoveSpeed => moveSpeed;
        public float JumpForce => jumpForce;
        public bool FacingRight => facingRight;
        public Rigidbody2D Rigidbody => rb;
        public PlayerInput Input => input;
        public Animator Animator => animator;
        #endregion

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
            }

            input = new PlayerInput();
            stateMachine = new PlayerStateMachine(this);
        }

        private void Start()
        {
            stateMachine.Initialize();
            PlayAnimation(PlayerAnimationState.Idle, 0f, true);
            UpdateAnimationParameters();
        }

        private void Update()
        {
            HandleInput();
            CheckGrounded();
            UpdateTimers();
            stateMachine.Update();
            UpdateAnimationParameters();
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
            {
                coyoteTimeCounter = coyoteTime;
            }
            else
            {
                coyoteTimeCounter -= Time.deltaTime;
            }

            if (input.JumpPressed)
            {
                jumpBufferCounter = jumpBufferTime;
            }
            else
            {
                jumpBufferCounter -= Time.deltaTime;
            }
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
            {
                Flip();
            }
            else if (direction < 0 && facingRight)
            {
                Flip();
            }
        }

        public void PlayAnimation(PlayerAnimationState state, float transitionDuration = -1f, bool forceRestart = false)
        {
            if (animator == null)
            {
                return;
            }

            if (transitionDuration < 0f)
            {
                transitionDuration = animationCrossFadeTime;
            }

            if (!forceRestart && currentAnimationState == state)
            {
                return;
            }

            currentAnimationState = state;
            string stateName = AnimationStateNames[(int)state];
            animator.CrossFadeInFixedTime(stateName, transitionDuration);
        }

        public void TriggerAttackAnimation()
        {
            if (animator == null)
            {
                return;
            }

            animator.ResetTrigger(AttackTriggerParam);
            animator.SetTrigger(AttackTriggerParam);
            PlayAnimation(PlayerAnimationState.Attack, animationCrossFadeTime, true);
        }

        public void TriggerRollAnimation()
        {
            if (animator == null)
            {
                return;
            }

            animator.ResetTrigger(RollTriggerParam);
            animator.SetTrigger(RollTriggerParam);
            PlayAnimation(PlayerAnimationState.Roll, animationCrossFadeTime, true);
        }

        private void UpdateAnimationParameters()
        {
            if (animator == null)
            {
                return;
            }

            animator.SetFloat(SpeedParam, Mathf.Abs(input.Horizontal));
            animator.SetFloat(VerticalVelocityParam, rb.velocity.y);
            animator.SetBool(IsGroundedParam, isGrounded);
        }

        private void Flip()
        {
            facingRight = !facingRight;
            transform.Rotate(0f, 180f, 0f);
        }

        private void OnLanded()
        {
            // Hook for landing VFX / SFX if needed
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