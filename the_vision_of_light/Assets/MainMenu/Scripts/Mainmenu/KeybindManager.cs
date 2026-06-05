using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Manages the game's keybindings, including rebinding, conflict resolution (swapping), and saving/loading preferences.
/// </summary>
public class KeybindManager : MonoBehaviour
{
    #region Singleton
    public static KeybindManager Instance { get; private set; }
    #endregion

    #region Variables & UI References
    [Header("Keybindings Dictionary")]
    /// <summary>Stores the active keybindings mapped to their action names.</summary>
    public Dictionary<string, KeyCode> keys = new Dictionary<string, KeyCode>();

    [Header("UI Popup References")]
    public GameObject conflictPopup; 
    public TextMeshProUGUI conflictText; 
    public Button confirmBtn;
    public Button cancelBtn;

    [Header("Escape Warning References")]
    public GameObject escapeWarningPopup;

    // State Variables
    private string actionToRebind = "";
    private KeyCode newKeyToBind = KeyCode.None;
    private string conflictingAction = "";
    private bool isWaitingForKey = false;
    private float warningTimer = 0f;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Initializes the singleton, loads saved keys, and sets up UI listeners.
    /// </summary>
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

        if (confirmBtn != null) 
        { 
            confirmBtn.onClick.RemoveAllListeners(); 
            confirmBtn.onClick.AddListener(ConfirmSwap); 
        }
        
        if (cancelBtn != null) 
        { 
            cancelBtn.onClick.RemoveAllListeners(); 
            cancelBtn.onClick.AddListener(CancelRebind); 
        }
    }

    /// <summary>
    /// Handles the timer and dismissal of the escape warning popup.
    /// </summary>
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

    /// <summary>
    /// Captures keyboard and mouse events for rebinding when the manager is in waiting mode.
    /// </summary>
    private void OnGUI()
    {
        if (isWaitingForKey)
        {
            Event e = Event.current;

            // Handle Escape key cancellation
            if (e.isKey && e.keyCode == KeyCode.Escape)
            {
                isWaitingForKey = false;
                ShowEscapeWarning();
                return;
            }

            // Handle Standard Keys
            if (e.isKey && e.keyCode != KeyCode.None)
            {
                isWaitingForKey = false;
                newKeyToBind = e.keyCode;
                CheckForKeyConflict();
            }
            // Handle Mouse Buttons
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
            // Handle Shift Key modifier
            else if (e.shift)
            {
                isWaitingForKey = false;
                newKeyToBind = KeyCode.LeftShift;
                CheckForKeyConflict();
            }
        }
    }

    /// <summary>
    /// Resets state variables and hides popups when the script/object is disabled.
    /// </summary>
    private void OnDisable()
    {
        isWaitingForKey = false;
        actionToRebind = "";
        
        if (conflictPopup != null) conflictPopup.SetActive(false);
        if (escapeWarningPopup != null) escapeWarningPopup.SetActive(false);
    }
    #endregion

    #region Rebinding Logic
    /// <summary>
    /// Prepares the manager to listen for a new key input for the specified action.
    /// Blocks rebinding for reserved actions.
    /// </summary>
    /// <param name="actionName">The name of the action to rebind.</param>
    public void StartRebinding(string actionName)
    {
        if (actionName == "OpenMenu" || actionName == "Interact" || actionName.StartsWith("Slot"))
        {
            ShowEscapeWarning();
            return;
        }
        
        actionToRebind = actionName;
        isWaitingForKey = true;
    }

    /// <summary>
    /// Checks if the new key is already in use and triggers the conflict resolution flow if necessary.
    /// </summary>
    private void CheckForKeyConflict()
    {
        if (keys.ContainsValue(newKeyToBind))
        {
            conflictingAction = keys.FirstOrDefault(x => x.Value == newKeyToBind).Key;
            
            // If mapping to the exact same key, do nothing
            if (conflictingAction == actionToRebind) return; 

            // Check if the conflicting action is reserved and cannot be swapped
            if (conflictingAction == "Interact" || conflictingAction.StartsWith("Slot") || conflictingAction == "OpenMenu")
            {
                conflictText.text = $"The key [{newKeyToBind}] is reserved for [{conflictingAction}] and cannot be swapped.";
                
                if (confirmBtn != null) confirmBtn.gameObject.SetActive(false); 
                
                conflictPopup.SetActive(true);
                return;
            }

            // Allow swapping for non-reserved keys
            if (confirmBtn != null) confirmBtn.gameObject.SetActive(true);
            conflictText.text = $"The key [{newKeyToBind}] is already used for [{conflictingAction}].\nDo you want to swap them?";
            conflictPopup.SetActive(true);
        }
        else
        {
            ExecuteRebind(actionToRebind, newKeyToBind);
        }
    }

    /// <summary>
    /// Swaps the conflicting keys and finalizes the rebind.
    /// </summary>
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

    /// <summary>
    /// Cancels the rebinding process and hides popups.
    /// </summary>
    public void CancelRebind()
    {
        if (conflictPopup != null) conflictPopup.SetActive(false);
        if (escapeWarningPopup != null) escapeWarningPopup.SetActive(false);
        actionToRebind = "";
        isWaitingForKey = false;
    }

    /// <summary>
    /// Updates the dictionary with the new key and saves it to PlayerPrefs.
    /// </summary>
    private void ExecuteRebind(string action, KeyCode key)
    {
        keys[action] = key;
        PlayerPrefs.SetInt("Key_" + action, (int)key);
        PlayerPrefs.Save();
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
    #endregion

    #region Load & Default Settings
    /// <summary>
    /// Populates the dictionary with default hardcoded keys.
    /// </summary>
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
        keys.Add("OpenQuests", KeyCode.J);
        
        keys.Add("OpenMenu", KeyCode.Escape); 
        keys.Add("Interact", KeyCode.F);
        keys.Add("Slot1", KeyCode.Alpha1);
        keys.Add("Slot2", KeyCode.Alpha2);
        keys.Add("Slot3", KeyCode.Alpha3);
        keys.Add("Slot4", KeyCode.Alpha4);
    }

    /// <summary>
    /// Loads user preferences from PlayerPrefs, overwriting default keys if a save exists.
    /// </summary>
    private void LoadKeys()
    {
        DefaultKeys();
        
        // Copy keys to a list to avoid 'collection modified' exceptions during iteration
        List<string> keysList = new List<string>(keys.Keys);
        foreach (string key in keysList)
        {
            if (PlayerPrefs.HasKey("Key_" + key))
            {
                keys[key] = (KeyCode)PlayerPrefs.GetInt("Key_" + key);
            }
        }
    }

    /// <summary>
    /// Resets all keys to their default values and overrides the saved preferences.
    /// </summary>
    public void ResetToDefault()
    {
        DefaultKeys(); 
        foreach (var kvp in keys)
        {
            PlayerPrefs.SetInt("Key_" + kvp.Key, (int)kvp.Value);
        }
        PlayerPrefs.Save();
    }
    #endregion

    #region Getters
    /// <summary>
    /// Returns the name of the action currently waiting for a keybind.
    /// </summary>
    public string GetActionToRebind()
    {
        return isWaitingForKey ? actionToRebind : "";
    }
    #endregion
}