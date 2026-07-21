using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VisionOfLight.Player;

/// <summary>
/// Looping ambient bed (market crowd, snow wind, etc.) while the player is inside this trigger.
/// Optionally ducks exploration music (e.g. snow region keeps Exploration playing at half volume).
/// </summary>
[RequireComponent(typeof(Collider))]
public class WorldAmbienceZone : MonoBehaviour
{
    private static readonly List<WorldAmbienceZone> ActiveZones = new List<WorldAmbienceZone>();

    [Header("Clip")]
    [Tooltip("Looping ambience — crowd murmur, snow wind, etc. Not music.")]
    public AudioClip ambienceClip;

    [Tooltip("Playback volume for this ambience layer.")]
    [Range(0f, 1f)]
    public float volume = 0.35f;

    [Tooltip("Seconds to fade in/out when entering or leaving.")]
    [Min(0f)]
    public float fadeSeconds = 1.2f;

    [Header("Exploration Music")]
    [Tooltip("ON for snow: keep Exploration playing but lower its volume while inside this zone.")]
    public bool duckExplorationMusic;

    [Header("Combat")]
    [Tooltip("Fade this ambience out while combat / boss music is playing; fades back in after the fight.")]
    public bool muteDuringCombat = true;

    [Header("Optional")]
    [Tooltip("If empty, uses AudioMixerHub SFX group when configured.")]
    public UnityEngine.Audio.AudioMixerGroup outputGroup;

    private Collider zoneCollider;
    private AudioSource audioSource;
    private Coroutine fadeRoutine;
    private bool wasAudible;

    /// <summary>True if the player is inside any ambience zone that ducks exploration music.</summary>
    public static bool ShouldDuckExploration(Vector3 playerPosition)
    {
        for (int i = ActiveZones.Count - 1; i >= 0; i--)
        {
            WorldAmbienceZone zone = ActiveZones[i];
            if (zone == null)
            {
                ActiveZones.RemoveAt(i);
                continue;
            }

            if (zone.duckExplorationMusic && zone.Contains(playerPosition))
                return true;
        }

        return false;
    }

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null && !zoneCollider.isTrigger)
            Debug.LogWarning("[WorldAmbienceZone] " + name + ": Collider must have Is Trigger enabled.", this);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0f;
        audioSource.clip = ambienceClip;

        if (outputGroup != null)
            audioSource.outputAudioMixerGroup = outputGroup;
        else
            AudioMixerHub.Route(audioSource, AudioMixerHub.Bus.SFX);
    }

    private void OnEnable()
    {
        if (!ActiveZones.Contains(this))
            ActiveZones.Add(this);
    }

    private void OnDisable()
    {
        ActiveZones.Remove(this);
        wasAudible = false;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.volume = 0f;
        }
    }

    private void OnDestroy()
    {
        ActiveZones.Remove(this);
    }

    private void Update()
    {
        Transform player = PlayerRegistry.Instance != null
            ? PlayerRegistry.Instance.transform
            : SharedInteractPromptUtility.GetPlayerTransform();

        bool isInside = player != null && Contains(player.position);
        bool combatMuted = muteDuringCombat && WorldMusicManager.IsCombatMusicActive;
        bool shouldBeAudible = isInside && !combatMuted;

        if (shouldBeAudible == wasAudible)
            return;

        wasAudible = shouldBeAudible;
        FadeTo(shouldBeAudible ? volume : 0f);
    }

    private bool Contains(Vector3 position)
    {
        if (zoneCollider == null || !zoneCollider.enabled || !gameObject.activeInHierarchy)
            return false;

        Vector3 closest = zoneCollider.ClosestPoint(position);
        return (closest - position).sqrMagnitude <= 0.0025f;
    }

    private void FadeTo(float target)
    {
        if (ambienceClip == null || audioSource == null)
            return;

        if (audioSource.clip != ambienceClip)
            audioSource.clip = ambienceClip;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(target));
    }

    private IEnumerator FadeRoutine(float target)
    {
        if (target > 0.01f && !audioSource.isPlaying)
            audioSource.Play();

        float start = audioSource.volume;
        float duration = Mathf.Max(0.05f, fadeSeconds);

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            audioSource.volume = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }

        audioSource.volume = target;

        if (target <= 0.01f)
            audioSource.Stop();

        fadeRoutine = null;
    }
}
