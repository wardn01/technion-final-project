using UnityEngine;

public class WindSwordSounds : MonoBehaviour
{
    [Header("Components")]
    public AudioSource audioSource;

    [Header("Normal Attacks (Wind Whoosh)")]
    public AudioClip[] normalAttackClips;

    [Header("Skills (E & Q)")]
    public AudioClip[] skillE_WindBurst;
    public AudioClip[] skillQ_Tornado;

    [Header("Movement Sounds (When Sword is Drawn)")]
    public AudioClip rollWindWhoosh;
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
        audioSource.spatialBlend = 1f;
    }

    public void PlayNormalAttackSound()
    {
        if (normalAttackClips == null || normalAttackClips.Length == 0) return;
        int index = Random.Range(0, normalAttackClips.Length);
        audioSource.pitch = Random.Range(1.1f, 1.3f);
        audioSource.PlayOneShot(normalAttackClips[index], normalAttackVolume);
    }

    public void PlaySkillESound()
    {
        if (skillE_WindBurst == null || skillE_WindBurst.Length == 0) return;
        int index = Random.Range(0, skillE_WindBurst.Length);
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(skillE_WindBurst[index], skillEVolume);
    }

    public void PlaySkillQSound()
    {
        if (skillQ_Tornado == null || skillQ_Tornado.Length == 0) return;
        int index = Random.Range(0, skillQ_Tornado.Length);
        audioSource.pitch = 0.9f;
        audioSource.PlayOneShot(skillQ_Tornado[index], skillQVolume);
    }

    public void PlayRollSound()
    {
        if (rollWindWhoosh == null) return;
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(rollWindWhoosh, rollVolume);
    }

    public void PlayCombatWalkSound()
    {
        if (combatWalk == null || combatWalk.Length == 0) return;
        int index = Random.Range(0, combatWalk.Length);
        audioSource.pitch = Random.Range(0.8f, 1.2f);
        audioSource.PlayOneShot(combatWalk[index], combatWalkVolume);
    }
}