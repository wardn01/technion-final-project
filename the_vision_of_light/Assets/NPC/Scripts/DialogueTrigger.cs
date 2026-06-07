using UnityEngine;
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

    public enum QuestDialogueAction
    {
        None,
        AdvanceStep,
        CompleteQuest
    }

    [System.Serializable]
    public class QuestDialogueEntry
    {
        public int stateId;
        public int requiredStep;
        public DialogueData dialogue;
        public QuestData questData;
        public QuestDialogueAction onComplete = QuestDialogueAction.None;
    }

    private void Start()
    {
        if (QuestManager.Instance != null && QuestManager.Instance.mainQuestState >= 2)
        {
            if (outsideLocation != null)
            {
                transform.position = outsideLocation.position;
                transform.rotation = outsideLocation.rotation;
            }
        }
    }

    public void TriggerDialogue()
    {
        StoryNPC storyNPC = GetComponent<StoryNPC>();
        Animator npcAnim = storyNPC != null ? storyNPC.GetComponentInChildren<Animator>() : null;

        if (QuestManager.Instance != null)
        {
            int currentState = QuestManager.Instance.mainQuestState;
            int currentStep = QuestManager.Instance.questStepIndex;

            QuestDialogueEntry entry = dialogueStates.Find(
                x => x.stateId == currentState && x.requiredStep == currentStep);

            if (entry == null)
                entry = dialogueStates.Find(x => x.stateId == currentState && x.requiredStep < 0);

            if (entry != null && entry.dialogue != null && DialogueManager.Instance != null)
            {
                System.Action onDialogueComplete = BuildDialogueCallback(entry, currentState);
                bool isQuest = onDialogueComplete != null;

                DialogueManager.Instance.StartDialogue(
                    entry.dialogue.npcName,
                    entry.dialogue.dialogueLines,
                    false,
                    null,
                    npcAnim,
                    npcFocusCamera,
                    playerFocusCamera,
                    onDialogueComplete,
                    isQuest
                );
                return;
            }
        }

        if (storyNPC != null && storyNPC.myData != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(
                storyNPC.myData.npcName,
                storyNPC.myData.welcomeDialogue,
                false,
                null,
                npcAnim,
                null,
                null,
                null,
                false
            );
            return;
        }

        ShopkeeperNPC shopNPC = GetComponent<ShopkeeperNPC>();
        if (shopNPC != null && shopNPC.myData != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(
                shopNPC.myData.npcName,
                shopNPC.myData.welcomeDialogue,
                true,
                shopNPC,
                null,
                null,
                null,
                null,
                false
            );
        }
    }

    private System.Action BuildDialogueCallback(QuestDialogueEntry entry, int currentState)
    {
        switch (entry.onComplete)
        {
            case QuestDialogueAction.AdvanceStep:
                return () => QuestManager.Instance.AdvanceStep(entry.stateId, entry.requiredStep);

            case QuestDialogueAction.CompleteQuest:
                return () => QuestManager.Instance.CompleteCurrentQuest(entry.questData);

            case QuestDialogueAction.None when entry.questData != null:
                return () => QuestManager.Instance.CompleteCurrentQuest(entry.questData);

            case QuestDialogueAction.None when currentState == 2:
                return () => QuestManager.Instance.AdvanceToState(3);

            default:
                return null;
        }
    }
}
