using UnityEngine;

/// <summary>
/// Skill cast audio clips stored on E/Q prefabs.
/// By default <see cref="playOnStart"/> is off — <c>PlayerCombat.PlaySkillESound</c> /
/// <c>PlaySkillQSound</c> (animation events) call <see cref="PlayFromPrefab"/> at the correct frame.
/// Set <see cref="playOnStart"/> to true only if the prefab must self-play without animation events.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class WeaponSkillAudio : MonoBehaviour
{
    #region Clips
    [Header("Clips")]
    public AudioClip[] clips;
    #endregion

    #region Playback
    [Header("Playback")]
    [Tooltip("When false, only PlayerCombat animation events play this clip (avoids double audio on spawn).")]
    public bool playOnStart;

    [Range(0f, 1f)]
    public float volume = 0.8f;

    public float pitch = 1f;
    public bool useRandomPitch;
    public float randomPitchMin = 0.9f;
    public float randomPitchMax = 1.1f;
    #endregion

    #region Components
    private AudioSource audioSource;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }

    private void Start()
    {
        if (playOnStart)
            Play();
    }
    #endregion

    #region Public API
    /// <summary>
    /// Plays a random clip from a skill prefab asset at a world position.
    /// Used by animation events before the skill VFX is instantiated.
    /// </summary>
    public static bool PlayFromPrefab(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return false;

        WeaponSkillAudio skillAudio = prefab.GetComponent<WeaponSkillAudio>();
        if (skillAudio == null || skillAudio.clips == null || skillAudio.clips.Length == 0)
            return false;

        AudioClip clip = skillAudio.clips[Random.Range(0, skillAudio.clips.Length)];
        AudioSource.PlayClipAtPoint(clip, position, skillAudio.volume);
        return true;
    }

    /// <summary>Plays a random clip on this instance's <see cref="AudioSource"/>.</summary>
    public void Play()
    {
        if (clips == null || clips.Length == 0 || audioSource == null) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        audioSource.pitch = useRandomPitch
            ? Random.Range(randomPitchMin, randomPitchMax)
            : pitch;

        audioSource.PlayOneShot(clip, volume);
    }
    #endregion
}
