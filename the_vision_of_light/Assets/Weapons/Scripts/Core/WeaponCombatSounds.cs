using UnityEngine;

/// <summary>
/// 3D audio on the equipped weapon model for normal attacks and combat movement.
/// Invoked from <c>PlayerCombat</c> animation events:
/// <c>PlayNormalAttackSound</c>, <c>PlayRollSound</c>, <c>PlayCombatWalkSound</c>.
/// Skill E/Q cast sounds use <see cref="WeaponSkillAudio"/> instead.
/// </summary>
public class WeaponCombatSounds : MonoBehaviour
{
    #region Components
    [Header("Components")]
    public AudioSource audioSource;
    #endregion

    #region Clips
    [Header("Normal Attacks (Whoosh / Swing)")]
    [Tooltip("Index 0 = Attack_1, 1 = Attack_2, 2 = Attack_3.")]
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
    /// <summary>Picks the clip that matches the current combo step on animator layer 1.</summary>
    public void PlayNormalAttackSound()
    {
        if (normalAttackClips == null || normalAttackClips.Length == 0 || audioSource == null) return;

        int clipIndex = GetNormalAttackClipIndex();
        clipIndex = Mathf.Clamp(clipIndex, 0, normalAttackClips.Length - 1);

        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(normalAttackClips[clipIndex], normalAttackVolume);
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

    #region Helpers
    private int GetNormalAttackClipIndex()
    {
        Animator animator = GetComponentInParent<Animator>();
        if (animator == null) return 0;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(1);
        if (state.IsName("Attack_2")) return 1;
        if (state.IsName("Attack_3")) return 2;
        return 0;
    }
    #endregion
}
