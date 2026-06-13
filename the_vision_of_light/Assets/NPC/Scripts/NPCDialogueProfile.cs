using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject catalog of quest-specific dialogue rows for an NPC.
/// Assign on <see cref="DialogueTrigger"/> or place under
/// Resources/DialogueProfiles for automatic loading by NPC name.
/// </summary>
[CreateAssetMenu(fileName = "NPCDialogueProfile", menuName = "NPC/Dialogue Profile")]
public class NPCDialogueProfile : ScriptableObject
{
    [Tooltip("Must match NPCData.npcName (e.g. Albedo).")]
    public string npcName;

    public List<DialogueTrigger.QuestDialogueEntry> entries = new List<DialogueTrigger.QuestDialogueEntry>();
}
