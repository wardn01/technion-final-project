using TMPro;
using UnityEngine;
using VisionOfLight.Chest;
using VisionOfLight.Enemy;
using VisionOfLight.Player;

/// <summary>
/// Shared InteractPrompt arbiter + player lookup helpers.
/// Only one owner may drive Interact_F at a time. Never disable the InteractPrompt root —
/// that breaks doors/teleports/chests that share the same UI.
/// </summary>
public static class SharedInteractPromptUtility
{
    public const float DefaultLeaveDistance = 6f;

    private static object s_owner;

    #region Player lookup
    public static Transform GetPlayerTransform()
    {
        if (PlayerRegistry.Instance != null)
            return PlayerRegistry.Instance.transform;

        GameObject tagged = GameObject.FindGameObjectWithTag("Player");
        return tagged != null ? tagged.transform : null;
    }

    public static GameObject GetPlayerGameObject()
    {
        Transform t = GetPlayerTransform();
        return t != null ? t.gameObject : null;
    }
    #endregion

    #region Prompt arbiter
    public static void Show(
        object owner,
        GameObject promptRoot,
        GameObject promptContainer,
        GameObject interactKeyPrompt,
        TextMeshProUGUI promptTextUI,
        string text,
        bool showInteractKey = true)
    {
        if (owner == null)
            return;

        s_owner = owner;

        if (promptRoot != null && !promptRoot.activeSelf)
            promptRoot.SetActive(true);

        if (promptContainer != null && !promptContainer.activeSelf)
            promptContainer.SetActive(true);

        if (interactKeyPrompt != null && interactKeyPrompt.activeSelf != showInteractKey)
            interactKeyPrompt.SetActive(showInteractKey);

        if (promptTextUI == null)
            return;

        if (!promptTextUI.gameObject.activeSelf)
            promptTextUI.gameObject.SetActive(true);

        promptTextUI.text = text ?? string.Empty;
    }

    /// <summary>
    /// Hides Interact_F only. Never disables InteractPrompt root.
    /// Ignored if another system currently owns the prompt.
    /// </summary>
    public static void Hide(
        object owner,
        GameObject promptContainer,
        GameObject interactKeyPrompt = null)
    {
        if (owner == null)
            return;

        if (s_owner != null && !ReferenceEquals(s_owner, owner))
            return;

        if (ReferenceEquals(s_owner, owner))
            s_owner = null;

        if (promptContainer != null && promptContainer.activeSelf)
            promptContainer.SetActive(false);

        if (interactKeyPrompt != null && interactKeyPrompt.activeSelf)
            interactKeyPrompt.SetActive(false);
    }

    public static void ForceRelease()
    {
        s_owner = null;
    }
    #endregion

    #region Proximity clear
    /// <summary>
    /// Clears shared InteractPrompt proximity when the player warps (map teleport / door)
    /// without triggering OnTriggerExit.
    /// </summary>
    public static void ClearAllProximityPrompts()
    {
        ForceRelease();

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
    #endregion
}
