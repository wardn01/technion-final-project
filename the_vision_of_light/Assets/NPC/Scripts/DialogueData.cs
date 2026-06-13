using UnityEngine;

/// <summary>
/// ScriptableObject holding one NPC conversation block (name + lines).
/// Referenced by <see cref="DialogueTrigger"/> quest entries or welcome dialogue.
/// </summary>
[CreateAssetMenu(fileName = "New Dialogue", menuName = "Game Data/NPC/Dialogue")]
public class DialogueData : ScriptableObject
{
    [Tooltip("Speaker name shown in the dialogue panel header.")]
    public string npcName;

    [TextArea(3, 10)]
    [Tooltip("Lines shown one at a time; player clicks Continue between them.")]
    public string[] dialogueLines;
}
