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

    /// <summary>One-time challenge stones that were cleared (trialId per stone).</summary>
    public List<string> completedOneTimeTrials = new List<string>();

    /// <summary>Repeatable training stones — cooldown + whether the linked quest was already cleared here.</summary>
    public List<RepeatableTrialSaveEntry> repeatableTrials = new List<RepeatableTrialSaveEntry>();
}

[System.Serializable]
public class RepeatableTrialSaveEntry
{
    public string trialId;
    /// <summary>Utc ticks when F becomes available again. 0 = ready now.</summary>
    public long nextAvailableUtcTicks;
    public bool questClearClaimed;
}
