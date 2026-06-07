using UnityEngine;

/// <summary>
/// Plays quest feedback sounds when the story advances. Watches <see cref="QuestManager"/> for
/// step changes (new objective) and chapter completions (quest finished) without coupling audio
/// into <see cref="QuestManager"/> itself.
/// </summary>
public class QuestAudioPlayer : MonoBehaviour
{
    #region Audio Clips
    [Header("Audio Clips")]
    public AudioClip stepAdvanceClip;
    public AudioClip questCompleteClip;

    [Range(0f, 1f)]
    public float stepAdvanceVolume = 0.6f;

    [Range(0f, 1f)]
    public float questCompleteVolume = 0.8f;
    #endregion

    #region Internal State
    private AudioSource audioSource;
    private int lastQuestState = -1;
    private int lastQuestStep = -1;
    private bool initialized;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        GameObject uiAudio = GameObject.Find("UIAudioPlayer");
        if (uiAudio != null)
            audioSource = uiAudio.GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (QuestManager.Instance == null) return;

        int currentState = QuestManager.Instance.mainQuestState;
        int currentStep = QuestManager.Instance.questStepIndex;

        if (!initialized)
        {
            lastQuestState = currentState;
            lastQuestStep = currentStep;
            initialized = true;
            return;
        }

        if (currentState != lastQuestState)
        {
            PlayClip(questCompleteClip, questCompleteVolume);
            lastQuestState = currentState;
            lastQuestStep = currentStep;
            return;
        }

        if (currentStep != lastQuestStep)
        {
            PlayClip(stepAdvanceClip, stepAdvanceVolume);
            lastQuestStep = currentStep;
        }
    }
    #endregion

    #region Playback
    private void PlayClip(AudioClip clip, float volume)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, volume);
    }
    #endregion
}
