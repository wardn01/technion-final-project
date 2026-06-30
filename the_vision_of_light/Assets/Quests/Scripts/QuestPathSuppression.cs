using UnityEngine;

/// <summary>
/// Hides the ground quest path while the player is in an interior (house) volume
/// or when a door teleporter marks the destination as indoors.
/// </summary>
public static class QuestPathSuppression
{
    private static int zoneOverlapCount;
    private static bool forcedInterior;

    public static bool IsSuppressed => forcedInterior || zoneOverlapCount > 0;

    public static void SetForcedInterior(bool inside)
    {
        forcedInterior = inside;
    }

    public static void EnterZone()
    {
        zoneOverlapCount++;
    }

    public static void ExitZone()
    {
        zoneOverlapCount = Mathf.Max(0, zoneOverlapCount - 1);
    }
}
