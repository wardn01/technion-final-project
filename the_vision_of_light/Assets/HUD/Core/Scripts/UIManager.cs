using UnityEngine;

/// <summary>
/// Lightweight HUD singleton that tracks whether a dialogue UI is open.
/// Other systems (input lock, teleports, NPCs) read <see cref="isDialogueOpen"/>.
/// </summary>
public class UIManager : MonoBehaviour
{
    #region Singleton

    public static UIManager Instance { get; private set; }

    #endregion

    #region State

    [HideInInspector]
    [Tooltip("Set by DialogueManager when conversation UI is shown.")]
    public bool isDialogueOpen = false;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    #endregion
}
