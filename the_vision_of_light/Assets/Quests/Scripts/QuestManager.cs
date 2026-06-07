using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Central authority for story progression. Each <see cref="QuestData"/> chapter is active while
/// <see cref="mainQuestState"/> matches its <c>stateId</c>. Within that chapter,
/// <see cref="questStepIndex"/> tracks which objective the player is on.
/// </summary>
public class QuestManager : MonoBehaviour
{
    #region Singleton
    public static QuestManager Instance;
    #endregion

    #region Quest State
    [Header("Quest State")]
    public int mainQuestState = 0;
    public int questStepIndex = 0;

    private int lastQuestState = -1;
    private int lastQuestStep = -1;
    #endregion

    #region Quest Data
    [Header("Quest Data")]
    public List<QuestData> allQuestLibrary;
    public QuestData trackedQuest = null;
    #endregion

    #region Settings
    [Header("Settings")]
    public bool autoTrackNewQuests = true;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (WorldSaveManager.Instance != null)
            WorldSaveManager.Instance.ApplyPendingQuestProgress();

        lastQuestState = mainQuestState;
        lastQuestStep = questStepIndex;
        SyncTrackedQuest();
    }

    private void Update()
    {
        if (mainQuestState != lastQuestState || questStepIndex != lastQuestStep)
        {
            lastQuestState = mainQuestState;
            lastQuestStep = questStepIndex;

            if (autoTrackNewQuests)
                SyncTrackedQuest();
        }
    }
    #endregion

    #region Quest Lookup
    public QuestData GetActiveQuest()
    {
        if (allQuestLibrary == null) return null;

        foreach (QuestData quest in allQuestLibrary)
        {
            if (quest != null && quest.stateId == mainQuestState)
                return quest;
        }

        return null;
    }

    public string GetCurrentObjectiveText()
    {
        QuestData quest = GetActiveQuest();
        return quest != null ? quest.GetDescriptionForStep(questStepIndex) : string.Empty;
    }

    public bool CurrentObjectiveHasTarget()
    {
        QuestData quest = GetActiveQuest();
        return quest != null && quest.HasTargetForStep(questStepIndex);
    }

    public Vector3 GetCurrentObjectiveTarget()
    {
        QuestData quest = GetActiveQuest();
        return quest != null ? quest.GetTargetForStep(questStepIndex) : Vector3.zero;
    }

    public bool IsAtQuestStep(int state, int step)
    {
        return mainQuestState == state && questStepIndex == step;
    }

    private void SyncTrackedQuest()
    {
        if (allQuestLibrary == null || allQuestLibrary.Count == 0) return;

        foreach (QuestData quest in allQuestLibrary)
        {
            if (quest != null && quest.stateId == mainQuestState)
            {
                trackedQuest = quest;
                return;
            }
        }
    }
    #endregion

    #region Step Progression
    /// <summary>
    /// Moves to the next objective inside the current quest chapter.
    /// </summary>
    public void AdvanceStep(int expectedState = -1, int expectedStep = -1)
    {
        if (expectedState >= 0 && mainQuestState != expectedState) return;
        if (expectedStep >= 0 && questStepIndex != expectedStep) return;

        QuestData quest = GetActiveQuest();
        if (quest == null) return;

        if (questStepIndex + 1 >= quest.StepCount)
        {
            Debug.LogWarning($"[QuestManager] Cannot advance step on final objective of quest '{quest.questTitle}'. Use CompleteCurrentQuest instead.");
            return;
        }

        questStepIndex++;
        SaveQuestProgress();
        Debug.Log($"[QuestManager] Step advanced: {quest.questTitle} ({questStepIndex + 1}/{quest.StepCount})");
    }

    /// <summary>
    /// Finishes the active quest chapter, grants rewards, and moves to the next chapter.
    /// </summary>
    public void CompleteCurrentQuest(QuestData rewardSource = null)
    {
        QuestData quest = rewardSource != null ? rewardSource : GetActiveQuest();
        if (quest == null) return;

        GrantRewards(quest);
        questStepIndex = 0;
        AdvanceToState(mainQuestState + 1);
    }

    /// <summary>
    /// Advances the story to a new chapter. Resets the step index to 0.
    /// </summary>
    public void AdvanceToState(int newState)
    {
        if (newState <= mainQuestState) return;

        mainQuestState = newState;
        questStepIndex = 0;
        lastQuestState = newState;
        lastQuestStep = 0;

        SaveQuestProgress();

        if (autoTrackNewQuests)
            SyncTrackedQuest();
    }

    private void GrantRewards(QuestData quest)
    {
        if (quest.rewards == null) return;

        foreach (QuestReward reward in quest.rewards)
        {
            if (reward.item == null || InventoryManager.Instance == null) continue;

            InventoryManager.Instance.AddItem(reward.item, reward.amount);

            if (reward.item is WeaponItemData weapon && QuickSlotManager.Instance != null)
                QuickSlotManager.Instance.AssignToFirstEmptySlot(weapon);
        }
    }

    #endregion

    #region Persistence
    public void SaveQuestProgress()
    {
        if (WorldSaveManager.Instance != null)
            WorldSaveManager.Instance.SaveCurrentWorld();
    }
    #endregion
}
