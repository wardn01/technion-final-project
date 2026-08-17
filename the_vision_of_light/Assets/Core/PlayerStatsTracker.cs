using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime player achievement / combat statistics for the active save slot.
/// Follows the same ApplyFromSave / WriteToSave pattern as TeleportUnlockRegistry.
/// </summary>
public static class PlayerStatsTracker
{
    #region Private State
    private static readonly PlayerStatistics runtime = new PlayerStatistics();
    private static readonly Dictionary<string, int> monsterKillLookup = new Dictionary<string, int>();
    #endregion

    #region Public Accessors
    /// <summary>Live counters for the current session (also mirrored into GameData on save).</summary>
    public static PlayerStatistics Stats => runtime;
    #endregion

    #region Save / Load
    /// <summary>Restores runtime counters from the active save slot.</summary>
    public static void ApplyFromSave(GameData data)
    {
        if (data == null || data.playerStatistics == null)
        {
            runtime.Reset();
            RebuildLookupFromRuntime();
            return;
        }

        runtime.CopyFrom(data.playerStatistics);
        RebuildLookupFromRuntime();
    }

    /// <summary>Writes runtime counters back into the active save slot.</summary>
    public static void WriteToSave(GameData data)
    {
        if (data == null)
            return;

        if (data.playerStatistics == null)
            data.playerStatistics = new PlayerStatistics();

        SyncLookupIntoRuntime();
        data.playerStatistics.CopyFrom(runtime);
    }
    #endregion

    #region Combat Statistics
    /// <summary>Records final damage applied to an enemy (after defense), split by weapon element.</summary>
    public static void AddDamage(float amount, WeaponItemData.WeaponElement element = WeaponItemData.WeaponElement.None)
    {
        if (amount <= 0f)
            return;

        runtime.totalDamageDealt += amount;

        if (amount > runtime.highestSingleDamage)
            runtime.highestSingleDamage = amount;

        switch (element)
        {
            case WeaponItemData.WeaponElement.Wind:
                runtime.windDamageDealt += amount;
                break;
            case WeaponItemData.WeaponElement.Fire:
                runtime.fireDamageDealt += amount;
                break;
            case WeaponItemData.WeaponElement.Ice:
                runtime.iceDamageDealt += amount;
                break;
        }
    }
    #endregion

    #region Kill Tracking
    /// <summary>
    /// Records a kill. Prefer <paramref name="monsterId"/> = EnemyBaseStats asset name (e.g. OrcData).
    /// Always increments <see cref="PlayerStatistics.totalEnemiesKilled"/>.
    /// </summary>
    public static void RecordKill(string monsterId = null)
    {
        runtime.totalEnemiesKilled++;

        if (string.IsNullOrWhiteSpace(monsterId))
            return;

        if (monsterKillLookup.TryGetValue(monsterId, out int count))
            monsterKillLookup[monsterId] = count + 1;
        else
            monsterKillLookup[monsterId] = 1;

        SyncLookupIntoRuntime();
    }

    /// <summary>Returns the kill count for a specific monster id, or zero when unknown.</summary>
    public static int GetKillCount(string monsterId)
    {
        if (string.IsNullOrWhiteSpace(monsterId))
            return 0;

        return monsterKillLookup.TryGetValue(monsterId, out int count) ? count : 0;
    }

    /// <summary>True when the player has defeated at least one enemy of this type.</summary>
    public static bool IsDiscovered(string monsterId)
    {
        return GetKillCount(monsterId) > 0;
    }
    #endregion

    #region Activity Counters
    /// <summary>Increments the chest-opened counter.</summary>
    public static void RecordChestOpened()
    {
        runtime.chestsOpened++;
    }

    /// <summary>Increments the wave-cleared counter.</summary>
    public static void RecordWaveCleared()
    {
        runtime.wavesCleared++;
    }

    /// <summary>Increments the player-death counter.</summary>
    public static void RecordDeath()
    {
        runtime.timesDied++;
    }

    /// <summary>Increments the potion-consumed counter.</summary>
    public static void RecordPotionConsumed()
    {
        runtime.potionsConsumed++;
    }
    #endregion

    #region Private Helpers
    private static void RebuildLookupFromRuntime()
    {
        monsterKillLookup.Clear();

        if (runtime.monsterKills == null)
            return;

        foreach (MonsterKillEntry entry in runtime.monsterKills)
        {
            if (entry == null || string.IsNullOrEmpty(entry.monsterId) || entry.killCount <= 0)
                continue;

            monsterKillLookup[entry.monsterId] = entry.killCount;
        }
    }

    private static void SyncLookupIntoRuntime()
    {
        if (runtime.monsterKills == null)
            runtime.monsterKills = new List<MonsterKillEntry>();
        else
            runtime.monsterKills.Clear();

        foreach (KeyValuePair<string, int> pair in monsterKillLookup)
        {
            if (string.IsNullOrEmpty(pair.Key) || pair.Value <= 0)
                continue;

            runtime.monsterKills.Add(new MonsterKillEntry
            {
                monsterId = pair.Key,
                killCount = pair.Value
            });
        }
    }
    #endregion

    #region Editor
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        runtime.Reset();
        monsterKillLookup.Clear();
    }
#endif
    #endregion
}
