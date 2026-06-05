using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// The core manager for the in-game pause menu. Handles time scaling, cursor locking, 
/// sub-screen navigation (map, inventory, quests, etc.), and the save/load data pipeline.
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    #region Singleton
    public static PauseMenuManager Instance { get; private set; }
    #endregion

    #region UI & Camera References
    [Header("Main UI Panels")]
    public GameObject pauseMainPanel;
    public GameObject settingsMenuUI;

    [Header("Sub Screens (Right Side)")]
    public GameObject mapScreen;
    public GameObject inventoryScreen;
    public GameObject setupScreen;
    public GameObject questScreen;
    public GameObject playerStatsScreen;

    [Header("Cameras")]
    /// <summary>The top-down camera used specifically for the full map view.</summary>
    public GameObject fullMapCamera; 

    [Header("UI Buttons")]
    public Button backBtn;
    #endregion

    #region Player & Data References
    [Header("Player & Data")]
    public Transform playerTransform;
    public PlayerData playerProfile;
    #endregion

    #region State Variables
    [HideInInspector] public bool isPaused;
    [HideInInspector] public bool openedFromHotkey = false;
    
    private GameObject currentActiveSubScreen = null;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Initializes the singleton instance and ensures it persists across scenes if necessary.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Binds the back button listener, resumes the game to ensure a clean state, and loads the player's saved data.
    /// </summary>
    private void Start()
    {
        if (backBtn != null)
            backBtn.onClick.AddListener(HandleBackButton);

        Resume();
        LoadPlayerData();
    }
    #endregion

    #region Pause Mechanics
    /// <summary>
    /// Intelligently handles the back/escape action. Closes settings or sub-screens first before unpausing the game.
    /// </summary>
    public void HandleBackButton()
    {
        if (settingsMenuUI != null && settingsMenuUI.activeSelf)
        {
            CloseSettings();
            return;
        }

        if (currentActiveSubScreen != null)
        {
            CloseAllSubScreens();
            
            if (openedFromHotkey)
            {
                Resume(); 
            }
            else
            {
                if (pauseMainPanel != null) pauseMainPanel.SetActive(true);
            }
            return;
        }

        if (isPaused) Resume();
        else Pause();
    }

    /// <summary>
    /// Freezes the game time, unlocks the cursor, and displays the main pause panel.
    /// </summary>
    public void Pause()
    {
        CloseAllSubScreens();
        openedFromHotkey = false;

        if (pauseMainPanel != null) pauseMainPanel.SetActive(true);
        if (settingsMenuUI != null) settingsMenuUI.SetActive(false);

        Time.timeScale = 0f;
        isPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Restores normal game time, locks the cursor, and hides all pause UI elements.
    /// </summary>
    public void Resume()
    {
        CloseAllSubScreens();
        if (settingsMenuUI != null) settingsMenuUI.SetActive(false);
        if (pauseMainPanel != null) pauseMainPanel.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
        openedFromHotkey = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    #endregion

    #region Sub-Screen Navigation
    /// <summary>
    /// Opens a specific sub-screen while ensuring the main pause panel is hidden to prevent UI overlap.
    /// </summary>
    /// <param name="screenToOpen">The GameObject of the UI panel to display.</param>
    private void OpenSubScreen(GameObject screenToOpen)
    {
        CloseAllSubScreens();
        if (screenToOpen != null)
        {
            if (pauseMainPanel != null) pauseMainPanel.SetActive(false); 
            screenToOpen.SetActive(true);
            currentActiveSubScreen = screenToOpen;
        }
    }

    /// <summary>
    /// Closes all active sub-screens and deactivates associated elements like the map camera.
    /// </summary>
    public void CloseAllSubScreens()
    {
        if (mapScreen != null) mapScreen.SetActive(false);
        if (inventoryScreen != null) inventoryScreen.SetActive(false);
        if (setupScreen != null) setupScreen.SetActive(false);
        if (questScreen != null) questScreen.SetActive(false);
        if (playerStatsScreen != null) playerStatsScreen.SetActive(false);
        
        if (fullMapCamera != null) fullMapCamera.SetActive(false); 

        currentActiveSubScreen = null;
    }

    public void OpenMap() 
    {
        OpenSubScreen(mapScreen);
        if (fullMapCamera != null) fullMapCamera.SetActive(true);
    }

    public void OpenInventory() => OpenSubScreen(inventoryScreen);
    public void OpenSetup() => OpenSubScreen(setupScreen);
    public void OpenQuests() => OpenSubScreen(questScreen);
    public void OpenPlayerStats() => OpenSubScreen(playerStatsScreen);

    public void OpenSettings()
    {
        if (settingsMenuUI != null) settingsMenuUI.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsMenuUI != null) settingsMenuUI.SetActive(false);
    }
    #endregion

    #region Data Management (Save/Load)
    /// <summary>
    /// Gathers all current world state, player position, inventory, and quest data, saving it to the active slot.
    /// </summary>
    public void SaveGameSilently()
    {
        int currentSlot = PlayerPrefs.GetInt("SelectedSlot", 1);
        GameData data = SaveManager.LoadGame(currentSlot);
        if (data == null) data = new GameData();

        data.worldName = PlayerPrefs.GetString("Slot_" + currentSlot + "_Name", "World " + currentSlot);
        data.currentTime = DayNightCycle.Instance != null ? DayNightCycle.Instance.currentTime : 12f;

        if (playerTransform != null)
        {
            data.playerPos[0] = playerTransform.position.x;
            data.playerPos[1] = playerTransform.position.y;
            data.playerPos[2] = playerTransform.position.z;
        }

        if (InventoryManager.Instance != null)
        {
            data.inventoryItems.Clear();
            foreach (var kvp in InventoryManager.Instance.GetInventory())
            {
                data.inventoryItems.Add(new SavedItem { itemName = kvp.Key.name, amount = kvp.Value });
            }
        }

        if (playerProfile != null)
        {
            playerProfile.PrepareForSave();
            data.playerDataJson = JsonUtility.ToJson(playerProfile);
        }

        if (QuestManager.Instance != null)
            data.mainQuestState = QuestManager.Instance.mainQuestState;

        SaveManager.SaveGame(currentSlot, data);
    }

    /// <summary>
    /// Executes a silent save and safely transitions the player back to the Main Menu via the loading screen.
    /// </summary>
    public void SaveAndExit()
    {
        SaveGameSilently();
        Time.timeScale = 1f;
        
        if (SceneLoaderManager.Instance != null) SceneLoaderManager.Instance.LoadWorldScene("MainMenu");
        else SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Loads saved data from the current slot, safely restoring player transform, time of day, inventory, and profile stats.
    /// </summary>
    private void LoadPlayerData()
    {
        int currentSlot = PlayerPrefs.GetInt("SelectedSlot", 1);
        GameData data = SaveManager.LoadGame(currentSlot);

        if (playerProfile != null) playerProfile.ResetToDefault();
        if (InventoryManager.Instance != null) InventoryManager.Instance.ClearInventory();

        if (QuickSlotManager.Instance != null)
        {
            for (int i = 0; i < 4; i++) QuickSlotManager.Instance.slots[i] = null;
            QuickSlotManager.Instance.UpdateUI();
        }

        if (data != null)
        {
            if (playerTransform != null)
            {
                CharacterController cc = playerTransform.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                playerTransform.position = new Vector3(data.playerPos[0], data.playerPos[1], data.playerPos[2]);
                if (cc != null) cc.enabled = true;
            }

            if (DayNightCycle.Instance != null) DayNightCycle.Instance.currentTime = data.currentTime;

            if (InventoryManager.Instance != null)
            {
                ItemData[] allItems = Resources.LoadAll<ItemData>("");
                foreach (SavedItem savedItem in data.inventoryItems)
                {
                    foreach (ItemData item in allItems)
                    {
                        if (item.name == savedItem.itemName)
                        {
                            InventoryManager.Instance.AddItem(item, savedItem.amount);
                            break;
                        }
                    }
                }
            }

            if (playerProfile != null && !string.IsNullOrEmpty(data.playerDataJson))
            {
                JsonUtility.FromJsonOverwrite(data.playerDataJson, playerProfile);
                playerProfile.RestoreAfterLoad();
                playerProfile.LoadBuild(playerProfile.currentActiveLoadout);
            }
        }
        else
        {
            if (InventoryManager.Instance != null) InventoryManager.Instance.ApplyStartingItems();
        }
    }
    #endregion
}