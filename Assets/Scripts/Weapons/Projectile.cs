using UnityEngine;
using DeadCellsTestFramework.Combat;

namespace DeadCellsTestFramework.Weapons
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float maxDistance = 20f;
        [SerializeField] private bool piercing = false;
        [SerializeField] private int maxPierceCount = 3;
        
        private Vector2 direction;
        private DamageInfo damageInfo;
        private LayerMask targetLayerMask;
        private float traveledDistance = 0f;
        private int pierceCount = 0;
        private Vector3 lastPosition;
        
        private Rigidbody2D rb;
        private Collider2D col;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }
        
        public void Initialize(Vector2 dir, float projectileSpeed, float range, DamageInfo damage, LayerMask targetMask)
        {
            direction = dir.normalized;
            speed = projectileSpeed;
            maxDistance = range;
            damageInfo = damage;
            targetLayerMask = targetMask;
            lastPosition = transform.position;
            
            // Set projectile rotation to face movement direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            
            // Set velocity
            rb.velocity = direction * speed;
        }
        
        private void Update()
        {
            // Track traveled distance
            float frameDistance = Vector3.Distance(transform.position, lastPosition);
            traveledDistance += frameDistance;
            lastPosition = transform.position;
            
            // Destroy if traveled too far
            if (traveledDistance >= maxDistance)
            {
                DestroyProjectile();
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            // Check if target is in the correct layer
            if ((targetLayerMask & (1 << other.gameObject.layer)) == 0)
                return;
            
            // Don't hit the shooter
            if (other.gameObject == damageInfo.source)
                return;
            
            // Deal damage
            CombatManager.Instance?.DealDamage(damageInfo.source, other.gameObject, damageInfo);
            
            // Handle piercing
            if (piercing && pierceCount < maxPierceCount)
            {
                pierceCount++;
                // Reduce damage for subsequent hits
                damageInfo.baseDamage *= 0.8f;
            }
            else
            {
                DestroyProjectile();
            }
        }
        
        private void DestroyProjectile()
        {
            // Spawn hit effects
            CreateImpactEffects();
            
            Destroy(gameObject);
        }
        
        private void CreateImpactEffects()
        {
            // Spawn particles
            // Play impact sound
            // Create screen shake if powerful projectile
        }
        
        private void OnBecameInvisible()
        {
            // Destroy projectile when it goes off screen to prevent memory leaks
            Invoke(nameof(DestroyProjectile), 1f);
        }
    }
}