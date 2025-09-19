using UnityEngine;

namespace DeadCellsTestFramework.Animation
{
    [RequireComponent(typeof(Animator))]
    public class AnimationController : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        
        private readonly int speedHash = Animator.StringToHash("Speed");
        private readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
        private readonly int isJumpingHash = Animator.StringToHash("IsJumping");
        private readonly int isFallingHash = Animator.StringToHash("IsFalling");
        private readonly int attackHash = Animator.StringToHash("Attack");
        private readonly int rollHash = Animator.StringToHash("Roll");
        private readonly int hurtHash = Animator.StringToHash("Hurt");
        private readonly int deathHash = Animator.StringToHash("Death");
        
        public Animator Animator => animator;
        public SpriteRenderer SpriteRenderer => spriteRenderer;
        
        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
                
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        public void SetSpeed(float speed)
        {
            animator.SetFloat(speedHash, Mathf.Abs(speed));
        }
        
        public void SetGrounded(bool isGrounded)
        {
            animator.SetBool(isGroundedHash, isGrounded);
        }
        
        public void SetJumping(bool isJumping)
        {
            animator.SetBool(isJumpingHash, isJumping);
        }
        
        public void SetFalling(bool isFalling)
        {
            animator.SetBool(isFallingHash, isFalling);
        }
        
        public void TriggerAttack()
        {
            animator.SetTrigger(attackHash);
        }
        
        public void TriggerRoll()
        {
            animator.SetTrigger(rollHash);
        }
        
        public void TriggerHurt()
        {
            animator.SetTrigger(hurtHash);
        }
        
        public void TriggerDeath()
        {
            animator.SetTrigger(deathHash);
        }
        
        public void FlipSprite(bool facingRight)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !facingRight;
            }
        }
        
        public void SetAnimationSpeed(float speed)
        {
            animator.speed = speed;
        }
        
        public bool IsAnimationPlaying(string animationName)
        {
            return animator.GetCurrentAnimatorStateInfo(0).IsName(animationName);
        }
        
        public float GetAnimationLength(string animationName)
        {
            AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
            foreach (var clip in clipInfo)
            {
                if (clip.clip.name == animationName)
                {
                    return clip.clip.length;
                }
            }
            return 0f;
        }
    }
}