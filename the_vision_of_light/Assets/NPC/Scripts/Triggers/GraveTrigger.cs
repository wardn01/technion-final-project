using UnityEngine;

public class GraveTrigger : MonoBehaviour
{
    [Header("Quest Data")]
    public QuestData questToComplete;

    [Header("Albedo Relocation")]
    public Transform albedoNPC;
    public Transform outsideLocation;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && QuestManager.Instance != null && QuestManager.Instance.mainQuestState == 1)
        {
            QuestManager.Instance.mainQuestState = 2;
            QuestManager.Instance.SaveQuestProgress();
            
            if (questToComplete != null && questToComplete.rewards != null)
            {
                foreach (var reward in questToComplete.rewards)
                {
                    if (reward.item != null && InventoryManager.Instance != null)
                    {
                        InventoryManager.Instance.AddItem(reward.item, reward.amount);
                        
                        if (reward.item is WeaponItemData weapon)
                            QuickSlotManager.Instance.AssignToFirstEmptySlot(weapon);
                    }
                }
            }
            
            if (albedoNPC != null && outsideLocation != null)
            {
                albedoNPC.position = outsideLocation.position;
                albedoNPC.rotation = outsideLocation.rotation;
            }

            Debug.Log("Graves Quest Completed! Rewards Given.");
            
            gameObject.SetActive(false);
        }
    }
}