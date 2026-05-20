using UnityEngine;
using System.Collections.Generic;

public class DialogueTrigger : MonoBehaviour
{
    public List<QuestDialogueEntry> dialogueStates;
    public DialogueData defaultDialogue;

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
            }
        }
        else if (defaultDialogue != null)
        {
            DialogueManager.Instance.StartDialogue(defaultDialogue.npcName, defaultDialogue.dialogueLines);
        }
    }
}