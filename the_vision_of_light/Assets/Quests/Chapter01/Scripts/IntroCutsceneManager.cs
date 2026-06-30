using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VisionOfLight.Player;

/// <summary>
/// Placeholder narrative intro shown once at the start of a new game (Quest 01, step 0).
/// Plays before <see cref="AwakeningManager"/> and then activates the first quest chapter.
/// </summary>
[DefaultExecutionOrder(-300)]
public class IntroCutsceneManager : MonoBehaviour
{
    public static bool HasFinishedIntro { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetIntroState()
    {
        HasFinishedIntro = false;
    }

    public static void ResetSessionState()
    {
        HasFinishedIntro = false;
    }

    [Header("UI")]
    [Tooltip("Full-screen black overlay with intro text.")]
    public GameObject introOverlay;

    public Image overlayImage;
    public TextMeshProUGUI introText;

    [Header("Copy")]
    [TextArea(3, 6)]
    public string introMessage =
        "The Shadow Entity attacked the village...\nThe Magic Stone was stolen...\nYour friends did not survive...";

    [Header("Timings")]
    [Tooltip("Seconds between each typed character.")]
    public float delayPerCharacter = 0.06f;

    [Tooltip("Seconds between each deleted character (backspace). Usually faster than typing.")]
    public float delayPerDeleteCharacter = 0.03f;

    [Tooltip("Pause after a line is fully typed, before it gets erased.")]
    public float holdAfterLine = 1.2f;

    [Tooltip("Pause after a line is erased, before the next line starts typing.")]
    public float delayBetweenLines = 0.6f;

    [Tooltip("Pause on the empty black screen before awakening begins.")]
    public float holdBeforeAwakening = 1f;

    [Header("Audio")]
    public AudioSource typeAudioSource;

    [Tooltip("Played once per typed character. No sound plays while text is erased.")]
    public AudioClip typeCharacterClip;

    [Range(0f, 1f)]
    public float typeVolume = 0.35f;

    [Range(0f, 1f)]
    [Tooltip("Type sound plays only until this fraction of the line is typed. 0.8 = silent for the last 20%.")]
    public float typeSoundLineFraction = 0.8f;

    [Range(0.8f, 1.2f)]
    public float typePitchMin = 0.95f;

    [Range(0.8f, 1.2f)]
    public float typePitchMax = 1.05f;

    [Header("Flow")]
    public AwakeningManager awakeningManager;

    private bool isPlaying;

    private void Awake()
    {
        ResolveFlowReferences();
        ResolveTypeAudioSource();
    }

    /// <summary>True when this manager should run the intro and drive awakening afterward.</summary>
    public bool ShouldPlayIntro()
    {
        if (HasFinishedIntro)
            return false;

        return IsIntroQuestState();
    }

    /// <summary>True while intro has not finished yet on a fresh Chapter 1 start.</summary>
    public bool IsIntroQuestState()
    {
        if (QuestManager.Instance == null)
            return true;

        return QuestManager.Instance.IsAtFreshStoryStart();
    }

    /// <summary>When true, <see cref="AwakeningManager"/> waits for this intro before starting.</summary>
    public bool HandlesAwakeningStart => !HasFinishedIntro && IsIntroQuestState();

    /// <summary>
    /// Resolves cross-references. Called automatically and by <see cref="Quest01ChapterManager"/>.
    /// </summary>
    public void ResolveFlowReferences()
    {
        if (awakeningManager == null)
        {
            awakeningManager = GetComponent<AwakeningManager>()
                ?? GetComponentInParent<AwakeningManager>()
                ?? FindAnyObjectByType<AwakeningManager>();
        }
    }

    /// <summary>
    /// Fills intro UI from <see cref="AwakeningManager.blackScreen"/> when not assigned in the Inspector.
    /// </summary>
    public void ResolveUiReferences(AwakeningManager awakening = null)
    {
        awakening ??= awakeningManager;
        if (awakening == null)
            return;

        if (introOverlay == null && awakening.blackScreen != null)
            introOverlay = awakening.blackScreen.gameObject;

        if (overlayImage == null && awakening.blackScreen != null)
            overlayImage = awakening.blackScreen;

        if (introText == null && introOverlay != null)
            introText = introOverlay.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void Start()
    {
        ResetSessionStateIfNeeded();

        ResolveFlowReferences();
        ResolveUiReferences();

        if (!ShouldPlayIntro())
        {
            HasFinishedIntro = true;
            HideIntroPresentation();
            return;
        }

        StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        isPlaying = true;
        SetPlayerFrozen(true);

        ResolveFlowReferences();
        ResolveUiReferences();

        if (introText == null)
            Debug.LogWarning("[IntroCutsceneManager] IntroText is not assigned. Wire BlackScreen/IntroText on Quest01_Manager.");

        if (introOverlay != null)
            introOverlay.SetActive(true);

        if (overlayImage != null)
            SetOverlayAlpha(1f);

        if (introText != null)
        {
            introText.gameObject.SetActive(true);
            introText.alpha = 1f;
            introText.text = string.Empty;
        }

        yield return RevealIntroOneLineAtATime();

        yield return new WaitForSeconds(holdBeforeAwakening);

        HideIntroPresentation();

        ActivateQuest01();

        HasFinishedIntro = true;
        isPlaying = false;

        awakeningManager?.StartAwakening();
    }

    private void ResetSessionStateIfNeeded()
    {
        if (WorldSaveManager.Instance != null && WorldSaveManager.Instance.HasCompletedChapter01Awakening)
            return;

        ResetSessionState();
        AwakeningManager.ResetSessionState();
    }

    /// <summary>Prepares intro text element before the typewriter begins.</summary>
    public void ShowIntroPresentation()
    {
        if (introText == null)
            return;

        introText.gameObject.SetActive(true);
        introText.text = string.Empty;
        introText.alpha = 1f;
    }

    /// <summary>Hides intro copy so awakening can reuse <see cref="AwakeningManager.blackScreen"/>.</summary>
    public void HideIntroPresentation()
    {
        if (introText == null)
            return;

        introText.text = string.Empty;
        introText.alpha = 0f;
        introText.gameObject.SetActive(false);
    }

    private void ResolveTypeAudioSource()
    {
        if (typeAudioSource != null)
            return;

        GameObject uiAudio = GameObject.Find("UIAudioPlayer");
        if (uiAudio != null)
            typeAudioSource = uiAudio.GetComponent<AudioSource>();
    }

    private IEnumerator RevealIntroOneLineAtATime()
    {
        if (introText == null || string.IsNullOrEmpty(introMessage))
            yield break;

        string[] lines = introMessage.Split('\n');

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex];
            if (string.IsNullOrEmpty(line))
                continue;

            for (int charIndex = 0; charIndex < line.Length; charIndex++)
            {
                introText.text = line.Substring(0, charIndex + 1);
                introText.ForceMeshUpdate();

                if (ShouldPlayTypeSound(charIndex, line.Length))
                    PlayTypeSound();

                if (delayPerCharacter > 0f)
                    yield return new WaitForSeconds(delayPerCharacter);
                else
                    yield return null;
            }

            if (holdAfterLine > 0f)
                yield return new WaitForSeconds(holdAfterLine);

            for (int charIndex = line.Length - 1; charIndex >= 0; charIndex--)
            {
                introText.text = charIndex > 0 ? line.Substring(0, charIndex) : string.Empty;
                introText.ForceMeshUpdate();

                if (delayPerDeleteCharacter > 0f)
                    yield return new WaitForSeconds(delayPerDeleteCharacter);
                else
                    yield return null;
            }

            if (lineIndex < lines.Length - 1 && delayBetweenLines > 0f)
                yield return new WaitForSeconds(delayBetweenLines);
        }
    }

