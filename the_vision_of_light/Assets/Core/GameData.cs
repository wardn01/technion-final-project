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

    public bool hasSavedStamina;
    public float savedCurrentStamina;

    /// <summary>One-time challenge stones that were cleared (trialId per stone).</summary>
    public List<string> completedOneTimeTrials = new List<string>();

    /// <summary>Individual quest-gated challenge entries cleared (trialId:state:step).</summary>
    public List<string> completedQuestChallenges = new List<string>();

    /// <summary>World chests that were opened once (chestId per chest).</summary>
    public List<string> openedChestIds = new List<string>();
}
