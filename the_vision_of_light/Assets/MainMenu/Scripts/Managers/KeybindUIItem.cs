using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Represents a single keybind UI element, handling its display and triggering the rebind process.
/// </summary>
public class KeybindUIItem : MonoBehaviour
{
    #region Configuration
    [Header("Action Setup")]
    /// <summary>The exact name of the action as defined in the KeybindManager dictionary.</summary>
    public string actionName;
    
    [Tooltip("Check this if you don't want the player to rebind this key")]
    public bool isLocked = false; 
    #endregion

    #region UI References
    [Header("UI References")]
    public TextMeshProUGUI actionNameText;
    public TextMeshProUGUI buttonText;
    public Button bindButton;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Initializes the button click listener.
    /// </summary>
    private void Start()
    {
        if (bindButton != null)
        {
            bindButton.onClick.AddListener(OnClickBind);
        }
    }

    /// <summary>
    /// Updates the UI text and color every frame to reflect the current key or rebinding state.
    /// </summary>
    private void Update()
    {
        if (KeybindManager.Instance != null && KeybindManager.Instance.keys.TryGetValue(actionName, out KeyCode currentKey))
        {
            // If this specific action is currently waiting for a new key input
            if (!isLocked && KeybindManager.Instance.GetActionToRebind() == actionName)
            {
                buttonText.text = "> ? <"; 
                buttonText.color = Color.yellow; 
            }
            else
            {
                buttonText.text = FormatKeyName(currentKey);
                // Gray out the text if the keybind is locked, otherwise keep it white
                buttonText.color = isLocked ? new Color(0.7f, 0.7f, 0.7f) : Color.white; 
            }
        }
    }
    #endregion

    #region UI Logic
    /// <summary>
    /// Sends a rebind request to the KeybindManager if the button is not locked.
    /// </summary>
    private void OnClickBind()
    {
        if (!isLocked)
        {
            KeybindManager.Instance.StartRebinding(actionName);
        }
    }

    /// <summary>
    /// Converts KeyCode values into user-friendly strings or TextMeshPro sprite tags.
    /// </summary>
    /// <param name="key">The KeyCode to format.</param>
    /// <returns>A formatted string ready for UI display.</returns>
    private string FormatKeyName(KeyCode key)
    {
        switch (key)
        {
            // Mouse Inputs (Using TMP Sprites)
            case KeyCode.Mouse0: return "<sprite name=\"MouseLeft\">"; 
            case KeyCode.Mouse1: return "<sprite name=\"MouseRight\">"; 
            case KeyCode.Mouse2: return "<sprite name=\"MouseWheel\">"; 
            
            // Modifiers and Special Keys
            case KeyCode.LeftShift: return "L-Shift";
            case KeyCode.RightShift: return "R-Shift";
            case KeyCode.LeftControl: return "L-Ctrl";
            case KeyCode.RightControl: return "R-Ctrl";
            case KeyCode.Escape: return "Esc";
            
            // Specific Overrides for cleaner UI
            case KeyCode.F: return "F";
            case KeyCode.Alpha1: return "1";
            case KeyCode.Alpha2: return "2";
            case KeyCode.Alpha3: return "3";
            case KeyCode.Alpha4: return "4";

            // Default fallback
            default:
                return key.ToString();
        }
    }
    #endregion
}