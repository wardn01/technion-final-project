using UnityEngine;

/// <summary>
/// Lightweight HUD singleton that tracks whether a dialogue UI is open.
/// Other systems (input lock, teleports, NPCs) read <see cref="isDialogueOpen"/>.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [HideInInspector]
    [Tooltip("Set by DialogueManager when conversation UI is shown.")]
    public bool isDialogueOpen = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
}
