using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hides the ground quest path while the player is in an interior (house) volume
/// or when a door teleporter marks the destination as indoors.
/// </summary>
public static class QuestPathSuppression
{
    private static int zoneOverlapCount;
    private static bool forcedInterior;

    // Static state must not leak across scene loads: saving & exiting while indoors
    // would otherwise keep the quest path hidden for the whole next session.
    // Zones re-fire OnTriggerEnter for the freshly spawned player, so starting
    // every scene clean is always correct.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnBoot()
    {
        zoneOverlapCount = 0;
        forcedInterior = false;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        zoneOverlapCount = 0;
        forcedInterior = false;
    }

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
