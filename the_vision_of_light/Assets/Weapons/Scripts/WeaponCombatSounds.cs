using UnityEngine;

/// <summary>
/// 3D audio on the equipped weapon model for swings and combat movement.
/// Skill E/Q sounds live on their skill prefabs via <see cref="WeaponSkillAudio"/> instead.
/// </summary>
public class WeaponCombatSounds : MonoBehaviour
{
    #region Components
    [Header("Components")]
    public AudioSource audioSource;
    #endregion

    #region Clips
    [Header("Normal Attacks (Whoosh / Swing)")]
    public AudioClip[] normalAttackClips;

    [Header("Movement Sounds (When Sword is Drawn)")]
    public AudioClip rollSound;
    public AudioClip[] combatWalk;
    #endregion

    #region Volume
    [Header("Sound Volume Settings")]
    public float normalAttackVolume = 1f;
    public float rollVolume = 1.0f;
    public float combatWalkVolume = 1f;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource != null) audioSource.spatialBlend = 1f;
    }
    #endregion

    #region Playback
    public void PlayNormalAttackSound()
    {
        if (normalAttackClips == null || normalAttackClips.Length == 0 || audioSource == null) return;
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(normalAttackClips[0], normalAttackVolume);
    }

    public void PlayRollSound()
    {
        if (rollSound == null || audioSource == null) return;
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(rollSound, rollVolume);
    }

    public void PlayCombatWalkSound()
    {
        if (combatWalk == null || combatWalk.Length == 0 || audioSource == null) return;
        audioSource.pitch = Random.Range(0.8f, 1.2f);
        audioSource.PlayOneShot(combatWalk[0], combatWalkVolume);
    }
    #endregion
}
