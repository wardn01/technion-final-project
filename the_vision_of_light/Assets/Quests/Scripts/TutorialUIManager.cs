using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Shows context-sensitive tutorial hints tied to the current quest chapter and step.
/// Each entry maps a <c>questStateId</c> + <c>requiredStep</c> pair to hint text. Placeholders
/// like <c>{MoveForward}</c> are replaced at runtime with the player's rebound keys.
/// </summary>
public class TutorialUIManager : MonoBehaviour
{
    /// <summary>A tutorial hint shown while the story is on a specific quest state and step.</summary>
    [System.Serializable]
    public class TutorialStep
    {
        /// <summary>Quest chapter (<see cref="QuestManager.mainQuestState"/>).</summary>
        public int questStateId;

        /// <summary>Objective index within the chapter. -1 = show for every step in this chapter.</summary>
        public int requiredStep = -1;

        /// <summary>Hint text; may contain {KeyAction} placeholders resolved from the keybinds.</summary>
        [TextArea(3, 5)]
        public string tutorialText;
    }

    #region Tutorials Setup
    [Header("Tutorials Setup (legacy)")]
    [Tooltip("Optional fallback. Tutorial text on Quest Data steps takes priority.")]
    public TutorialStep[] tutorials;
    #endregion

    #region UI References
    [Header("UI References")]
    public RectTransform tutorialPanelRect;
    public TextMeshProUGUI tutorialTextUI;
    #endregion

    #region Animation Settings
    [Header("Animation Settings")]
    /// <summary>Slide speed in UI units per second.</summary>
    public float slideSpeed = 1500f;

    /// <summary>Horizontal distance the panel travels when hidden off-screen.</summary>
    public float slideOffset = 800f;
    #endregion

    #region Internal State
    private int currentActiveState = -1;
    private int currentActiveStep = -1;
    private bool isTransitioning = false;

    private Vector2 visiblePosition;
    private Vector2 hiddenLeft;

    /// <summary>Fallback key labels used when KeybindManager is not present in the scene.</summary>
    private static readonly Dictionary<string, KeyCode> DefaultKeys = new Dictionary<string, KeyCode>
    {
        { "MoveForward", KeyCode.W },
        { "MoveBackward", KeyCode.S },
        { "MoveLeft", KeyCode.A },
        { "MoveRight", KeyCode.D },
        { "NormalAttack", KeyCode.Mouse0 },
        { "Skill", KeyCode.E },
        { "Burst", KeyCode.Q },
        { "OpenInventory", KeyCode.I },
        { "Interact", KeyCode.F },
    };
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Caches the on-screen position, computes the hidden position, and starts hidden.
    /// </summary>
    void Start()
    {
        if (tutorialPanelRect != null)
        {
            visiblePosition = tutorialPanelRect.anchoredPosition;
            hiddenLeft = new Vector2(visiblePosition.x - slideOffset, visiblePosition.y);

            tutorialPanelRect.anchoredPosition = hiddenLeft;
            tutorialPanelRect.gameObject.SetActive(false);
        }

        StartCoroutine(ShowInitialTutorial());
    }

    /// <summary>
    /// Watches story progress and runs a transition whenever the active tutorial entry changes.
    /// </summary>
    void Update()
    {
        if (QuestManager.Instance == null || isTransitioning) return;

        int state = QuestManager.Instance.mainQuestState;
        int step = QuestManager.Instance.questStepIndex;

        if (state != currentActiveState || step != currentActiveStep)
            StartCoroutine(TransitionTutorial(state, step));
    }
    #endregion

    #region State Lookup
    /// <summary>
    /// Shows the first matching tutorial after startup, once managers have finished initializing.
    /// </summary>
    private IEnumerator ShowInitialTutorial()
    {
        yield return null;

        if (QuestManager.Instance == null || isTransitioning) yield break;

        int state = QuestManager.Instance.mainQuestState;
        int step = QuestManager.Instance.questStepIndex;

        if (state != currentActiveState || step != currentActiveStep)
            yield return StartCoroutine(TransitionTutorial(state, step));
    }

    /// <summary>
    /// Picks the best tutorial entry: exact state+step first, then a chapter-wide entry (requiredStep = -1).
    /// </summary>
    private bool TryResolveTutorial(int state, int step, out string formattedText)
    {
        formattedText = string.Empty;
        if (!CanShowTutorial(state, step))
            return false;

        QuestData quest = QuestManager.Instance?.GetActiveQuest();
        if (quest != null)
        {
            if (quest.TryGetTutorialForStep(step, out string questTutorial))
            {
                formattedText = FormatTutorialText(questTutorial);
                return !string.IsNullOrEmpty(formattedText);
            }

            if (quest.steps != null && step >= 0 && step < quest.steps.Count)
                return false;
        }

        if (tutorials == null)
            return false;

        TutorialStep chapterFallback = null;

        foreach (TutorialStep entry in tutorials)
        {
            if (entry == null || entry.questStateId != state) continue;

            if (entry.requiredStep == step)
            {
                formattedText = FormatTutorialText(entry.tutorialText);
                return !string.IsNullOrEmpty(formattedText);
            }

            if (entry.requiredStep < 0)
                chapterFallback = entry;
        }

        if (chapterFallback != null)
        {
            formattedText = FormatTutorialText(chapterFallback.tutorialText);
            return !string.IsNullOrEmpty(formattedText);
        }

        return false;
    }

