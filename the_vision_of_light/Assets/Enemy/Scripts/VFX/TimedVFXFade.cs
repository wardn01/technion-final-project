using UnityEngine;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// Scales a spawned VFX from zero → target → zero for a soft appear / disappear.
    /// Added at runtime by <see cref="GolemAttackVFX"/>.
    /// </summary>
    public class TimedVFXFade : MonoBehaviour
    {
        private float targetScale = 1f;
        private float totalLifetime = 1f;
        private float fadeInDuration = 0.15f;
        private float fadeOutDuration = 0.3f;
        private float elapsed;

        /// <summary>Configures scale and fade timing after the VFX is spawned.</summary>
        public void Configure(float scale, float lifetime, float fadeIn, float fadeOut)
        {
            targetScale = scale;
            totalLifetime = Mathf.Max(lifetime, fadeIn + fadeOut + 0.05f);
            fadeInDuration = Mathf.Max(0.01f, fadeIn);
            fadeOutDuration = Mathf.Max(0.01f, fadeOut);
            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;

            if (elapsed >= totalLifetime)
            {
                Destroy(gameObject);
                return;
            }

            float fadeOutStart = totalLifetime - fadeOutDuration;
            float scaleMultiplier;

            if (elapsed < fadeInDuration)
            {
                scaleMultiplier = Mathf.SmoothStep(0f, targetScale, elapsed / fadeInDuration);
            }
            else if (elapsed >= fadeOutStart)
            {
                scaleMultiplier = Mathf.SmoothStep(targetScale, 0f, (elapsed - fadeOutStart) / fadeOutDuration);
            }
            else
            {
                scaleMultiplier = targetScale;
            }

            transform.localScale = Vector3.one * scaleMultiplier;
        }
    }
}
