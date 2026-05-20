using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Game Data/Dialogue")]
public class DialogueData : ScriptableObject
{
    public string npcName;
    [TextArea(3, 10)]
    public string[] dialogueLines;
}