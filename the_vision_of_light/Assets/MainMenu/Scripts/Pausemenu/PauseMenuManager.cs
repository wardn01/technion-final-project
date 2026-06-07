using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

    [Header("Main UI Panels")]
    public GameObject pauseMainPanel;
    public GameObject settingsMenuUI;

    [Header("Sub Screens")]
    public GameObject mapScreen;
    public GameObject inventoryScreen;
    public GameObject setupScreen;
    public GameObject questScreen;
    public GameObject playerStatsScreen;

    [Header("HUD Elements (Hidden when paused)")]
    public GameObject[] hudElementsToHide;

    [Header("Quick Slots Reference")]
    public GameObject quickSlotBar;

    [Header("Cameras")]
    public GameObject fullMapCamera;

    [Header("UI Buttons")]
    public Button backBtn;

    [Header("Player & Data")]
    public Transform playerTransform;
    public PlayerData playerProfile;

    [HideInInspector] public bool isPaused;
    [HideInInspector] public bool openedFromHotkey = false;

    private GameObject currentActiveSubScreen = null;

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

    private void Start()
    {
        if (backBtn != null)
            backBtn.onClick.AddListener(HandleBackButton);

        Resume();
        LoadPlayerData();
    }

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
            else if (isPaused && pauseMainPanel != null)
            {
                pauseMainPanel.SetActive(true);
            }
            else
            {
                Resume();
            }
            
            return;
        }

        if (isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        CloseAllSubScreens();
        openedFromHotkey = false;

        if (pauseMainPanel != null) pauseMainPanel.SetActive(true);
        if (settingsMenuUI != null) settingsMenuUI.SetActive(false);

        ToggleHUDElements(false);
        SetQuickSlots(false);

        Time.timeScale = 0f;
        isPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        CloseAllSubScreens();

        if (settingsMenuUI != null) settingsMenuUI.SetActive(false);
        if (pauseMainPanel != null) pauseMainPanel.SetActive(false);

        ToggleHUDElements(true);
        SetQuickSlots(true);

        Time.timeScale = 1f;
        isPaused = false;
        openedFromHotkey = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ToggleHUDElements(bool show)
    {
        foreach (GameObject element in hudElementsToHide)
            if (element != null) element.SetActive(show);
    }

    private void SetQuickSlots(bool show)
    {
        if (quickSlotBar != null)
            quickSlotBar.SetActive(show);
    }

    private void OpenSubScreen(GameObject screenToOpen, bool keepQuickSlots)
    {
        CloseAllSubScreens();
        if (screenToOpen != null)
        {
            if (pauseMainPanel != null) pauseMainPanel.SetActive(false);
            screenToOpen.SetActive(true);
            currentActiveSubScreen = screenToOpen;

            ToggleHUDElements(false);
            SetQuickSlots(keepQuickSlots);
            
            Time.timeScale = 0f;
            isPaused = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void CloseAllSubScreens()
    {
        if (mapScreen != null) mapScreen.SetActive(false);
        if (questScreen != null) questScreen.SetActive(false);
        if (playerStatsScreen != null) playerStatsScreen.SetActive(false);
        if (fullMapCamera != null) fullMapCamera.SetActive(false);

        if (InventoryUIManager.Instance != null && InventoryUIManager.Instance.inventoryWindow.activeSelf)
            InventoryUIManager.Instance.ToggleInventory();

        if (CharacterMenuController.Instance != null && CharacterMenuController.Instance.attributesScreen.activeSelf)
            CharacterMenuController.Instance.ToggleMenu();

        currentActiveSubScreen = null;
    }

    public void OpenMap()
    {
        OpenSubScreen(mapScreen, false);
        if (fullMapCamera != null) fullMapCamera.SetActive(true);
        if (FullMapController.Instance != null) FullMapController.Instance.RefreshMapUI();
    }

    public void OpenInventory()
    {
        CloseAllSubScreens();
        if (pauseMainPanel != null) pauseMainPanel.SetActive(false);

        if (InventoryUIManager.Instance != null)
        {
            if (!InventoryUIManager.Instance.inventoryWindow.activeSelf)
                InventoryUIManager.Instance.ToggleInventory();

            currentActiveSubScreen = InventoryUIManager.Instance.inventoryWindow;
            ToggleHUDElements(false);
            SetQuickSlots(true);
            
            Time.timeScale = 0f;
            isPaused = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void OpenSetup()
    {
        CloseAllSubScreens();
        if (pauseMainPanel != null) pauseMainPanel.SetActive(false);

        if (CharacterMenuController.Instance != null)
        {
            if (!CharacterMenuController.Instance.attributesScreen.activeSelf)
                CharacterMenuController.Instance.ToggleMenu();

            currentActiveSubScreen = CharacterMenuController.Instance.attributesScreen;
            ToggleHUDElements(false);

            Time.timeScale = 0f;
            isPaused = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void OpenQuests()
    {
        OpenSubScreen(questScreen, false);
        QuestUIController.Instance?.RefreshQuestUI();
    }
    public void OpenPlayerStats() => OpenSubScreen(playerStatsScreen, false);
    public void OpenSettings() => settingsMenuUI?.SetActive(true);
    public void CloseSettings() => settingsMenuUI?.SetActive(false);

    public void SaveGameSilently()
    {
        int currentSlot = PlayerPrefs.GetInt("SelectedSlot", 1);
        GameData data = SaveManager.LoadGame(currentSlot) ?? new GameData();

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
                data.inventoryItems.Add(new SavedItem { itemName = kvp.Key.name, amount = kvp.Value });
        }

        if (playerProfile != null)
        {
            playerProfile.PrepareForSave();
            data.playerDataJson = JsonUtility.ToJson(playerProfile);
        }

        if (QuestManager.Instance != null)
        {
            data.mainQuestState = QuestManager.Instance.mainQuestState;
            data.questStepIndex = QuestManager.Instance.questStepIndex;
        }

        SaveManager.SaveGame(currentSlot, data);
    }

    public void SaveAndExit()
    {
        SaveGameSilently();
        Time.timeScale = 1f;
        if (SceneLoaderManager.Instance != null) SceneLoaderManager.Instance.LoadWorldScene("MainMenu");
        else SceneManager.LoadScene("MainMenu");
    }

    private void LoadPlayerData()
    {
        int currentSlot = PlayerPrefs.GetInt("SelectedSlot", 1);
        GameData data = SaveManager.LoadGame(currentSlot);

        playerProfile?.ResetToDefault();
        InventoryManager.Instance?.ClearInventory();
        QuickSlotManager.Instance?.ResetSelection();

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
                    foreach (ItemData item in allItems)
                        if (item.name == savedItem.itemName)
                        {
                            InventoryManager.Instance.AddItem(item, savedItem.amount);
                            break;
                        }
            }

            if (playerProfile != null && !string.IsNullOrEmpty(data.playerDataJson))
            {
                // Ascension phases are inspector-authored config, not runtime state. Saves are
                // generated from a blank PlayerData instance whose JSON carries an empty phases
                // array, so we cache the configured phases and restore them after the overwrite.
                AscensionPhase[] configuredPhases = playerProfile.ascensionPhases;

                JsonUtility.FromJsonOverwrite(data.playerDataJson, playerProfile);

                playerProfile.ascensionPhases = configuredPhases;

                playerProfile.RestoreAfterLoad();
                playerProfile.LoadBuild(playerProfile.currentActiveLoadout);
            }
        }
    }
}