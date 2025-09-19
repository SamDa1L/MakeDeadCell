using UnityEngine;

namespace DeadCells.Core
{
    public class EffectsManager : MonoBehaviour
    {
        [Header("Screen Shake")]
        [SerializeField] private float shakeIntensity = 1f;
        [SerializeField] private float shakeDuration = 0.1f;
        
        [Header("Particle Systems")]
        [SerializeField] private ParticleSystem hitEffect;
        [SerializeField] private ParticleSystem bloodEffect;
        [SerializeField] private ParticleSystem deathEffect;
        [SerializeField] private ParticleSystem jumpDustEffect;
        
        [Header("Screen Flash")]
        [SerializeField] private ScreenFlash screenFlash;
        
        public static EffectsManager Instance { get; private set; }
        
        private Camera mainCamera;
        private Vector3 originalCameraPosition;
        private float shakeTimer = 0f;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                mainCamera = Camera.main;
                if (mainCamera != null)
                    originalCameraPosition = mainCamera.transform.position;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Update()
        {
            HandleScreenShake();
        }
        
        public void ShakeScreen(float intensity = 1f, float duration = 0.1f)
        {
            shakeIntensity = intensity;
            shakeDuration = duration;
            shakeTimer = duration;
        }
        
        private void HandleScreenShake()
        {
            if (shakeTimer > 0f && mainCamera != null)
            {
                shakeTimer -= Time.deltaTime;
                
                Vector3 shakeOffset = Random.insideUnitCircle * shakeIntensity * (shakeTimer / shakeDuration);
                mainCamera.transform.position = originalCameraPosition + shakeOffset;
                
                if (shakeTimer <= 0f)
                {
                    mainCamera.transform.position = originalCameraPosition;
                }
            }
        }
        
        public void PlayHitEffect(Vector3 position, Vector3 normal = default)
        {
            if (hitEffect != null)
            {
                PlayParticleEffect(hitEffect, position, normal);
            }
        }
        
        public void PlayBloodEffect(Vector3 position, Vector3 direction = default)
        {
            if (bloodEffect != null)
            {
                PlayParticleEffect(bloodEffect, position, direction);
            }
        }
        
        public void PlayDeathEffect(Vector3 position)
        {
            if (deathEffect != null)
            {
                PlayParticleEffect(deathEffect, position);
            }
        }
        
        public void PlayJumpDustEffect(Vector3 position)
        {
            if (jumpDustEffect != null)
            {
                PlayParticleEffect(jumpDustEffect, position);
            }
        }
        
        private void PlayParticleEffect(ParticleSystem effect, Vector3 position, Vector3 direction = default)
        {
            if (effect == null) return;
            
            ParticleSystem instance = Instantiate(effect, position, Quaternion.identity);
            
            if (direction != Vector3.zero)
            {
                var main = instance.main;
                main.startRotation = Mathf.Atan2(direction.y, direction.x);
            }
            
            instance.Play();
            
            // Destroy after playing
            float duration = instance.main.duration + instance.main.startLifetime.constantMax;
            Destroy(instance.gameObject, duration);
        }
        
        public void FlashScreen(Color color, float duration = 0.1f)
        {
            if (screenFlash != null)
            {
                screenFlash.Flash(color, duration);
            }
        }
        
        public void FlashDamage()
        {
            FlashScreen(Color.red, 0.1f);
        }
        
        public void FlashHeal()
        {
            FlashScreen(Color.green, 0.2f);
        }
    }
}