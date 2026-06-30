using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;

/// <summary>
/// Picks quest-specific or default dialogue for an NPC and forwards it to
/// <see cref="DialogueManager"/>. Attach alongside <see cref="StoryNPC"/> or
/// <see cref="ShopkeeperNPC"/> on the same GameObject.
/// </summary>
public class DialogueTrigger : MonoBehaviour
{
    [Tooltip("Optional profile asset; overrides matching rows in dialogueStates.")]
    public NPCDialogueProfile dialogueProfile;

    [Tooltip("Quest/state-specific dialogue rows; matched by stateId + requiredStep.")]
    public List<QuestDialogueEntry> dialogueStates;

    [Header("Cinematic Dialogue Cameras")]
    public CinemachineCamera npcFocusCamera;
    public CinemachineCamera playerFocusCamera;

    [Header("Navigation")]
    [Tooltip("Optional world position for this NPC after a quest milestone (e.g. Villager outside the house).")]
    public Transform outsideLocation;

    [Tooltip("Move to Outside Location when mainQuestState matches this chapter.")]
    public int moveOutsideAtState = 0;

    [Tooltip("Move once questStepIndex reaches this value within moveOutsideAtState.")]
    public int moveOutsideAtMinStep = 2;

    private bool hasMovedOutside;

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

        [Tooltip("What happens when the player finishes this dialogue.")]
        public QuestDialogueAction onComplete = QuestDialogueAction.None;
    }

    private void Awake()
    {
        ApplyDialogueProfile();
    }

    private void Start()
    {
        TryMoveToOutsideLocation();
    }

    private void Update()
    {
        TryMoveToOutsideLocation();
    }

    private void TryMoveToOutsideLocation()
    {
        if (hasMovedOutside || outsideLocation == null || QuestManager.Instance == null)
            return;

        if (!ShouldMoveOutside())
            return;

        transform.position = outsideLocation.position;
        transform.rotation = outsideLocation.rotation;

        StoryNPC storyNPC = GetComponent<StoryNPC>();
        if (storyNPC != null)
            storyNPC.ApplyStandingPoseAfterRelocation();
        else
            ApplyStandingPoseFallback();

        hasMovedOutside = true;
    }

    private void ApplyStandingPoseFallback()
    {
        Animator anim = GetComponentInChildren<Animator>();
        if (anim == null)
            return;

        anim.SetBool("IsStanding", true);
        anim.SetBool("IsTalk", false);
        anim.SetBool("IsWalking", false);
        anim.Play("Idle", 0, 0f);
        anim.Update(0f);
    }

    private bool ShouldMoveOutside()
    {
        int state = QuestManager.Instance.mainQuestState;
        int step = QuestManager.Instance.questStepIndex;

        if (state == moveOutsideAtState && step >= moveOutsideAtMinStep)
            return true;

        // Legacy quests that used state 2+ before chapter-based state ids.
        return moveOutsideAtState == 0 && moveOutsideAtMinStep == 2 && state >= 2;
    }

    private void ApplyDialogueProfile()
    {
        if (dialogueProfile == null)
            dialogueProfile = TryLoadProfileFromResources();

        if (dialogueProfile == null || dialogueProfile.entries == null)
            return;

        if (dialogueStates == null)
            dialogueStates = new List<QuestDialogueEntry>();

        foreach (QuestDialogueEntry entry in dialogueProfile.entries)
        {
            if (entry == null)
                continue;

            int index = dialogueStates.FindIndex(
                x => x.stateId == entry.stateId && x.requiredStep == entry.requiredStep);

            if (index >= 0)
                dialogueStates[index] = entry;
            else
                dialogueStates.Add(entry);
        }
    }

    private NPCDialogueProfile TryLoadProfileFromResources()
    {
        StoryNPC storyNPC = GetComponent<StoryNPC>();
        if (storyNPC == null || storyNPC.myData == null)
            return null;

        NPCDialogueProfile[] profiles = Resources.LoadAll<NPCDialogueProfile>("DialogueProfiles");
        foreach (NPCDialogueProfile profile in profiles)
        {
            if (profile != null && profile.npcName == storyNPC.myData.npcName)
                return profile;
        }

        return null;
    }

    /// <summary>Called when the player presses Interact near this NPC.</summary>
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
                    isQuest);

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
                false);

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
                false);
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
