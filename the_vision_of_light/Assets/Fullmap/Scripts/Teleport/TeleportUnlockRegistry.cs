using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks unlocked world teleport points per save slot (replaces global PlayerPrefs).
/// </summary>
public static class TeleportUnlockRegistry
{
    #region Private State
    private static readonly HashSet<int> unlockedTeleportIds = new HashSet<int>();

    /// <summary>
    /// When true, <see cref="TeleportPoint"/> may import legacy PlayerPrefs unlocks
    /// into this registry (existing saves that pre-date slot-based teleport data).
    /// </summary>
    public static bool AllowLegacyPlayerPrefsFallback { get; private set; }
    #endregion

    #region Save / Load
    /// <summary>Restores unlocked teleport ids from the active save slot.</summary>
    public static void ApplyFromSave(GameData data)
    {
        unlockedTeleportIds.Clear();
        AllowLegacyPlayerPrefsFallback = false;

        if (data == null)
            return;

        if (data.unlockedTeleportIds != null)
        {
            foreach (int id in data.unlockedTeleportIds)
                unlockedTeleportIds.Add(id);
        }

        // Pre-migration saves have an empty list — allow one-time PlayerPrefs import on Start.
        // Migrated saves and freshly created worlds must never inherit global legacy unlocks.
        if (unlockedTeleportIds.Count == 0 && !data.teleportDataMigrated)
            AllowLegacyPlayerPrefsFallback = true;
    }

    /// <summary>Writes unlocked teleport ids back into the active save slot.</summary>
    public static void WriteToSave(GameData data)
    {
        if (data == null)
            return;

        data.unlockedTeleportIds = new List<int>(unlockedTeleportIds);
        data.teleportDataMigrated = true;
    }
    #endregion

    #region Unlock API
    /// <summary>Returns true when the given teleport id is unlocked for the active save.</summary>
    public static bool IsUnlocked(int teleportId)
    {
        return unlockedTeleportIds.Contains(teleportId);
    }

    /// <summary>Marks a teleport id as unlocked for the active save.</summary>
    public static void MarkUnlocked(int teleportId)
    {
        unlockedTeleportIds.Add(teleportId);
    }
    #endregion

    #region Nearest Spawn
    /// <summary>Finds the nearest unlocked teleport spawn to <paramref name="fromWorldPosition"/>.</summary>
    public static bool TryGetNearestUnlockedSpawn(Vector3 fromWorldPosition, out Vector3 spawnPosition, out Quaternion spawnRotation)
    {
        spawnPosition = fromWorldPosition;
        spawnRotation = Quaternion.identity;

        TeleportPoint[] points = Object.FindObjectsByType<TeleportPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        TeleportPoint best = null;
        float bestDistance = float.MaxValue;

        foreach (TeleportPoint point in points)
        {
            if (point == null)
                continue;

            bool unlocked = point.isUnlocked || IsUnlocked(point.teleportID);
            if (!unlocked)
                continue;

            Vector3 candidate = point.spawnLocation != null
                ? point.spawnLocation.position
                : point.transform.position;

            float distance = Vector3.Distance(fromWorldPosition, candidate);
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            best = point;
        }

        if (best == null)
            return false;

        if (best.spawnLocation != null)
        {
            spawnPosition = best.spawnLocation.position;
            spawnRotation = best.spawnLocation.rotation;
        }
        else
        {
            spawnPosition = best.transform.position + new Vector3(2f, 1f, 0f);
            spawnRotation = best.transform.rotation;
        }

        return true;
    }
    #endregion
}
