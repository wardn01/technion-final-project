using UnityEngine;

public class QuestMonster : MonoBehaviour
{
    private bool isDead = false;

    public void OnMonsterDeath()
    {
        if (isDead) return;
        isDead = true;

        if (MonsterQuestManager.Instance != null)
        {
            MonsterQuestManager.Instance.MonsterKilled();
        }
    }
}