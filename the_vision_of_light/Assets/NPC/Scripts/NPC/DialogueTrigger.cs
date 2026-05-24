using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;

public class DialogueTrigger : MonoBehaviour
{
    public List<QuestDialogueEntry> dialogueStates;

    [Header("Cinematic Dialogue Cameras")]
    public CinemachineCamera npcFocusCamera;
    public CinemachineCamera playerFocusCamera;

    [Header("Navigation")]
    public Transform outsideLocation;

    [System.Serializable]
    public class QuestDialogueEntry
    {
        public int stateId;
        public DialogueData dialogue;
        public QuestData questData;
    }

    public void TriggerDialogue()
    {
        StoryNPC storyNPC = GetComponent<StoryNPC>();
        Animator npcAnim = storyNPC != null ? storyNPC.GetComponentInChildren<Animator>() : null;

        if (QuestManager.Instance != null)
        {
            int currentState = QuestManager.Instance.mainQuestState;
            QuestDialogueEntry entry = dialogueStates.Find(x => x.stateId == currentState);

            if (entry != null && entry.dialogue != null)
            {
                System.Action onDialogueComplete = null;

                if (entry.questData != null)
                {
                    onDialogueComplete = () => StartCoroutine(CompleteQuestRoutine(entry.questData));
                }
                else if (currentState == 2)
                {
                    onDialogueComplete = () => {
                        QuestManager.Instance.mainQuestState = 3;
                        QuestManager.Instance.SaveQuestProgress();
                    };
                }

                DialogueManager.Instance.StartDialogue(
                    entry.dialogue.npcName, entry.dialogue.dialogueLines, false, null,
                    npcAnim, npcFocusCamera, playerFocusCamera, onDialogueComplete
                );
                return;
            }
        }

        if (storyNPC != null && storyNPC.myData != null)
        {
            DialogueManager.Instance.StartDialogue(
                storyNPC.myData.npcName, storyNPC.myData.welcomeDialogue, false, null,
                npcAnim, null, null, null
            );
            return;
        }

        ShopkeeperNPC shopNPC = GetComponent<ShopkeeperNPC>();
        if (shopNPC != null && shopNPC.myData != null)
        {
            DialogueManager.Instance.StartDialogue(
                shopNPC.myData.npcName, shopNPC.myData.welcomeDialogue, true, shopNPC,
                null, null, null
            );
        }
    }

    private IEnumerator CompleteQuestRoutine(QuestData questData)
    {
        QuestManager.Instance.mainQuestState += 1;
        QuestManager.Instance.SaveQuestProgress();

        if (questData.rewards != null)
        {
            foreach (var reward in questData.rewards)
            {
                if (reward.item != null && InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.AddItem(reward.item, reward.amount);
                    
                    if (reward.item is WeaponItemData weapon)
                        QuickSlotManager.Instance.AssignToFirstEmptySlot(weapon);
                }
            }
        }

        yield return new WaitForSeconds(0.5f);

        if (outsideLocation != null)
        {
            transform.position = outsideLocation.position;
            transform.rotation = outsideLocation.rotation;
        }
    }
}