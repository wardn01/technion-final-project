using UnityEngine;
using System.Collections.Generic;

/// <summary>One inventory stack stored inside a save file (item name + amount).</summary>
[System.Serializable]
public class SavedItem
{
    public string itemName;
    public int amount;
}

/// <summary>
/// Serializable snapshot of one world slot: player, quests, inventory, teleports, chests, and stats.
/// Written/read by <see cref="SaveManager"/> as JSON.
/// </summary>
[System.Serializable]
public class GameData
{
    #region World
    public float[] playerPos = new float[3];
    public bool hasSavedPlayerPosition;
    public float currentTime;
    public string worldName;

    /// <summary>Last time this slot was entered/saved, shown in the Play menu (dd/MM/yyyy).</summary>
    public string lastJoinedDate;
    #endregion

    #region Inventory & Player
    public List<SavedItem> inventoryItems = new List<SavedItem>();
    
    public string playerDataJson;
    #endregion

    #region Quests
    public int mainQuestState;
    public int questStepIndex;

    /// <summary>Chapter 1 bed cinematic finished — skip intro/awakening on reload.</summary>
    public bool hasCompletedChapter01Awakening;
    #endregion

    #region Survival

    /// <summary>True when this save includes a health value (false for older saves).</summary>
    public bool hasSavedHealth;
    public int savedCurrentHealth;

    public bool hasSavedStamina;
    public float savedCurrentStamina;
    #endregion

    #region World Progress
    /// <summary>One-time challenge stones that were cleared (trialId per stone).</summary>
    public List<string> completedOneTimeTrials = new List<string>();

    /// <summary>Individual quest-gated challenge entries cleared (trialId:state:step).</summary>
    public List<string> completedQuestChallenges = new List<string>();

    /// <summary>World chests that were opened once (chestId per chest).</summary>
    public List<string> openedChestIds = new List<string>();

    /// <summary>Unlocked world teleport point IDs for this save slot.</summary>
    public List<int> unlockedTeleportIds = new List<int>();

    /// <summary>
    /// True once teleport data is slot-based. Old saves (false) may import legacy
    /// global PlayerPrefs unlocks once; new worlds start true so they never inherit them.
    /// </summary>
    public bool teleportDataMigrated;

    /// <summary>When all guardians were defeated per chest (UTC seconds) for hourly respawn.</summary>
    public List<ChestGuardianDefeatTime> chestGuardianDefeatTimes = new List<ChestGuardianDefeatTime>();

    /// <summary>Lifetime combat / exploration achievement counters for this save slot.</summary>
    public PlayerStatistics playerStatistics = new PlayerStatistics();
    #endregion
}

/// <summary>Per-species kill counter stored inside <see cref="PlayerStatistics"/>.</summary>
[System.Serializable]
public class MonsterKillEntry
{
    public string monsterId;
    public int killCount;
}

/// <summary>Lifetime combat and exploration counters for one save slot.</summary>
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

    /// <summary>Per-species kill counts keyed by EnemyBaseStats asset name (e.g. OrcData).</summary>
    public List<MonsterKillEntry> monsterKills = new List<MonsterKillEntry>();

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
        monsterKills = new List<MonsterKillEntry>();
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

        monsterKills = new List<MonsterKillEntry>();
        if (other.monsterKills == null)
            return;

        foreach (MonsterKillEntry entry in other.monsterKills)
        {
            if (entry == null || string.IsNullOrEmpty(entry.monsterId))
                continue;

            monsterKills.Add(new MonsterKillEntry
            {
                monsterId = entry.monsterId,
                killCount = entry.killCount
            });
        }
    }
}

/// <summary>UTC timestamp when a chest's guardians were last all defeated (hourly respawn).</summary>
[System.Serializable]
public class ChestGuardianDefeatTime
{
    public string chestId;
    public double defeatedAtUtc;
}
