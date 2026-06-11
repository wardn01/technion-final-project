using UnityEngine;
using TMPro;

/// <summary>
/// Keeps a TMP label in sync with a <see cref="KeybindManager"/> action name
/// (e.g. skill hotkeys on the HUD). Supports TMP sprite tags for mouse buttons.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class AutoUpdateKeyText : MonoBehaviour
{
    [Tooltip("Key in KeybindManager.keys, e.g. \"SkillE\" or \"SkillQ\".")]
    public string actionName;

    private TextMeshProUGUI myText;

    private void Start()
    {
        myText = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (myText == null)
            return;

        KeyCode currentKey = KeyCode.None;

        if (KeybindManager.Instance != null && KeybindManager.Instance.keys.TryGetValue(actionName, out KeyCode foundKey))
        {
            currentKey = foundKey;
        }
        else if (PlayerPrefs.HasKey("Key_" + actionName))
        {
            currentKey = (KeyCode)PlayerPrefs.GetInt("Key_" + actionName);
        }

        if (currentKey != KeyCode.None)
            myText.text = FormatKey(currentKey);
    }

    /// <summary>Short display string; mouse keys use TMP sprite tags.</summary>
    private string FormatKey(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Mouse0: return "<sprite name=\"MouseLeft\">";
            case KeyCode.Mouse1: return "<sprite name=\"MouseRight\">";
            case KeyCode.Mouse2: return "<sprite name=\"MouseWheel\">";
            case KeyCode.LeftShift: return "L-Sh";
            case KeyCode.RightShift: return "R-Sh";
            case KeyCode.LeftControl: return "L-Ct";
            default: return key.ToString();
        }
    }
}
