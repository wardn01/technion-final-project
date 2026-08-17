using UnityEngine;

/// <summary>
/// Chapter 2 bootstrap. Add to <c>Quest02_Manager</c> under the central QuestManager hierarchy.
/// </summary>
[DisallowMultipleComponent]
public class Quest02ChapterManager : QuestChapterManager
{
    #region Chapter Identity
    public override int ChapterStateId => 1;
    #endregion

    #region Chapter Objectives
    [Header("Chapter 02 Objectives")]
    public QuestKillObjective beachMonsters;
    public QuestKillObjective millMonsters;
    public QuestShopObjective buyPotionObjective;
    #endregion

    #region Reference Resolution
    public override void ResolveReferences()
    {
        QuestKillObjective[] killObjectives = FindObjectsByType<QuestKillObjective>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (QuestKillObjective objective in killObjectives)
        {
            if (objective == null)
                continue;

            if (objective.requiredStep == 1)
                beachMonsters = objective;
            else if (objective.requiredStep == 5)
                millMonsters = objective;
        }

        buyPotionObjective ??= FindFirstObjectByType<QuestShopObjective>(FindObjectsInactive.Include);
    }
    #endregion
}
