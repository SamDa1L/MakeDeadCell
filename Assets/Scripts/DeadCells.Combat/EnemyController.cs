using UnityEngine;

namespace DeadCells.Combat
{
    public class EnemyController : MonoBehaviour
    {
        [Header("Enemy Settings")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float detectionRange = 8f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private LayerMask playerLayerMask = 1 << 9;
        
        [Header("Components")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Health health;
        [SerializeField] private HitstunController hitstun;
        [SerializeField] private Animator animator;
        
        private Transform player;
        private bool playerInRange = false;
        private bool facingRight = true;
        
        public enum EnemyState
        {
            Idle,
            Patrol,
            Chase,
            Attack,
            Hurt,
            Death
        }
        
        private EnemyState currentState = EnemyState.Idle;
        
        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (health == null) health = GetComponent<Health>();
            if (hitstun == null) hitstun = GetComponent<HitstunController>();
            if (animator == null) animator = GetComponent<Animator>();
        }
        
        private void Start()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            player = playerObj?.transform;
            
            if (health != null)
            {
                health.OnDamageTaken += OnDamageTaken;
                health.OnDeath += OnDeath;
            }
        }
        
        private void Update()
        {
            if (hitstun != null && hitstun.IsInHitstun) return;
            
            DetectPlayer();
            UpdateState();
            UpdateAnimation();
        }
        
        private void DetectPlayer()
        {
            if (player == null) return;
            
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            playerInRange = distanceToPlayer <= detectionRange;
        }
        
        private void UpdateState()
        {
            switch (currentState)
            {
                case EnemyState.Idle:
                    if (playerInRange)
                        currentState = EnemyState.Chase;
                    break;
                    
                case EnemyState.Chase:
                    if (!playerInRange)
                        currentState = EnemyState.Idle;
                    else if (Vector2.Distance(transform.position, player.position) <= attackRange)
                        currentState = EnemyState.Attack;
                    else
                        ChasePlayer();
                    break;
                    
                case EnemyState.Attack:
                    if (Vector2.Distance(transform.position, player.position) > attackRange)
                        currentState = EnemyState.Chase;
                    else
                        AttackPlayer();
                    break;
                    
                case EnemyState.Hurt:
                    // Handled by hitstun system
                    if (hitstun == null || !hitstun.IsInHitstun)
                        currentState = playerInRange ? EnemyState.Chase : EnemyState.Idle;
                    break;
            }
        }
        
        private void ChasePlayer()
        {
            if (player == null || rb == null) return;
            
            Vector2 direction = (player.position - transform.position).normalized;
            rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);
            
            // Flip sprite based on movement direction
            if (direction.x > 0 && !facingRight)
                Flip();
            else if (direction.x < 0 && facingRight)
                Flip();
        }
        
        private void AttackPlayer()
        {
            // Basic attack logic
            if (player != null)
            {
                Vector2 attackDirection = (player.position - transform.position).normalized;
                
                // Create damage info
                var damageInfo = new DamageInfo
                {
                    baseDamage = 15f,
                    damageType = DamageType.Physical,
                    knockback = attackDirection * 3f,
                    hitstunDuration = 0.3f,
                    source = gameObject
                };
                
                // Deal damage to player
                CombatManager.Instance?.DealDamage(gameObject, player.gameObject, damageInfo);
                
                // Set cooldown before next attack
                Invoke(nameof(EndAttackCooldown), 1f);
                currentState = EnemyState.Idle;
            }
        }
        
        private void EndAttackCooldown()
        {
            // Attack is ready again
        }
        
        private void Flip()
        {
            facingRight = !facingRight;
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
        
        private void UpdateAnimation()
        {
            if (animator == null) return;
            
            float speed = rb != null ? Mathf.Abs(rb.velocity.x) : 0f;
            animator.SetFloat("Speed", speed);
            
            switch (currentState)
            {
                case EnemyState.Attack:
                    animator.SetTrigger("Attack");
                    break;
                case EnemyState.Hurt:
                    animator.SetTrigger("Hurt");
                    break;
                case EnemyState.Death:
                    animator.SetTrigger("Death");
                    break;
            }
        }
        
        private void OnDamageTaken(DamageInfo damageInfo)
        {
            currentState = EnemyState.Hurt;
            
            // Screen shake and effects
            // EffectsManager.Instance?.ShakeScreen(0.3f, 0.1f);
            // EffectsManager.Instance?.PlayHitEffect(transform.position);
        }
        
        private void OnDeath()
        {
            currentState = EnemyState.Death;
            
            // Disable colliders and rigidbody
            Collider2D[] colliders = GetComponents<Collider2D>();
            foreach (var col in colliders)
                col.enabled = false;
                
            if (rb != null)
                rb.simulated = false;
            
            // Play death effects
            // EffectsManager.Instance?.PlayDeathEffect(transform.position);
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}