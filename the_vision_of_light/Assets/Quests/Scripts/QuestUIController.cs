using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the full quest log screen: builds the list of discovered quests, shows the selected
/// quest's details (status, description, rewards, live distance), and manages the "track / locate"
/// button that focuses the world map on the objective.
/// </summary>
public class QuestUIController : MonoBehaviour
{
    #region Singleton
    public static QuestUIController Instance { get; private set; }
    #endregion

    #region Quest List UI
    [Header("Quest List UI")]
    /// <summary>Parent that holds the instantiated quest rows.</summary>
    public Transform contentParent;

    /// <summary>Prefab used for each quest row.</summary>
    public QuestButton questButtonPrefab;
    #endregion

    #region Details View UI
    [Header("Details View UI")]
    public TextMeshProUGUI infoTitle;
    public TextMeshProUGUI infoDesc;
    public TextMeshProUGUI distanceText;
    #endregion

    #region Location Tracking UI
    [Header("Location Tracking UI")]
    public Button locationBtn;
    public TextMeshProUGUI locationBtnText;
    #endregion

    #region Rewards UI
    [Header("Rewards UI")]
    public Transform rewardsContainer;
    public GameObject rewardSlotPrefab;
    #endregion

    #region Player Reference
    [Header("Player Reference")]
    public Transform player;
    #endregion

