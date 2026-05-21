using UnityEngine;
using System.Collections.Generic;

public class DialogueTrigger : MonoBehaviour
{
    public List<QuestDialogueEntry> dialogueStates;

    public void TriggerDialogue()
    {
        if (QuestManager.Instance != null)
        {
            int currentState = QuestManager.Instance.mainQuestState;
            QuestDialogueEntry entry = dialogueStates.Find(x => x.stateId == currentState);

            if (entry != null && entry.dialogue != null)
            {
                DialogueManager.Instance.StartDialogue(entry.dialogue.npcName, entry.dialogue.dialogueLines);
                
                if (currentState == 0) QuestManager.Instance.mainQuestState = 1;
                else if (currentState == 2) QuestManager.Instance.mainQuestState = 3;
                return;
            }
        }

        StoryNPC storyNPC = GetComponent<StoryNPC>();
        if (storyNPC != null && storyNPC.myData != null)
        {
            DialogueManager.Instance.StartDialogue(storyNPC.myData.npcName, storyNPC.myData.welcomeDialogue, false, null);
            return;
        }

        ShopkeeperNPC shopNPC = GetComponent<ShopkeeperNPC>();
        if (shopNPC != null && shopNPC.myData != null)
        {
            DialogueManager.Instance.StartDialogue(shopNPC.myData.npcName, shopNPC.myData.welcomeDialogue, true, shopNPC);
        }
    }
}