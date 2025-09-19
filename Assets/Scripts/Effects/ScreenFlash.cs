using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace DeadCellsTestFramework.Effects
{
    [RequireComponent(typeof(Image))]
    public class ScreenFlash : MonoBehaviour
    {
        private Image flashImage;
        private Coroutine flashCoroutine;
        
        private void Awake()
        {
            flashImage = GetComponent<Image>();
            
            // Make sure the image starts transparent
            Color color = flashImage.color;
            color.a = 0f;
            flashImage.color = color;
        }
        
        public void Flash(Color color, float duration)
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
            
            flashCoroutine = StartCoroutine(FlashCoroutine(color, duration));
        }
        
        private IEnumerator FlashCoroutine(Color flashColor, float duration)
        {
            // Set the flash color
            flashImage.color = flashColor;
            
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(flashColor.a, 0f, elapsedTime / duration);
                
                Color currentColor = flashImage.color;
                currentColor.a = alpha;
                flashImage.color = currentColor;
                
                yield return null;
            }
            
            // Ensure it's completely transparent at the end
            Color finalColor = flashImage.color;
            finalColor.a = 0f;
            flashImage.color = finalColor;
            
            flashCoroutine = null;
        }
    }
}