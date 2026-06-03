using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class KeybindUIItem : MonoBehaviour
{
    [Header("Action Setup")]
    public string actionName;
    
    [Tooltip("Check this if you don't want the player to rebind this key")]
    public bool isLocked = false; 

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
        if (KeybindManager.Instance != null && KeybindManager.Instance.keys.TryGetValue(actionName, out KeyCode currentKey))
        {
            if (!isLocked && KeybindManager.Instance.GetActionToRebind() == actionName)
            {
                buttonText.text = "> ? <"; 
                buttonText.color = Color.yellow; 
            }
            else
            {
                buttonText.text = FormatKeyName(currentKey);
                buttonText.color = isLocked ? new Color(0.7f, 0.7f, 0.7f) : Color.white; 
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
            case KeyCode.Mouse0: return "<sprite name=\"MouseLeft\">"; 
            case KeyCode.Mouse1: return "<sprite name=\"MouseRight\">"; 
            case KeyCode.Mouse2: return "<sprite name=\"MouseWheel\">"; 
            
            case KeyCode.LeftShift: return "L-Shift";
            case KeyCode.RightShift: return "R-Shift";
            case KeyCode.LeftControl: return "L-Ctrl";
            case KeyCode.RightControl: return "R-Ctrl";
            case KeyCode.Escape: return "Esc";
            
            case KeyCode.F: return "F";
            case KeyCode.Alpha1: return "1";
            case KeyCode.Alpha2: return "2";
            case KeyCode.Alpha3: return "3";
            case KeyCode.Alpha4: return "4";

            default:
                return key.ToString();
        }
    }
}