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
    #region Matching
    public int stateId;
    public int requiredStep;
    #endregion

    #region Payload
    public DialogueData dialogue;
    public QuestData questData;

    [Tooltip("What happens when the player finishes this dialogue.")]
    public QuestDialogueAction onComplete = QuestDialogueAction.None;
    #endregion
}
