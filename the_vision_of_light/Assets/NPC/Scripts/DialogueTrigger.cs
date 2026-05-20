using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public DialogueData dialogueAsset;

    public void TriggerDialogue()
    {
        if (dialogueAsset != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(dialogueAsset.npcName, dialogueAsset.dialogueLines);
        }
    }
}