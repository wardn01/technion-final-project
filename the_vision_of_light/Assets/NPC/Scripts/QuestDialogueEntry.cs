using UnityEngine;

public enum QuestDialogueAction
{
    None,
    AdvanceStep,
    CompleteQuest
}

/// <summary>
/// One quest-specific dialogue row for an NPC (state + step → dialogue + callback).
/// </summary>
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
