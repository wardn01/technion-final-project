using UnityEngine;
using VisionOfLight.Chest;
using VisionOfLight.Enemy;

/// <summary>
/// Clears shared InteractPrompt proximity when the player warps (map teleport / door)
/// without triggering OnTriggerExit.
/// </summary>
public static class SharedInteractPromptUtility
{
    public const float DefaultLeaveDistance = 6f;

    public static void ClearAllProximityPrompts()
    {
        foreach (WorldChest chest in Object.FindObjectsByType<WorldChest>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            chest.ClearPlayerProximity();

        foreach (DoorTeleporter door in Object.FindObjectsByType<DoorTeleporter>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            door.ClearPlayerProximity();

        foreach (TeleportPoint point in Object.FindObjectsByType<TeleportPoint>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            point.ClearPlayerProximity();

        foreach (ChallengeStone stone in Object.FindObjectsByType<ChallengeStone>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            stone.ClearPlayerProximity();
    }

    public static bool IsPlayerBeyondRange(Vector3 anchor, Transform player, float maxDistance)
    {
        if (player == null)
            return true;

        return Vector3.Distance(anchor, player.position) > maxDistance;
    }
}