    private bool ShouldPlayTypeSound(int charIndex, int lineLength)
    {
        if (lineLength <= 0 || typeSoundLineFraction >= 1f)
            return true;

        if (typeSoundLineFraction <= 0f)
            return false;

        float typedFraction = (charIndex + 1f) / lineLength;
        return typedFraction <= typeSoundLineFraction;
    }

    private void PlayTypeSound()
    {
        PlayCharacterSound(typeCharacterClip, typeVolume);
    }

    private void PlayCharacterSound(AudioClip clip, float volume)
    {
        if (clip == null || typeAudioSource == null || volume <= 0f)
            return;

        float pitch = Random.Range(typePitchMin, typePitchMax);
        typeAudioSource.pitch = pitch;
        typeAudioSource.PlayOneShot(clip, volume);
        typeAudioSource.pitch = 1f;
    }

    private void ActivateQuest01()
    {
        if (QuestManager.Instance == null)
            return;

        QuestManager.Instance.BeginStoryQuest(0);
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (overlayImage == null)
            return;

        Color color = overlayImage.color;
        color.a = alpha;
        overlayImage.color = color;
    }

    private static void SetPlayerFrozen(bool frozen)
    {
        if (PlayerInputManager.Instance != null)
            PlayerInputManager.Instance.isInputLocked = frozen;

        PlayerMovement movement = PlayerRegistry.Instance?.Movement;
        if (movement != null)
            movement.enabled = !frozen;

        if (ShopManager.Instance != null)
            ShopManager.Instance.SetPlayerFreeze(frozen);
    }
}
