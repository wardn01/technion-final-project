using UnityEngine;
using System.Collections.Generic;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;
    
    [Header("Quest State")]
    public int mainQuestState = 0;
    private int lastQuestState = -1;

    [Header("Quest Data")]
    public List<QuestData> allQuestLibrary; 
    public QuestData trackedQuest = null; 

    [Header("Settings")]
    public bool autoTrackNewQuests = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        CheckAndAutoTrackQuest();
    }

    private void Update()
    {
        if (mainQuestState != lastQuestState)
        {
            lastQuestState = mainQuestState;
            
            if (autoTrackNewQuests)
            {
                CheckAndAutoTrackQuest();
            }
        }
    }

    private void CheckAndAutoTrackQuest()
    {
        if (allQuestLibrary == null || allQuestLibrary.Count == 0) return;

        foreach (QuestData quest in allQuestLibrary)
        {
            if (quest != null && quest.stateId == mainQuestState)
            {
                trackedQuest = quest;
                Debug.Log($"[QuestManager] Auto-Tracked New Quest: {quest.questTitle}");
                break;
            }
        }
    }

    public void SaveQuestProgress()
    {
        if (WorldSaveManager.Instance != null)
            WorldSaveManager.Instance.SaveCurrentWorld();
    }
}