    private static bool CanShowTutorial(int state, int step)
    {
        if (state != 0)
            return true;

        if (!IntroCutsceneManager.HasFinishedIntro)
            return false;

        return AwakeningManager.HasCompletedAwakening;
    }
    #endregion

    #region Transitions
    /// <summary>
    /// Slides the current hint out, looks up the hint for the new state/step, and slides it in (if any).
    /// </summary>
    IEnumerator TransitionTutorial(int newState, int newStep)
    {
        if (tutorialPanelRect == null) yield break;

        isTransitioning = true;

        if (tutorialPanelRect.gameObject.activeSelf)
            yield return StartCoroutine(SlideUI(tutorialPanelRect, hiddenLeft, true));

        yield return new WaitForSeconds(0.4f);

        currentActiveState = newState;
        currentActiveStep = newStep;

        if (TryResolveTutorial(newState, newStep, out string newText))
        {
            ApplyTutorialText(newText);
            tutorialPanelRect.anchoredPosition = hiddenLeft;
            yield return StartCoroutine(SlideUI(tutorialPanelRect, visiblePosition, false));
        }
        else
        {
            tutorialPanelRect.gameObject.SetActive(false);
        }

        isTransitioning = false;
    }

    /// <summary>
    /// Writes the tutorial text and expands the panel so multi-line hints remain visible.
    /// </summary>
    private void ApplyTutorialText(string newText)
    {
        if (tutorialTextUI == null) return;

        tutorialTextUI.richText = true;
        tutorialTextUI.text = newText;
        tutorialTextUI.ForceMeshUpdate();

        if (tutorialPanelRect != null)
        {
            float textHeight = tutorialTextUI.preferredHeight;
            tutorialPanelRect.sizeDelta = new Vector2(
                tutorialPanelRect.sizeDelta.x,
                Mathf.Max(80f, textHeight + 20f));
        }
    }

    /// <summary>
    /// Moves a RectTransform toward a target anchored position, optionally disabling it when done.
    /// </summary>
    IEnumerator SlideUI(RectTransform rect, Vector2 targetPos, bool hideAtEnd)
    {
        if (rect == null) yield break;
        if (!hideAtEnd) rect.gameObject.SetActive(true);

        while (Vector2.Distance(rect.anchoredPosition, targetPos) > 0.5f)
        {
            rect.anchoredPosition = Vector2.MoveTowards(rect.anchoredPosition, targetPos, slideSpeed * Time.deltaTime);
            yield return null;
        }

        rect.anchoredPosition = targetPos;
        if (hideAtEnd) rect.gameObject.SetActive(false);
    }
    #endregion

    #region Text Formatting
    /// <summary>
    /// Replaces {ActionName} placeholders in the hint with the player's current key for that action,
    /// formatted nicely and highlighted, so hints always reflect the active keybindings.
    /// </summary>
    string FormatTutorialText(string rawText)
    {
        if (string.IsNullOrEmpty(rawText)) return string.Empty;

        string formattedText = rawText;
        IEnumerable<KeyValuePair<string, KeyCode>> keySource = DefaultKeys;

        if (KeybindManager.Instance != null && KeybindManager.Instance.keys != null && KeybindManager.Instance.keys.Count > 0)
            keySource = KeybindManager.Instance.keys;

        foreach (var kvp in keySource)
        {
            string placeholder = "{" + kvp.Key + "}";
            if (formattedText.Contains(placeholder))
            {
                string niceKeyName = GetFriendlyKeyName(kvp.Value);
                formattedText = formattedText.Replace(placeholder, "<color=yellow>[" + niceKeyName + "]</color>");
            }
        }

        return formattedText;
    }

    /// <summary>
    /// Converts a <see cref="KeyCode"/> into a short, player-friendly label (e.g. Mouse0 -> "Left Click").
    /// </summary>
    string GetFriendlyKeyName(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Mouse0: return "Left Click";
            case KeyCode.Mouse1: return "Right Click";
            case KeyCode.Mouse2: return "Middle Click";
            case KeyCode.LeftShift: return "L-Shift";
            case KeyCode.Escape: return "Esc";
            case KeyCode.Alpha1: return "1";
            case KeyCode.Alpha2: return "2";
            case KeyCode.Alpha3: return "3";
            case KeyCode.Alpha4: return "4";
            default: return key.ToString();
        }
    }
    #endregion
}
