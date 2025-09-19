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
            // Controllers should check IsInHitstun before allowing actions
            // This can be accessed via GetComponent<HitstunController>().IsInHitstun
        }
        
        private void EndHitstun()
        {
            isInHitstun = false;
            hitstunTimer = 0f;
        }
    }
}