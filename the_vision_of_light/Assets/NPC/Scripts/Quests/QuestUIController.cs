using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestUIController : MonoBehaviour
{
    public static QuestUIController Instance { get; private set; }

    [Header("Quest List UI")]
    public Transform contentParent; 
    public QuestButton questButtonPrefab; 
    
    [Header("Details View UI")]
    public TextMeshProUGUI infoTitle;
    public TextMeshProUGUI infoDesc;
    
    [Header("Location Tracking UI")]
    public Button locationBtn; 
    public TextMeshProUGUI locationBtnText; 

    [Header("Rewards UI")]
    public Transform rewardsContainer;
    public GameObject rewardSlotPrefab;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        RefreshQuestUI();
    }

    public void RefreshQuestUI()
    {
        PopulateList();
    }

    public void PopulateList()
    {
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        if (QuestManager.Instance != null && QuestManager.Instance.allQuestLibrary != null)
        {
            int currentState = QuestManager.Instance.mainQuestState;
            QuestData activeQuest = null; 

            foreach (var quest in QuestManager.Instance.allQuestLibrary)
            {
                if (quest.stateId <= currentState)
                {
                    bool isActive = (quest.stateId == currentState); 
                    QuestButton btn = Instantiate(questButtonPrefab, contentParent);
                    btn.Setup(quest, isActive, ShowQuestDetails);
                    
                    if (isActive) activeQuest = quest;
                }
            }

            if (activeQuest != null) ShowQuestDetails(activeQuest);
            else
            {
                infoTitle.text = "No Active Quests";
                infoDesc.text = "";
                foreach (Transform child in rewardsContainer) Destroy(child.gameObject);
                if (locationBtn != null) locationBtn.gameObject.SetActive(false);
            }
        }
    }

    public void ShowQuestDetails(QuestData quest)
    {
        infoTitle.text = quest.questTitle;
        
        bool isActive = QuestManager.Instance != null && (quest.stateId == QuestManager.Instance.mainQuestState);
        string statusText = isActive ? "<color=yellow>Status: In Progress</color>\n\n" : "<color=green>Status: Completed</color>\n\n";
        infoDesc.text = statusText + quest.questDescription;
        
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
                    if (QuestManager.Instance.trackedQuest == quest)
                    {
                        QuestManager.Instance.trackedQuest = null; 
                    }
                    else
                    {
                        QuestManager.Instance.trackedQuest = quest; 
                        
                        if (quest.hasTargetLocation && FullMapController.Instance != null)
                        {
                            var pauseNav = FindAnyObjectByType<PauseMenuNavigation>();
                            if (pauseNav != null && pauseNav.questScreen != null)
                            {
                                pauseNav.questScreen.SetActive(false);
                            }

                            if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.isPaused)
                            {
                                PauseMenuManager.Instance.Resume();
                            }

                            FullMapController.Instance.OpenMapToPosition(quest.targetLocation);
                        }
                    }
                    
                    if (QuestTrackerUI.Instance != null) QuestTrackerUI.Instance.UpdateTracker();
                    ShowQuestDetails(quest);
                });
            }
            else
            {
                locationBtn.gameObject.SetActive(false);
            }
        }
        
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
}