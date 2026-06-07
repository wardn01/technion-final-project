using UnityEngine;

/// <summary>
/// Generic world trigger that advances a quest step when the player enters a location.
/// Use for "visit this place" objectives (graves, shop area, etc.).
/// </summary>
public class QuestStepTrigger : MonoBehaviour
{
    [Header("Quest Requirements")]
    public int requiredState;
    public int requiredStep;

    [Header("Behaviour")]
    public bool completeQuestOnTrigger;
    public QuestData questForRewards;

    [Header("Optional")]
    public bool disableAfterTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || QuestManager.Instance == null) return;
        if (!QuestManager.Instance.IsAtQuestStep(requiredState, requiredStep)) return;

        if (completeQuestOnTrigger)
            QuestManager.Instance.CompleteCurrentQuest(questForRewards);
        else
            QuestManager.Instance.AdvanceStep(requiredState, requiredStep);

        if (disableAfterTrigger)
            gameObject.SetActive(false);
    }
}
