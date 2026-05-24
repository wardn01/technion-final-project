using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class QuestReward
{
    public ItemData item;
    public int amount;
}

[CreateAssetMenu(fileName = "New Quest", menuName = "Game Data/Quest/Quest Data")]
public class QuestData : ScriptableObject
{
    public int stateId;
    public string questTitle;
    [TextArea] public string questDescription;
    public List<QuestReward> rewards; 
}