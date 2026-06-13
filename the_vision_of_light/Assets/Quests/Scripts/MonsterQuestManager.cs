using UnityEngine;

/// <summary>
/// Drives the "kill monsters" objective. While the story is on <see cref="targetQuestState"/> it
/// reveals a group of monsters, counts the kills reported through <see cref="MonsterKilled"/>, and
/// advances the quest once the required number have been defeated.
/// </summary>
public class MonsterQuestManager : MonoBehaviour
{
    #region Singleton
    public static MonsterQuestManager Instance;
    #endregion

    #region Quest Settings
    [Header("Quest Settings")]
    /// <summary>The story state during which this combat objective is active.</summary>
    public int targetQuestState = 3;
    /// <summary>-1 = match state only (legacy). Set to 0+ to require a specific quest step.</summary>
    public int targetQuestStep = -1;

    /// <summary>Number of monsters the player must kill to complete the objective.</summary>
    public int monstersToKill = 3;

    /// <summary>Running kill count for the current objective (not persisted between sessions).</summary>
    private int currentKills = 0;
    #endregion

    #region Monster Spawning
    [Header("Monster Spawning")]
    /// <summary>Parent object holding the quest monsters; toggled active when the objective starts.</summary>
    public GameObject monstersGroup;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (monstersGroup != null)
        {
            monstersGroup.SetActive(false);
        }
    }

    /// <summary>
    /// Activates the monster group once the story reaches the target state and the objective is unfinished.
    /// </summary>
    private void Update()
    {
        if (QuestManager.Instance != null && IsObjectiveActive())
        {
            if (monstersGroup != null && !monstersGroup.activeSelf && currentKills < monstersToKill)
            {
                monstersGroup.SetActive(true);
            }
        }
    }
    #endregion

    #region Objective Logic
    /// <summary>
    /// Reports a quest monster death. Counts it only while the objective is active and completes the
    /// objective once enough monsters have been killed.
    /// </summary>
    public void MonsterKilled()
    {
        if (QuestManager.Instance != null && IsObjectiveActive())
        {
            currentKills++;

            if (currentKills >= monstersToKill)
            {
                CompleteObjective();
            }
        }
    }

    private bool IsObjectiveActive()
    {
        if (targetQuestStep < 0)
            return QuestManager.Instance.mainQuestState == targetQuestState;

        return QuestManager.Instance.IsAtQuestStep(targetQuestState, targetQuestStep);
    }

    /// <summary>
    /// Advances the story past the combat objective and saves progress.
    /// </summary>
    private void CompleteObjective()
    {
        if (targetQuestStep < 0)
            QuestManager.Instance.AdvanceToState(4);
        else
            QuestManager.Instance.AdvanceStep(targetQuestState, targetQuestStep);
    }
    #endregion
}
