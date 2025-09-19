using UnityEngine;

namespace DeadCellsTestFramework.Combat
{
    public class HitstunController : MonoBehaviour
    {
        [Header("Hitstun Settings")]
        [SerializeField] private bool isInHitstun = false;
        [SerializeField] private float hitstunTimer = 0f;
        
        public bool IsInHitstun => isInHitstun;
        
        private void Update()
        {
            if (isInHitstun)
            {
                hitstunTimer -= Time.deltaTime;
                if (hitstunTimer <= 0)
                {
                    EndHitstun();
                }
            }
        }
        
        public void ApplyHitstun(float duration)
        {
            isInHitstun = true;
            hitstunTimer = duration;
            
            // Disable movement/actions during hitstun
            var playerController = GetComponent<DeadCellsTestFramework.Player.PlayerController>();
            var enemyController = GetComponent<EnemyController>();
            
            // Player and enemy controllers should check IsInHitstun before allowing actions
        }
        
        private void EndHitstun()
        {
            isInHitstun = false;
            hitstunTimer = 0f;
        }
    }
}