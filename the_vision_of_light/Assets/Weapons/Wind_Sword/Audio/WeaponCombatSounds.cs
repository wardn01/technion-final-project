using UnityEngine;

public class WeaponCombatSounds : MonoBehaviour
{
    [Header("Components")]
    public AudioSource audioSource;

    [Header("Normal Attacks (Whoosh / Swing)")]
    public AudioClip[] normalAttackClips;

    [Header("Skills (E & Q)")]
    public AudioClip[] skillE_Sound;
    public AudioClip[] skillQ_Sound;

    [Header("Movement Sounds (When Sword is Drawn)")]
    public AudioClip rollSound;
    public AudioClip[] combatWalk;

    [Header("Sound Volume Settings")]
    public float normalAttackVolume = 0.5f;
    public float skillEVolume = 0.7f;
    public float skillQVolume = 1.0f;
    public float rollVolume = 1.0f;
    public float combatWalkVolume = 0.5f;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource != null) audioSource.spatialBlend = 1f;
    }

    public void PlayNormalAttackSound()
    {
        if (normalAttackClips == null || normalAttackClips.Length == 0 || audioSource == null) return;
        int index = Random.Range(0, normalAttackClips.Length);
        audioSource.pitch = Random.Range(1.1f, 1.3f);
        audioSource.PlayOneShot(normalAttackClips[index], normalAttackVolume);
    }

    public void PlaySkillESound()
    {
        if (skillE_Sound == null || skillE_Sound.Length == 0 || audioSource == null) return;
        int index = Random.Range(0, skillE_Sound.Length);
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(skillE_Sound[index], skillEVolume);
    }

    public void PlaySkillQSound()
    {
        if (skillQ_Sound == null || skillQ_Sound.Length == 0 || audioSource == null) return;
        int index = Random.Range(0, skillQ_Sound.Length);
        audioSource.pitch = 0.9f;
        audioSource.PlayOneShot(skillQ_Sound[index], skillQVolume);
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
        int index = Random.Range(0, combatWalk.Length);
        audioSource.pitch = Random.Range(0.8f, 1.2f);
        audioSource.PlayOneShot(combatWalk[index], combatWalkVolume);
    }
}