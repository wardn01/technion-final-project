using System.Collections;
using UnityEngine;

/// <summary>
/// Drives rift VFX (mesh emission, particles, light, audio) for <see cref="ChallengeStone"/> trials.
/// </summary>
public class Rift_Controller : MonoBehaviour
{
    #region Inspector
    [Header("Applied to the effects at start")]
    [SerializeField] private Color effectsColor;

    [Header("Changing these might break the effects")]
    [Space(20)]
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
    private bool inTransition;
    private bool activated;
    private Material matInstance;
    private float fadeFloat;
    private Coroutine transitionRoutine;
    private Coroutine runeBlastRoutine;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (meshRenderer != null)
        {
            matInstance = meshRenderer.material;
            matInstance.SetColor("_EmissionColor", effectsColor);
            matInstance.SetFloat("_EmissionStrength", 0f);
        }

        if (riftLight != null)
        {
            maxLightIntensity = riftLight.intensity;
            riftLight.intensity = 0f;
            riftLight.color = effectsColor;
        }

        if (effectsParticles == null)
            return;

        foreach (ParticleSystem part in effectsParticles)
        {
            if (part == null)
                continue;

            ParticleSystem.MainModule mod = part.main;
            mod.startColor = effectsColor;
        }
    }
    #endregion

    #region Public API
    /// <summary>Turns rift visuals on or off with a fade transition.</summary>
    public void F_ToggleRift(bool activate)
    {
        if (inTransition || activate == activated)
            return;

        activated = activate;

        if (activate)
        {
            PlayParticleRange(0, 3);
            PlayAudio(0);

            transitionRoutine = StartCoroutine(TransitionSequence());
            runeBlastRoutine = StartCoroutine(RuneBlasts());
        }
        else
        {
            if (runeBlastRoutine != null)
            {
                StopCoroutine(runeBlastRoutine);
                runeBlastRoutine = null;
            }

            transitionRoutine = StartCoroutine(TransitionSequence());
            StopParticleRange(0, 2);
        }
    }
    #endregion

    #region VFX Routines
    private IEnumerator TransitionSequence()
    {
        inTransition = true;

        while (true)
        {
            if (activated)
            {
                fadeFloat = Mathf.MoveTowards(fadeFloat, 1f, Time.deltaTime * TransitionSpeed);

                if (fadeFloat >= 1f)
                {
                    inTransition = false;
                    yield break;
                }
            }
            else
            {
                fadeFloat = Mathf.MoveTowards(fadeFloat, 0f, Time.deltaTime * TransitionSpeed);

                if (fadeFloat <= 0f)
                {
                    StopAudio(0);
                    inTransition = false;
                    yield break;
                }
            }

            ApplyFade(fadeFloat);
            yield return null;
        }
    }

    private IEnumerator RuneBlasts()
    {
        if (effectsParticles == null || effectsParticles.Length <= 4 || effectsParticles[4] == null)
            yield break;

        ParticleSystem runeBlast = effectsParticles[4];
        ParticleSystem.MainModule partMain = runeBlast.main;

        while (activated)
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

    private void ApplyFade(float fade)
    {
        if (matInstance != null)
            matInstance.SetFloat("_EmissionStrength", fade);

        if (riftLight != null)
            riftLight.intensity = maxLightIntensity * fade;

        if (effectsAudio != null && effectsAudio.Length > 0 && effectsAudio[0] != null)
            effectsAudio[0].volume = fade * MaxAmbientVolume;
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

    private void StopParticleRange(int fromInclusive, int toInclusive)
    {
        if (effectsParticles == null)
            return;

        for (int i = fromInclusive; i <= toInclusive && i < effectsParticles.Length; i++)
        {
            if (effectsParticles[i] != null)
                effectsParticles[i].Stop();
        }
    }

    private void PlayAudio(int index)
    {
        if (effectsAudio == null || index < 0 || index >= effectsAudio.Length)
            return;

        if (effectsAudio[index] != null)
            effectsAudio[index].Play();
    }

    private void StopAudio(int index)
    {
        if (effectsAudio == null || index < 0 || index >= effectsAudio.Length)
            return;

        if (effectsAudio[index] != null)
            effectsAudio[index].Stop();
    }
    #endregion
}
