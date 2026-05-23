using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class KeybindManager : MonoBehaviour
{
    public static KeybindManager Instance { get; private set; }

    [Header("Keybindings Dictionary")]
    public Dictionary<string, KeyCode> keys = new Dictionary<string, KeyCode>();

    [Header("UI Popup References")]
    public GameObject conflictPopup; 
    public TextMeshProUGUI conflictText; 
    public Button confirmBtn;
    public Button cancelBtn;

    [Header("Escape Warning References")]
    public GameObject escapeWarningPopup;

    private string actionToRebind = "";
    private KeyCode newKeyToBind = KeyCode.None;
    private string conflictingAction = "";
    private bool isWaitingForKey = false;
    private float warningTimer = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadKeys();
        if (conflictPopup != null) conflictPopup.SetActive(false);
        if (escapeWarningPopup != null) escapeWarningPopup.SetActive(false);

        if (confirmBtn != null) { confirmBtn.onClick.RemoveAllListeners(); confirmBtn.onClick.AddListener(ConfirmSwap); }
        if (cancelBtn != null) { cancelBtn.onClick.RemoveAllListeners(); cancelBtn.onClick.AddListener(CancelRebind); }
    }

    private void Update()
    {
        if (escapeWarningPopup != null && escapeWarningPopup.activeSelf)
        {
            warningTimer -= Time.deltaTime;

            if (warningTimer <= 0f || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.anyKeyDown)
            {
                escapeWarningPopup.SetActive(false);
            }
        }
    }

    private void DefaultKeys()
    {
        keys.Clear();
        keys.Add("MoveForward", KeyCode.W);
        keys.Add("MoveBackward", KeyCode.S);
        keys.Add("MoveLeft", KeyCode.A);
        keys.Add("MoveRight", KeyCode.D);
        keys.Add("NormalAttack", KeyCode.Mouse0); 
        keys.Add("Skill", KeyCode.E);
        keys.Add("Burst", KeyCode.Q);
        keys.Add("Sprint", KeyCode.LeftShift);
        keys.Add("Jump", KeyCode.Space);
        keys.Add("OpenInventory", KeyCode.I);
        keys.Add("OpenCharacterScreen", KeyCode.C);
        keys.Add("OpenMap", KeyCode.M);
        keys.Add("OpenMenu", KeyCode.Escape); 
        keys.Add("Slot1", KeyCode.Alpha1);
        keys.Add("Slot2", KeyCode.Alpha2);
        keys.Add("Slot3", KeyCode.Alpha3);
        keys.Add("Slot4", KeyCode.Alpha4);
    }

    public void StartRebinding(string actionName)
    {
        if (actionName == "OpenMenu")
        {
            ShowEscapeWarning();
            return;
        }
        actionToRebind = actionName;
        isWaitingForKey = true;
    }

    private void OnGUI()
    {
        if (isWaitingForKey)
        {
            Event e = Event.current;

            if (e.isKey && e.keyCode == KeyCode.Escape)
            {
                isWaitingForKey = false;
                ShowEscapeWarning();
                return;
            }

            if (e.isKey && e.keyCode != KeyCode.None)
            {
                isWaitingForKey = false;
                newKeyToBind = e.keyCode;
                CheckForKeyConflict();
            }
            else if (e.isMouse && e.type == EventType.MouseDown)
            {
                isWaitingForKey = false;

                if (e.button == 0) newKeyToBind = KeyCode.Mouse0;      
                else if (e.button == 1) newKeyToBind = KeyCode.Mouse1; 
                else if (e.button == 2) newKeyToBind = KeyCode.Mouse2; 
                else if (e.button == 3) newKeyToBind = KeyCode.Mouse3; 
                else if (e.button == 4) newKeyToBind = KeyCode.Mouse4;

                CheckForKeyConflict();
            }
            else if (e.shift)
            {
                isWaitingForKey = false;
                newKeyToBind = KeyCode.LeftShift;
                CheckForKeyConflict();
            }
        }
    }

    private void ShowEscapeWarning()
    {
        if (escapeWarningPopup != null)
        {
            escapeWarningPopup.SetActive(true);
            warningTimer = 3f;
        }
        actionToRebind = "";
    }

    private void CheckForKeyConflict()
    {
        if (keys.ContainsValue(newKeyToBind))
        {
            conflictingAction = keys.FirstOrDefault(x => x.Value == newKeyToBind).Key;
            if (conflictingAction == actionToRebind) return; 

            conflictText.text = $"The key [{newKeyToBind}] is already used for [{conflictingAction}].\nDo you want to swap them?";
            conflictPopup.SetActive(true);
        }
        else
        {
            ExecuteRebind(actionToRebind, newKeyToBind);
        }
    }

    private void ConfirmSwap()
    {
        if (keys.TryGetValue(actionToRebind, out KeyCode oldKey))
        {
            ExecuteRebind(actionToRebind, newKeyToBind);
            ExecuteRebind(conflictingAction, oldKey);
        }
        else
        {
            ExecuteRebind(actionToRebind, newKeyToBind);
        }

        if (conflictPopup != null)
            conflictPopup.SetActive(false);
    }

    public void CancelRebind()
    {
        if (conflictPopup != null) conflictPopup.SetActive(false);
        if (escapeWarningPopup != null) escapeWarningPopup.SetActive(false);
        actionToRebind = "";
        isWaitingForKey = false;
    }

    private void ExecuteRebind(string action, KeyCode key)
    {
        keys[action] = key;
        PlayerPrefs.SetInt("Key_" + action, (int)key);
        PlayerPrefs.Save();
    }

    private void LoadKeys()
    {
        DefaultKeys();
        List<string> keysList = new List<string>(keys.Keys);
        foreach (string key in keysList)
        {
            if (PlayerPrefs.HasKey("Key_" + key))
            {
                keys[key] = (KeyCode)PlayerPrefs.GetInt("Key_" + key);
            }
        }
    }

    public string GetActionToRebind()
    {
        return isWaitingForKey ? actionToRebind : "";
    }

    public void ResetToDefault()
    {
        DefaultKeys(); 
        foreach (var kvp in keys)
        {
            PlayerPrefs.SetInt("Key_" + kvp.Key, (int)kvp.Value);
        }
        PlayerPrefs.Save();
    }

    private void OnDisable()
    {
        isWaitingForKey = false;
        actionToRebind = "";
        
        if (conflictPopup != null) conflictPopup.SetActive(false);
        if (escapeWarningPopup != null) escapeWarningPopup.SetActive(false);
    }
}