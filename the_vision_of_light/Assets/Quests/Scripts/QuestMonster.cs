using UnityEngine;

/// <summary>
/// Marks an enemy as part of the kill objective. When the enemy dies it reports the kill to
/// <see cref="MonsterQuestManager"/> exactly once, guarding against double-counting.
/// </summary>
public class QuestMonster : MonoBehaviour
{
    /// <summary>Ensures the death is only reported a single time.</summary>
    private bool isDead = false;

    /// <summary>
    /// Called by the enemy's death logic; forwards a single kill notification to the quest manager.
    /// </summary>
    public void OnMonsterDeath()
    {
        if (isDead) return;
        isDead = true;

        QuestKillObjective objective = GetComponentInParent<QuestKillObjective>();
        if (objective != null)
        {
            objective.RegisterKill();
            return;
        }

        if (MonsterQuestManager.Instance != null)
            MonsterQuestManager.Instance.MonsterKilled();
    }
}
