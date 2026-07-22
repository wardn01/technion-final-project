using UnityEngine;

/// <summary>
/// Runtime player achievement / combat statistics for the active save slot.
/// Follows the same ApplyFromSave / WriteToSave pattern as TeleportUnlockRegistry.
/// </summary>
public static class PlayerStatsTracker
{
    private static readonly PlayerStatistics runtime = new PlayerStatistics();

    /// <summary>Live counters for the current session (also mirrored into GameData on save).</summary>
    public static PlayerStatistics Stats => runtime;

    public static void ApplyFromSave(GameData data)
    {
        if (data == null || data.playerStatistics == null)
        {
            runtime.Reset();
            return;
        }

        runtime.CopyFrom(data.playerStatistics);
    }

    public static void WriteToSave(GameData data)
    {
        if (data == null)
            return;

        if (data.playerStatistics == null)
            data.playerStatistics = new PlayerStatistics();

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

    public static void RecordKill()
    {
        runtime.totalEnemiesKilled++;
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

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        runtime.Reset();
    }
#endif
}
