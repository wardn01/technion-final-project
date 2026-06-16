using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// Rift visuals for <see cref="ChallengeStone"/>:
    /// Waiting = dark glow (same VFX intensity as active), Active = lilac, Off = extinguished.
    /// </summary>
    public class RiftController : MonoBehaviour
    {
        public enum RiftVisualState
        {
            Off,
            WaitingBlackGlow,
            ActiveLilac
        }

        #region Inspector
        [Header("Rift Colors")]
        [Tooltip("Dark glow while the trial is available. Uses the same VFX power as Active Glow.")]
        [SerializeField] private Color waitingGlowColor = new Color(0.22f, 0.22f, 0.28f, 1f);

        [Tooltip("Bright glow while the challenge is running.")]
        [FormerlySerializedAs("activeGlowColor")]
        [FormerlySerializedAs("effectsColor")]
        [SerializeField] private Color activeGlowColor = new Color(0.65f, 0.45f, 0.95f, 1f);

        [Header("Effect References")]
        [Space(10)]
        [SerializeField] private Renderer meshRenderer;
        [SerializeField] private ParticleSystem[] effectsParticles;
        [SerializeField] private Light riftLight;
        [SerializeField] private AudioSource[] effectsAudio;
        #endregion

        #region Settings
        private float maxLightIntensity = 4f;
        private const float TransitionSpeed = 0.8f;
        private const float MaxAmbientVolume = 0.8f;
        #endregion

        #region Runtime State
        private RiftVisualState currentState = RiftVisualState.Off;
        private bool inTransition;
        private Material matInstance;
        private float fadeFloat;
        private Coroutine transitionRoutine;
        private Coroutine runeBlastRoutine;
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (meshRenderer != null)
            {
                matInstance = meshRenderer.material;
                matInstance.SetFloat(EmissionStrengthId, 0f);
            }

            if (riftLight != null)
            {
                maxLightIntensity = riftLight.intensity;
                riftLight.intensity = 0f;
            }

            SetVisualState(RiftVisualState.Off);
        }
        #endregion

        #region Public API
        public void SetVisualState(RiftVisualState state)
        {
            if (currentState == state && !inTransition)
                return;

            currentState = state;
            StopRunningEffects();

            switch (state)
            {
                case RiftVisualState.Off:
                    ApplyOffImmediate();
                    break;

                case RiftVisualState.WaitingBlackGlow:
                    BeginWaitingGlow();
                    break;

                case RiftVisualState.ActiveLilac:
                    BeginActiveGlow();
                    break;
            }
        }

        public void F_ToggleRift(bool activate)
        {
            SetVisualState(activate ? RiftVisualState.ActiveLilac : RiftVisualState.WaitingBlackGlow);
        }
        #endregion

        #region Visual States
        private void ApplyOffImmediate()
        {
            fadeFloat = 0f;

            if (matInstance != null)
            {
                matInstance.SetColor(EmissionColorId, waitingGlowColor);
                matInstance.SetFloat(EmissionStrengthId, 0f);
            }

            if (riftLight != null)
                riftLight.intensity = 0f;

            StopAllParticles();
            StopAllAudio();
        }

        private void BeginWaitingGlow()
        {
            fadeFloat = 0f;
            ApplyEffectColors(waitingGlowColor);

            if (matInstance != null)
                matInstance.SetColor(EmissionColorId, waitingGlowColor);

            PlayParticleRange(0, 3);
            transitionRoutine = StartCoroutine(WaitingTransitionSequence());
        }

        private void BeginActiveGlow()
        {
            fadeFloat = 0f;
            ApplyEffectColors(activeGlowColor);

            if (matInstance != null)
                matInstance.SetColor(EmissionColorId, activeGlowColor);

            PlayParticleRange(0, 3);
            PlayAudio(0);
            transitionRoutine = StartCoroutine(GlowTransitionSequence(activeGlowColor, playAmbientAudio: true));
            runeBlastRoutine = StartCoroutine(RuneBlasts());
        }

        private void StopRunningEffects()
        {
            inTransition = false;

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            if (runeBlastRoutine != null)
            {
                StopCoroutine(runeBlastRoutine);
                runeBlastRoutine = null;
            }
        }
        #endregion

        #region VFX Routines
        private IEnumerator WaitingTransitionSequence()
        {
            inTransition = true;

            while (fadeFloat < 1f)
            {
                fadeFloat = Mathf.MoveTowards(fadeFloat, 1f, Time.deltaTime * TransitionSpeed);
                ApplyGlowFade(waitingGlowColor, fadeFloat, ambientVolume: 0f);
                yield return null;
            }

            fadeFloat = 1f;
            ApplyGlowFade(waitingGlowColor, 1f, ambientVolume: 0f);
            inTransition = false;
            transitionRoutine = null;
        }

        private IEnumerator GlowTransitionSequence(Color glowColor, bool playAmbientAudio)
        {
            inTransition = true;

            while (fadeFloat < 1f)
            {
                fadeFloat = Mathf.MoveTowards(fadeFloat, 1f, Time.deltaTime * TransitionSpeed);
                ApplyGlowFade(glowColor, fadeFloat, playAmbientAudio ? fadeFloat * MaxAmbientVolume : 0f);
                yield return null;
            }

            fadeFloat = 1f;
            ApplyGlowFade(glowColor, 1f, playAmbientAudio ? MaxAmbientVolume : 0f);
            inTransition = false;
            transitionRoutine = null;
        }

        private IEnumerator RuneBlasts()
        {
            if (effectsParticles == null || effectsParticles.Length <= 4 || effectsParticles[4] == null)
                yield break;

            ParticleSystem runeBlast = effectsParticles[4];
            ParticleSystem.MainModule partMain = runeBlast.main;

            while (currentState == RiftVisualState.ActiveLilac)
            {
                runeBlast.Stop();
                partMain.duration = Random.Range(0.8f, 1f);
                runeBlast.Play();

                if (effectsAudio != null && effectsAudio.Length > 1 && effectsAudio[1] != null)
                {
                    effectsAudio[1].pitch = Random.Range(0.85f, 0.9f);
                    effectsAudio[1].Play();
                }

                yield return new WaitForSeconds(Random.Range(2f, 6f));
            }
        }

        private void ApplyGlowFade(Color glowColor, float fade, float ambientVolume)
        {
            if (matInstance != null)
            {
                matInstance.SetColor(EmissionColorId, glowColor);
                matInstance.SetFloat(EmissionStrengthId, fade);
            }

            if (riftLight != null)
            {
                riftLight.color = glowColor;
                riftLight.intensity = maxLightIntensity * fade;
            }

            if (effectsAudio != null && effectsAudio.Length > 0 && effectsAudio[0] != null)
                effectsAudio[0].volume = ambientVolume;
        }

        private void ApplyEffectColors(Color color)
        {
            if (effectsParticles == null)
                return;

            foreach (ParticleSystem part in effectsParticles)
            {
                if (part == null)
                    continue;

                ParticleSystem.MainModule mod = part.main;
                mod.startColor = color;
            }
        }

        private void StopAllParticles()
        {
            if (effectsParticles == null)
                return;

            for (int i = 0; i < effectsParticles.Length; i++)
            {
                if (effectsParticles[i] != null)
                    effectsParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void StopAllAudio()
        {
            if (effectsAudio == null)
                return;

            for (int i = 0; i < effectsAudio.Length; i++)
            {
                if (effectsAudio[i] != null)
                    effectsAudio[i].Stop();
            }
        }

        private void PlayParticleRange(int fromInclusive, int toInclusive)
        {
            if (effectsParticles == null)
                return;

            for (int i = fromInclusive; i <= toInclusive && i < effectsParticles.Length; i++)
            {
                if (effectsParticles[i] != null)
                    effectsParticles[i].Play();
            }
        }

        private void PlayAudio(int index)
        {
            if (effectsAudio == null || index < 0 || index >= effectsAudio.Length)
                return;

            if (effectsAudio[index] != null)
                effectsAudio[index].Play();
        }
        #endregion
    }
}
