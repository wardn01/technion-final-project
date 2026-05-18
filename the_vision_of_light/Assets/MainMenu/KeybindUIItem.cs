using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class KeybindUIItem : MonoBehaviour
{
    [Header("Action Setup")]
    public string actionName;

    [Header("UI References")]
    public TextMeshProUGUI actionNameText;
    public TextMeshProUGUI buttonText;
    public Button bindButton;

    void Start()
    {
        bindButton.onClick.AddListener(OnClickBind);
    }

    void Update()
    {
        if (KeybindManager.Instance != null && KeybindManager.Instance.keys.ContainsKey(actionName))
        {
            if (KeybindManager.Instance.GetActionToRebind() == actionName)
            {
                buttonText.text = "> ? <"; 
                buttonText.color = Color.yellow; 
            }
            else
            {
                KeyCode currentKey = KeybindManager.Instance.keys[actionName];
                buttonText.text = FormatKeyName(currentKey);
                buttonText.color = Color.white; 
            }
        }
    }

    private void OnClickBind()
    {
        KeybindManager.Instance.StartRebinding(actionName);
    }

    private string FormatKeyName(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Mouse0:
                return "<sprite name=\"MouseLeft\">"; 
            case KeyCode.Mouse1:
                return "<sprite name=\"MouseRight\">"; 
            case KeyCode.Mouse2:
                return "<sprite name=\"MouseWheel\">"; 
            
            case KeyCode.LeftShift: return "L-Shift";
            case KeyCode.RightShift: return "R-Shift";
            case KeyCode.LeftControl: return "L-Ctrl";
            case KeyCode.RightControl: return "R-Ctrl";
            case KeyCode.Escape: return "Esc";

            default:
                return key.ToString();
        }
    }
}