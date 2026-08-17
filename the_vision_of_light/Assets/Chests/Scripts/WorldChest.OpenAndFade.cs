using System.Collections;
using UnityEngine;

namespace VisionOfLight.Chest
{
    /// <summary>Open sequence, loot, lid animation, fade, and audio for <see cref="WorldChest"/>.</summary>
    public partial class WorldChest
    {
        #region Open Flow
        private void TryOpenChest()
        {
            if (isOpening || ChestRegistry.IsOpened(chestId))
                return;

            if (unlockMode == ChestUnlockMode.DefeatEnemies && !AreGuardiansCleared())
                return;

            isOpening = true;
            DisableInteractionColliders();
            GrantLoot();
            PlayOpenSound();
            ChestRegistry.MarkOpened(chestId);
            PlayerStatsTracker.RecordChestOpened();

            if (PauseMenuManager.Instance != null)
                PauseMenuManager.Instance.SaveGameSilently();

            StartCoroutine(OpenChestSequence());
        }

        private IEnumerator OpenChestSequence()
        {
            yield return PlayLidOpenAnimation();
            yield return FadeOutRoutine();
        }

        private IEnumerator PlayLidOpenAnimation()
        {
            ResolveLidTransform();

            if (lidTransform == null || lidOpenDuration <= 0f)
                yield break;

            Quaternion startRotation = lidTransform.localRotation;
            Quaternion endRotation = startRotation * Quaternion.Euler(lidOpenAngle, 0f, 0f);
            float elapsed = 0f;

            while (elapsed < lidOpenDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / lidOpenDuration);
                lidTransform.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
                yield return null;
            }

            lidTransform.localRotation = endRotation;
        }

        private void ResolveLidTransform()
        {
            if (lidTransform != null)
                return;

            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child == transform)
                    continue;

                string name = child.name.ToLowerInvariant();
                if (name.Contains("cover") || name.Contains("roof") || name.Contains("lid"))
                {
                    lidTransform = child;
                    return;
                }
            }
        }

        private void GrantLoot()
        {
            if (lootTable == null)
                return;

            lootTable.GrantToPlayer();
        }

        private void PlayOpenSound()
        {
            if (openSound == null || openSoundVolume <= 0f)
                return;

            EnsureAudioSource();
            if (audioSource != null)
                audioSource.PlayOneShot(openSound, openSoundVolume);
        }

        private void DisableInteractionColliders()
        {
            if (interactionColliders == null)
                return;

            foreach (Collider col in interactionColliders)
            {
                if (col != null)
                    col.enabled = false;
            }
        }

        private IEnumerator FadeOutRoutine()
        {
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
                ApplyFadeAlpha(alpha);
                yield return null;
            }

            ApplyFadeAlpha(0f);
            HideChestVisuals();
            chestVisuallyOpened = true;
        }

        private void ApplyOpenedVisualState()
        {
            DisableInteractionColliders();
            ApplyFadeAlpha(0f);
            HideChestVisuals();
        }

        /// <summary>Hides the chest meshes after opening while keeping this GameObject active for guardian respawns.</summary>
        private void HideChestVisuals()
        {
            if (cachedFadeRenderers == null)
                return;

            foreach (Renderer renderer in cachedFadeRenderers)
            {
                if (renderer != null)
                    renderer.enabled = false;
            }
        }
        #endregion

        #region Fade Helpers
        private void CacheFadeRenderers()
        {
            cachedFadeRenderers = GetComponentsInChildren<Renderer>(true);
        }

        private void CacheBaseColors()
        {
            baseColors.Clear();

            if (cachedFadeRenderers == null)
                return;

            foreach (Renderer renderer in cachedFadeRenderers)
            {
                if (renderer == null)
                    continue;

                baseColors[renderer] = ReadRendererColor(renderer);
            }
        }

        private static Color ReadRendererColor(Renderer renderer)
        {
            Material mat = renderer.sharedMaterial;
            if (mat == null)
                return Color.white;

            if (mat.HasProperty(BaseColorId))
                return mat.GetColor(BaseColorId);

            if (mat.HasProperty(ColorId))
                return mat.GetColor(ColorId);

            return Color.white;
        }

        private void ApplyFadeAlpha(float alpha)
        {
            if (cachedFadeRenderers == null)
                return;

            fadePropertyBlock ??= new MaterialPropertyBlock();

            foreach (Renderer renderer in cachedFadeRenderers)
            {
                if (renderer == null || !baseColors.TryGetValue(renderer, out Color baseColor))
                    continue;

                Color faded = baseColor;
                faded.a = baseColor.a * alpha;

                renderer.GetPropertyBlock(fadePropertyBlock);
                if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(BaseColorId))
                    fadePropertyBlock.SetColor(BaseColorId, faded);
                else
                    fadePropertyBlock.SetColor(ColorId, faded);

                renderer.SetPropertyBlock(fadePropertyBlock);
            }
        }

        private void EnsureAudioSource()
        {
            if (audioSource != null)
                return;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            AudioMixerHub.Route(audioSource, AudioMixerHub.Bus.SFX);
        }
        #endregion
    }
}
