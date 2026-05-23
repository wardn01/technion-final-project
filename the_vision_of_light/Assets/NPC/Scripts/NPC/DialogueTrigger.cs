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

    [Header("Quest 1 Rewards")]
    public WeaponItemData startingWeapon;
    public string giveWeaponAnimTrigger = "GiveWeapon";

    public Transform outsideLocation;

    public void TriggerDialogue()
    {
        StoryNPC storyNPC = GetComponent<StoryNPC>();
        Animator npcAnim = storyNPC != null 
            ? storyNPC.GetComponentInChildren<Animator>() 
            : null;

        if (QuestManager.Instance != null)
        {
            int currentState = QuestManager.Instance.mainQuestState;

            QuestDialogueEntry entry = dialogueStates.Find(
                x => x.stateId == currentState
            );

            if (entry != null && entry.dialogue != null)
            {
                System.Action onDialogueComplete = null;

                if (currentState == 0)
                {
                    onDialogueComplete = () =>
                    {
                        StartCoroutine(QuestOneCompletionRoutine(npcAnim));
                    };
                }
                else if (currentState == 2)
                {
                    onDialogueComplete = () =>
                    {
                        QuestManager.Instance.mainQuestState = 3;
                        QuestManager.Instance.SaveQuestProgress();
                    };
                }

                DialogueManager.Instance.StartDialogue(
                    entry.dialogue.npcName,
                    entry.dialogue.dialogueLines,
                    false,
                    null,
                    npcAnim,
                    npcFocusCamera,
                    playerFocusCamera,
                    onDialogueComplete
                );

                return;
            }
        }

        if (storyNPC != null && storyNPC.myData != null)
        {
            DialogueManager.Instance.StartDialogue(
                storyNPC.myData.npcName,
                storyNPC.myData.welcomeDialogue,
                false,
                null,
                npcAnim,
                null,
                null
            );

            return;
        }

        ShopkeeperNPC shopNPC = GetComponent<ShopkeeperNPC>();

        if (shopNPC != null && shopNPC.myData != null)
        {
            DialogueManager.Instance.StartDialogue(
                shopNPC.myData.npcName,
                shopNPC.myData.welcomeDialogue,
                true,
                shopNPC,
                null,
                null,
                null
            );
        }
    }

    private IEnumerator QuestOneCompletionRoutine(Animator npcAnim)
    {
        QuestManager.Instance.mainQuestState = 1;
        QuestManager.Instance.SaveQuestProgress();

        if (npcAnim != null)
            npcAnim.SetTrigger(giveWeaponAnimTrigger);

        if (startingWeapon != null)
        {
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.AddItem(startingWeapon, 1);

            if (QuickSlotManager.Instance != null)
                QuickSlotManager.Instance.AssignToFirstEmptySlot(startingWeapon);
        }

        yield return new WaitForSeconds(2.0f);

        if (outsideLocation != null)
        {
            transform.position = outsideLocation.position;
            transform.rotation = outsideLocation.rotation;
        }
    }
}