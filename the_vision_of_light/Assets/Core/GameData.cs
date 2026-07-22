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
    public bool hasSavedPlayerPosition;
    public float currentTime;
    public string worldName;
    public List<SavedItem> inventoryItems = new List<SavedItem>();
    
    public string playerDataJson;

    public int mainQuestState;
    public int questStepIndex;

    /// <summary>Chapter 1 bed cinematic finished — skip intro/awakening on reload.</summary>
    public bool hasCompletedChapter01Awakening;

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

    /// <summary>Unlocked world teleport point IDs for this save slot.</summary>
    public List<int> unlockedTeleportIds = new List<int>();

    /// <summary>When all guardians were defeated per chest (UTC seconds) for hourly respawn.</summary>
    public List<ChestGuardianDefeatTime> chestGuardianDefeatTimes = new List<ChestGuardianDefeatTime>();

    /// <summary>Lifetime combat / exploration achievement counters for this save slot.</summary>
    public PlayerStatistics playerStatistics = new PlayerStatistics();
}

[System.Serializable]
public class PlayerStatistics
{
    public int totalEnemiesKilled;
    public float totalDamageDealt;
    public float highestSingleDamage;
    public float windDamageDealt;
    public float fireDamageDealt;
    public float iceDamageDealt;
    public int chestsOpened;
    public int wavesCleared;
    public int timesDied;
    public int potionsConsumed;

    public void Reset()
    {
        totalEnemiesKilled = 0;
        totalDamageDealt = 0f;
        highestSingleDamage = 0f;
        windDamageDealt = 0f;
        fireDamageDealt = 0f;
        iceDamageDealt = 0f;
        chestsOpened = 0;
        wavesCleared = 0;
        timesDied = 0;
        potionsConsumed = 0;
    }

    public void CopyFrom(PlayerStatistics other)
    {
        if (other == null)
        {
            Reset();
            return;
        }

        totalEnemiesKilled = other.totalEnemiesKilled;
        totalDamageDealt = other.totalDamageDealt;
        highestSingleDamage = other.highestSingleDamage;
        windDamageDealt = other.windDamageDealt;
        fireDamageDealt = other.fireDamageDealt;
        iceDamageDealt = other.iceDamageDealt;
        chestsOpened = other.chestsOpened;
        wavesCleared = other.wavesCleared;
        timesDied = other.timesDied;
        potionsConsumed = other.potionsConsumed;
    }
}

[System.Serializable]
public class ChestGuardianDefeatTime
{
    public string chestId;
    public double defeatedAtUtc;
}
