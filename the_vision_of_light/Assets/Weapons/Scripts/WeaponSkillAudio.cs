using UnityEngine;

/// <summary>
/// Plays a skill sound when the E or Q prefab is spawned. Lives on the skill prefab itself so audio
/// continues even if the player swaps weapons while the VFX is still active.
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
        Play();
    }
    #endregion

    #region Public API
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
