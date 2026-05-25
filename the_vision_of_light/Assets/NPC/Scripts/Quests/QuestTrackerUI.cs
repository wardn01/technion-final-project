using UnityEngine;
using TMPro;

public class QuestTrackerUI : MonoBehaviour
{
    public static QuestTrackerUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject trackerPanel;
    public TextMeshProUGUI questTitleText;
    public TextMeshProUGUI questDescText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        UpdateTracker();
    }

    public void UpdateTracker()
    {
        if (QuestManager.Instance == null || QuestManager.Instance.trackedQuest == null)
        {
            trackerPanel.SetActive(false);
            return;
        }

        if (QuestManager.Instance.trackedQuest.stateId < QuestManager.Instance.mainQuestState)
        {
            QuestManager.Instance.trackedQuest = null;
            trackerPanel.SetActive(false);
            return;
        }

        trackerPanel.SetActive(true);
        questTitleText.text = QuestManager.Instance.trackedQuest.questTitle;

        string desc = QuestManager.Instance.trackedQuest.questDescription;
        string[] words = desc.Split(' ');
        
        if (words.Length > 7)
        {
            questDescText.text = string.Join(" ", words, 0, 7) + " ...";
        }
        else
        {
            questDescText.text = desc;
        }
    }
}