    /// <summary>The quest currently shown in the details view.</summary>
    private QuestData currentDisplayedQuest;

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        EnsureScrollLayout();
        RefreshQuestUI();
    }

    /// <summary>
    /// Keeps the details view's distance readout updated for quests with a target location.
    /// </summary>
    private void Update()
    {
        if (currentDisplayedQuest != null && player != null && distanceText != null)
        {
            bool showDistance = QuestManager.Instance != null
                && currentDisplayedQuest.stateId == QuestManager.Instance.mainQuestState
                && QuestManager.Instance.CurrentObjectiveHasTarget();

            if (showDistance)
            {
                distanceText.gameObject.SetActive(true);
                float dist = Vector3.Distance(player.position, QuestManager.Instance.GetCurrentObjectiveTarget());
                distanceText.text = Mathf.RoundToInt(dist).ToString() + "m";
            }
        }
        else if (distanceText != null)
        {
            distanceText.gameObject.SetActive(false);
        }
    }
    #endregion

    #region List Population
    /// <summary>Rebuilds the entire quest log.</summary>
    public void RefreshQuestUI()
    {
        PopulateList();
    }

    /// <summary>
    /// Clears and rebuilds the quest rows from the library, including every quest up to the current
    /// state (completed + active). Auto-selects the active quest, or shows an empty state if none.
    /// </summary>
    public void PopulateList()
    {
        if (contentParent == null || questButtonPrefab == null)
        {
            Debug.LogWarning("[QuestUIController] contentParent or questButtonPrefab is not assigned.");
            return;
        }

        foreach (Transform child in contentParent) Destroy(child.gameObject);

        if (QuestManager.Instance == null || QuestManager.Instance.allQuestLibrary == null)
            return;

        int currentState = QuestManager.Instance.mainQuestState;
        QuestData activeQuest = null;

        foreach (var quest in QuestManager.Instance.allQuestLibrary)
        {
            if (quest == null) continue;

            // Only quests the player has reached (completed or currently active) appear in the log.
            if (quest.stateId <= currentState)
            {
                bool isActive = (quest.stateId == currentState);
                QuestButton btn = Instantiate(questButtonPrefab, contentParent);
                btn.gameObject.SetActive(true);
                ConfigureQuestRowLayout(btn.gameObject);
                btn.Setup(quest, isActive, ShowQuestDetails);

                if (isActive) activeQuest = quest;
            }
        }

        RebuildScrollContent();

        if (activeQuest != null) ShowQuestDetails(activeQuest);
        else
        {
            currentDisplayedQuest = null;
            if (infoTitle != null) infoTitle.text = "No Active Quests";
            if (infoDesc != null) infoDesc.text = "";
            if (distanceText != null) distanceText.gameObject.SetActive(false);
            if (rewardsContainer != null)
            {
                foreach (Transform child in rewardsContainer) Destroy(child.gameObject);
            }
            if (locationBtn != null) locationBtn.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Ensures the scroll content area grows with its children. Without a ContentSizeFitter the
    /// Content height stays fixed and the ScrollRect cannot reach quests below the fold.
    /// </summary>
    private void EnsureScrollLayout()
    {
        if (contentParent == null) return;

        ContentSizeFitter fitter = contentParent.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = contentParent.gameObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup layoutGroup = contentParent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
        }
    }

    /// <summary>
    /// Gives each quest row a stable height so the VerticalLayoutGroup can calculate total content size.
    /// </summary>
    private void ConfigureQuestRowLayout(GameObject row)
    {
        LayoutElement layoutElement = row.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = row.AddComponent<LayoutElement>();

        layoutElement.minHeight = 100f;
        layoutElement.preferredHeight = 100f;
        layoutElement.flexibleWidth = 1f;
    }

    /// <summary>
    /// Rebuilds layout and resets the scroll position to the top after the list changes.
    /// </summary>
    private void RebuildScrollContent()
    {
        if (contentParent is not RectTransform contentRect) return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        Canvas.ForceUpdateCanvases();

        ScrollRect scrollRect = contentParent.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }
    #endregion

    #region Details View
    /// <summary>
    /// Populates the details panel for a quest: title, status-tagged description, the track/locate
    /// button (active quests only), and the reward icons. Toggling tracking focuses the world map.
    /// </summary>
    public void ShowQuestDetails(QuestData quest)
    {
        currentDisplayedQuest = quest;
        infoTitle.text = quest.questTitle;

        bool isActive = QuestManager.Instance != null && (quest.stateId == QuestManager.Instance.mainQuestState);
        string statusText = isActive ? "<color=yellow>Status: In Progress</color>\n\n" : "<color=green>Status: Completed</color>\n\n";
        string objectiveText = isActive
            ? quest.GetDescriptionForStep(QuestManager.Instance.questStepIndex)
            : quest.questDescription;

        infoDesc.text = statusText + objectiveText;

        if (locationBtn != null)
        {
            if (isActive)
            {
                locationBtn.gameObject.SetActive(true);
                locationBtn.onClick.RemoveAllListeners();

                bool isTracked = (QuestManager.Instance.trackedQuest == quest);

                if (locationBtnText != null)
                {
                    locationBtnText.text = isTracked ? "Tracking..." : "Location";
                    locationBtnText.color = isTracked ? Color.yellow : Color.white;
                }

                locationBtn.onClick.AddListener(() =>
                {
                    // Toggle tracking: clicking an already-tracked quest stops tracking it.
                    if (QuestManager.Instance.trackedQuest == quest)
                    {
                        QuestManager.Instance.trackedQuest = null;
                    }
                    else
                    {
                        QuestManager.Instance.trackedQuest = quest;

                        // Jump straight to the objective on the world map, closing the pause UI first.
                        if (quest.stateId == QuestManager.Instance.mainQuestState
                            && QuestManager.Instance.CurrentObjectiveHasTarget()
                            && FullMapController.Instance != null)
                        {
                            if (PauseMenuManager.Instance != null)
                            {
                                PauseMenuManager.Instance.CloseAllSubScreens();

                                if (PauseMenuManager.Instance.isPaused)
                                {
                                    PauseMenuManager.Instance.Resume();
                                }
                            }

                            FullMapController.Instance.OpenMapToPosition(QuestManager.Instance.GetCurrentObjectiveTarget());
                        }
                    }

                    ShowQuestDetails(quest);
                });
            }
            else
            {
                locationBtn.gameObject.SetActive(false);
            }
        }

        // Rebuild reward icons for the selected quest.
        foreach (Transform child in rewardsContainer) Destroy(child.gameObject);

        if (quest.rewards != null && quest.rewards.Count > 0)
        {
            foreach (var reward in quest.rewards)
            {
                if (reward.item == null) continue;

                GameObject slot = Instantiate(rewardSlotPrefab, rewardsContainer);
                slot.SetActive(true);

                Transform iconTr = slot.transform.Find("ItemIcon");
                Transform amountTr = slot.transform.Find("ItemAmountText");

                if (iconTr != null)
                {
                    Image img = iconTr.GetComponent<Image>();
                    img.sprite = reward.item.itemIcon;
                    img.color = Color.white;
                }

                if (amountTr != null)
                {
                    TextMeshProUGUI amountTxt = amountTr.GetComponent<TextMeshProUGUI>();
                    amountTxt.text = reward.amount > 1 ? reward.amount.ToString() : "";
                }
            }
        }
    }
    #endregion
}
