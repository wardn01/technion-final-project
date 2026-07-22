using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime player achievement / combat statistics for the active save slot.
/// Follows the same ApplyFromSave / WriteToSave pattern as TeleportUnlockRegistry.
/// </summary>
public static class PlayerStatsTracker
{
    private static readonly PlayerStatistics runtime = new PlayerStatistics();
    private static readonly Dictionary<string, int> monsterKillLookup = new Dictionary<string, int>();

    /// <summary>Live counters for the current session (also mirrored into GameData on save).</summary>
    public static PlayerStatistics Stats => runtime;

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

    public static void WriteToSave(GameData data)
    {
        if (data == null)
            return;

        if (data.playerStatistics == null)
            data.playerStatistics = new PlayerStatistics();

        SyncLookupIntoRuntime();
        data.playerStatistics.CopyFrom(runtime);
    }

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

    public static int GetKillCount(string monsterId)
    {
        if (string.IsNullOrWhiteSpace(monsterId))
            return 0;

        return monsterKillLookup.TryGetValue(monsterId, out int count) ? count : 0;
    }

    public static bool IsDiscovered(string monsterId)
    {
        return GetKillCount(monsterId) > 0;
    }

    public static void RecordChestOpened()
    {
        runtime.chestsOpened++;
    }

    public static void RecordWaveCleared()
    {
        runtime.wavesCleared++;
    }

    public static void RecordDeath()
    {
        runtime.timesDied++;
    }

    public static void RecordPotionConsumed()
    {
        runtime.potionsConsumed++;
    }

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

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        runtime.Reset();
        monsterKillLookup.Clear();
    }
#endif
}
