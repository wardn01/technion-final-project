using UnityEngine;

public class GraveTrigger : MonoBehaviour
{
    [Header("Quest Data (legacy scene setup)")]
    public QuestData questToComplete;

    [Header("Quest Step (optional — set requiredStep >= 0 to use step mode)")]
    public int requiredState = 0;
    public int requiredStep = -1;

    [Header("Albedo Relocation")]
    public Transform albedoNPC;
    public Transform outsideLocation;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || QuestManager.Instance == null) return;

        if (requiredStep >= 0)
        {
            if (!QuestManager.Instance.IsAtQuestStep(requiredState, requiredStep)) return;
            QuestManager.Instance.AdvanceStep(requiredState, requiredStep);
        }
        else if (questToComplete != null && QuestManager.Instance.mainQuestState == 1)
        {
            QuestManager.Instance.CompleteCurrentQuest(questToComplete);
            Debug.Log("Graves Quest Completed! Rewards Given.");
        }
        else
        {
            return;
        }

        if (albedoNPC != null && outsideLocation != null)
        {
            albedoNPC.position = outsideLocation.position;
            albedoNPC.rotation = outsideLocation.rotation;
        }

        gameObject.SetActive(false);
    }
}
