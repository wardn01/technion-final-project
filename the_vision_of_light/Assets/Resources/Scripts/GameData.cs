using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SavedItem
{
    public string itemName;
    public int amount;
}

[System.Serializable]
public class GameData
{
    public float[] playerPos = new float[3];
    public float currentTime;
    public string worldName;
    public List<SavedItem> inventoryItems = new List<SavedItem>();
    
    public string playerDataJson;

    public int mainQuestState;
    public int questStepIndex;

    /// <summary>True when this save includes a health value (false for older saves).</summary>
    public bool hasSavedHealth;
    public int savedCurrentHealth;
}