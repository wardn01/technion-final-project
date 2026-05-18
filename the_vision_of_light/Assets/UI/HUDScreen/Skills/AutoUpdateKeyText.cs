using UnityEngine;
using TMPro;

public class AutoUpdateKeyText : MonoBehaviour
{
    public string actionName; 
    
    private TextMeshProUGUI myText;

    void Start()
    {
        myText = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        KeyCode currentKey = KeyCode.None;

        if (KeybindManager.Instance != null && KeybindManager.Instance.keys.ContainsKey(actionName))
        {
            currentKey = KeybindManager.Instance.keys[actionName];
        }
        else if (PlayerPrefs.HasKey("Key_" + actionName))
        {
            currentKey = (KeyCode)PlayerPrefs.GetInt("Key_" + actionName);
        }

        if (currentKey != KeyCode.None)
        {
            myText.text = FormatKey(currentKey);
        }
    }

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