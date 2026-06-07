using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// The on-screen HUD widget that shows the currently tracked quest (title, short description, and
/// live distance). When the tracked quest changes it slides the old panel out and the new one in,
/// and color-codes the distance as the player nears the objective.
/// </summary>
public class QuestTrackerUI : MonoBehaviour
{
    #region Singleton
    public static QuestTrackerUI Instance { get; private set; }
    #endregion

    #region UI References
    [Header("UI References")]
    public RectTransform trackerPanelRect;
    public TextMeshProUGUI questTitleText;
    public TextMeshProUGUI questDescText;
    public TextMeshProUGUI distanceText;
    #endregion

    #region Animation Settings
    [Header("Animation Settings")]
    /// <summary>Slide speed in UI units per second.</summary>
    public float slideSpeed = 1500f;

    /// <summary>Horizontal distance the panel travels when hidden off-screen.</summary>
    public float slideOffset = 800f;
    #endregion

    #region World References
    [Header("World References")]
    public Transform player;
    #endregion

    #region Internal State
    private QuestData currentDisplayedQuest = null;
    private int currentDisplayedStep = -1;
    private bool isTransitioning = false;

    private Vector2 visiblePosition;
    private Vector2 hiddenLeft;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Caches the on-screen position, computes the hidden position, and starts hidden.
    /// </summary>
    private void Start()
    {
        if (trackerPanelRect != null)
        {
            visiblePosition = trackerPanelRect.anchoredPosition;
            hiddenLeft = new Vector2(visiblePosition.x - slideOffset, visiblePosition.y);

            trackerPanelRect.anchoredPosition = hiddenLeft;
            trackerPanelRect.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Detects changes to the tracked quest and either runs the slide transition or just refreshes
    /// the live texts (e.g. distance) of the already displayed quest. Completed quests are dropped.
    /// </summary>
    private void Update()
    {
        if (isTransitioning) return;

        QuestData actualQuest = QuestManager.Instance != null ? QuestManager.Instance.trackedQuest : null;

        // A tracked quest whose id is behind the current state is already completed; stop showing it.
        if (actualQuest != null && actualQuest.stateId < QuestManager.Instance.mainQuestState)
        {
            actualQuest = null;
        }

        int actualStep = QuestManager.Instance != null ? QuestManager.Instance.questStepIndex : 0;

        if (actualQuest != currentDisplayedQuest || actualStep != currentDisplayedStep)
        {
            StartCoroutine(TransitionQuest(actualQuest, actualStep));
        }
        else if (actualQuest != null)
        {
            UpdateQuestTexts(actualQuest, actualStep);
        }
    }
    #endregion

    #region Transitions
    /// <summary>
    /// Slides the current panel out (if any), swaps to the new quest, then slides it in.
    /// </summary>
    IEnumerator TransitionQuest(QuestData newQuest, int newStep)
    {
        isTransitioning = true;

        if (currentDisplayedQuest != null && trackerPanelRect.gameObject.activeSelf)
        {
            yield return StartCoroutine(SlideUI(trackerPanelRect, hiddenLeft, true));
        }

        yield return new WaitForSeconds(0.4f);

        currentDisplayedQuest = newQuest;
        currentDisplayedStep = newStep;

        if (newQuest != null)
        {
            UpdateQuestTexts(newQuest, newStep);
            trackerPanelRect.anchoredPosition = hiddenLeft;
            yield return StartCoroutine(SlideUI(trackerPanelRect, visiblePosition, false));
        }

        isTransitioning = false;
    }

    void UpdateQuestTexts(QuestData quest, int stepIndex)
    {
        if (quest == null) return;

        questTitleText.text = quest.questTitle;
        string desc = quest.GetDescriptionForStep(stepIndex);
        string[] words = desc.Split(' ');

        if (words.Length > 7) questDescText.text = string.Join(" ", words, 0, 7) + " ...";
        else questDescText.text = desc;

        if (distanceText != null)
        {
            if (quest.HasTargetForStep(stepIndex) && player != null)
            {
                if (!distanceText.gameObject.activeSelf) distanceText.gameObject.SetActive(true);

                float distance = Vector3.Distance(player.position, quest.GetTargetForStep(stepIndex));
                distanceText.text = Mathf.RoundToInt(distance).ToString() + "m";

                if (distance < 15f) distanceText.color = Color.green;
                else distanceText.color = new Color(1f, 0.8f, 0f);
            }
            else
            {
                if (distanceText.gameObject.activeSelf) distanceText.gameObject.SetActive(false);
            }
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
}
