using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using VisionOfLight.Enemy;
using VisionOfLight.Player;
using VisionOfLight.Chest;

[DefaultExecutionOrder(200)]
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
    private bool worldDataLoadedOnce;
    private bool worldRestoreScheduled;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Instance.InheritSceneReferences(this);
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "World" || Instance != this)
            return;

        ResolveWorldReferences();

        if (worldDataLoadedOnce)
        {
            Resume();
            ScheduleWorldRestore();
        }
    }

    private void Start()
    {
        if (backBtn != null)
            backBtn.onClick.AddListener(HandleBackButton);

        Resume();

        if (SceneManager.GetActiveScene().name == "World")
            ScheduleWorldRestore();

        worldDataLoadedOnce = true;
    }

    private void ScheduleWorldRestore()
    {
        if (worldRestoreScheduled)
            return;

        worldRestoreScheduled = true;
        StartCoroutine(RestorePlayerAfterWorldReady());
    }

    private IEnumerator RestorePlayerAfterWorldReady()
    {
        yield return null;

        worldRestoreScheduled = false;

        if (SceneManager.GetActiveScene().name != "World" || Instance != this)
            yield break;

        if (!CanRestorePersistedWorldState())
            yield break;

        ResolveWorldReferences();
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

        GameplayCursorPolicy.RequestApply();
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

        GameplayCursorPolicy.RequestApply();
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
            GameplayCursorPolicy.RequestApply();
        }
    }

    public void CloseAllSubScreens()
    {
        if (mapScreen != null) mapScreen.SetActive(false);
        if (questScreen != null) questScreen.SetActive(false);
        if (playerStatsScreen != null) playerStatsScreen.SetActive(false);
        if (FullMapController.Instance != null)
            FullMapController.Instance.SetFullMapCameraEnabled(false);

        if (InventoryUIManager.Instance != null && InventoryUIManager.Instance.inventoryWindow.activeSelf)
            InventoryUIManager.Instance.ToggleInventory();

        if (CharacterMenuController.Instance != null && CharacterMenuController.Instance.attributesScreen.activeSelf)
            CharacterMenuController.Instance.ToggleMenu();

        currentActiveSubScreen = null;
    }

    public void OpenMap()
    {
        OpenSubScreen(mapScreen, false);
        if (FullMapController.Instance != null)
        {
            FullMapController.Instance.SetFullMapCameraEnabled(true);
            FullMapController.Instance.RefreshMapUI();
        }
        else if (fullMapCamera != null)
            fullMapCamera.SetActive(true);
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
            GameplayCursorPolicy.RequestApply();
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
            GameplayCursorPolicy.RequestApply();
        }
    }

    public void OpenQuests()
    {
        OpenSubScreen(questScreen, false);
        QuestUIController.Instance?.RefreshQuestUI();
    }
    public void OpenPlayerStats()
    {
        OpenSubScreen(playerStatsScreen, false);
        PlayerStatsUI.Instance?.RefreshValues();
    }
    public void OpenSettings() => settingsMenuUI?.SetActive(true);
    public void CloseSettings() => settingsMenuUI?.SetActive(false);

    public void SaveGameSilently()
    {
        ResolveWorldReferences();

        int currentSlot = PlayerPrefs.GetInt("SelectedSlot", 1);
        GameData data = SaveManager.LoadGame(currentSlot) ?? new GameData();

        data.worldName = PlayerPrefs.GetString("Slot_" + currentSlot + "_Name", "World " + currentSlot);
        data.currentTime = DayNightCycle.Instance != null ? DayNightCycle.Instance.currentTime : 12f;

        if (playerTransform != null)
        {
            data.playerPos[0] = playerTransform.position.x;
            data.playerPos[1] = playerTransform.position.y;
            data.playerPos[2] = playerTransform.position.z;
            data.hasSavedPlayerPosition = true;

            PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                data.hasSavedHealth = true;
                data.savedCurrentHealth = playerHealth.currentHealth;
            }

            PlayerStamina playerStamina = playerTransform.GetComponent<PlayerStamina>();
            if (playerStamina != null)
            {
                data.hasSavedStamina = true;
                data.savedCurrentStamina = playerStamina.currentStamina;
            }
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

        if (WorldSaveManager.Instance != null)
            data.hasCompletedChapter01Awakening = WorldSaveManager.Instance.HasCompletedChapter01Awakening;

        ChallengeTrialRegistry.WriteToSave(data);
        ChestRegistry.WriteToSave(data);
        ChestGuardianRespawnRegistry.WriteToSave(data);
        TeleportUnlockRegistry.WriteToSave(data);
        PlayerStatsTracker.WriteToSave(data);

        SaveManager.SaveGame(currentSlot, data);
    }

    public void SaveAndExit()
    {
        SaveGameSilently();
        Time.timeScale = 1f;
        if (SceneLoaderManager.Instance != null) SceneLoaderManager.Instance.LoadWorldScene("MainMenu");
        else SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Restores only position and vitals — used when skipping the bed cinematic on reload.
    /// Returns true when a saved world position was applied.
    /// </summary>
    public bool TryRestorePlayerTransformFromSave()
    {
        if (!CanRestorePersistedWorldState())
            return false;

        ResolveWorldReferences();

        int currentSlot = PlayerPrefs.GetInt("SelectedSlot", 1);
        GameData data = SaveManager.LoadGame(currentSlot);
        if (data == null || playerTransform == null)
            return false;

        bool appliedPosition = HasStoredPlayerPosition(data);
        if (appliedPosition)
            ApplySavedPlayerPosition(data);

        ApplyLoadedPlayerVitals(data);
        return appliedPosition;
    }

    private bool CanRestorePersistedWorldState()
    {
        if (WorldSaveManager.Instance != null && WorldSaveManager.Instance.HasCompletedChapter01Awakening)
            return true;

        int currentSlot = PlayerPrefs.GetInt("SelectedSlot", 1);
        GameData data = SaveManager.LoadGame(currentSlot);
        return data != null && data.hasCompletedChapter01Awakening;
    }

    private void LoadPlayerData()
    {
        int currentSlot = PlayerPrefs.GetInt("SelectedSlot", 1);
        GameData data = SaveManager.LoadGame(currentSlot);

        if (QuestManager.Instance != null && data != null)
        {
            QuestManager.Instance.mainQuestState = data.mainQuestState;
            QuestManager.Instance.questStepIndex = data.questStepIndex;
        }

        playerProfile?.ResetToDefault();
        InventoryManager.Instance?.ClearInventory();
        QuickSlotManager.Instance?.ResetSelection();

        if (data != null)
        {
            if (playerTransform != null && HasStoredPlayerPosition(data))
                ApplySavedPlayerPosition(data);

            if (DayNightCycle.Instance != null) DayNightCycle.Instance.currentTime = data.currentTime;

            if (InventoryManager.Instance != null)
            {
                ItemData[] allItems = Resources.LoadAll<ItemData>("");
                foreach (SavedItem savedItem in data.inventoryItems)
                    foreach (ItemData item in allItems)
                        if (item.name == savedItem.itemName
                            || (savedItem.itemName == "Gold Coin" && item.name == "Gold coin 0"))
                        {
                            InventoryManager.Instance.AddItem(item, savedItem.amount, silent: true);
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

            ApplyLoadedPlayerVitals(data);
            ChallengeTrialRegistry.ApplyFromSave(data);
            ChestRegistry.ApplyFromSave(data);
            ChestGuardianRespawnRegistry.ApplyFromSave(data);
            TeleportUnlockRegistry.ApplyFromSave(data);
            PlayerStatsTracker.ApplyFromSave(data);
        }
        else
        {
            ApplyLoadedPlayerVitals(null);
            ChallengeTrialRegistry.ApplyFromSave(null);
            ChestRegistry.ApplyFromSave(null);
            ChestGuardianRespawnRegistry.ApplyFromSave(null);
            TeleportUnlockRegistry.ApplyFromSave(null);
            PlayerStatsTracker.ApplyFromSave(null);
        }
    }

    private void ApplyLoadedPlayerVitals(GameData data)
    {
        if (playerTransform == null) return;

        PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            if (data != null && data.hasSavedHealth)
            {
                int healthToLoad = data.savedCurrentHealth;
                if (healthToLoad <= 0)
                    healthToLoad = playerHealth.maxHealth > 0 ? playerHealth.maxHealth : 100;

                playerHealth.ApplyLoadedHealth(true, healthToLoad);
            }
            else
                playerHealth.ApplyLoadedHealth(false, 0);
        }

        PlayerStamina playerStamina = playerTransform.GetComponent<PlayerStamina>();
        if (playerStamina != null)
        {
            if (data != null && data.hasSavedStamina)
                playerStamina.ApplyLoadedStamina(true, data.savedCurrentStamina);
            else
                playerStamina.ApplyLoadedStamina(false, 0f);
        }
    }

    private static bool HasStoredPlayerPosition(GameData data)
    {
        if (data?.playerPos == null || data.playerPos.Length < 3)
            return false;

        if (data.hasSavedPlayerPosition)
            return true;

        return Mathf.Abs(data.playerPos[0]) > 0.01f
            || Mathf.Abs(data.playerPos[1]) > 0.01f
            || Mathf.Abs(data.playerPos[2]) > 0.01f;
    }

    private void ApplySavedPlayerPosition(GameData data)
    {
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        playerTransform.position = new Vector3(data.playerPos[0], data.playerPos[1], data.playerPos[2]);

        if (cc != null)
            cc.enabled = true;

        PlayerMovement movement = playerTransform.GetComponent<PlayerMovement>();
        if (movement != null)
            movement.ResetFallDamage();
        else
            playerTransform.SendMessage("ResetFallDamage", SendMessageOptions.DontRequireReceiver);
    }

    /// <summary>
    /// Copies serialized World-scene bindings from a scene-local manager into this persistent instance.
    /// </summary>
    private void InheritSceneReferences(PauseMenuManager sceneSource)
    {
        if (sceneSource == null)
            return;

        pauseMainPanel = sceneSource.pauseMainPanel;
        settingsMenuUI = sceneSource.settingsMenuUI;
        mapScreen = sceneSource.mapScreen;
        inventoryScreen = sceneSource.inventoryScreen;
        setupScreen = sceneSource.setupScreen;
        questScreen = sceneSource.questScreen;
        playerStatsScreen = sceneSource.playerStatsScreen;
        hudElementsToHide = sceneSource.hudElementsToHide;
        quickSlotBar = sceneSource.quickSlotBar;
        fullMapCamera = sceneSource.fullMapCamera;
        backBtn = sceneSource.backBtn;
        playerTransform = sceneSource.playerTransform;

        if (sceneSource.playerProfile != null)
            playerProfile = sceneSource.playerProfile;

        RebindBackButton();
    }

    /// <summary>
    /// Rebinds scene objects when re-entering World after DontDestroyOnLoad persistence.
    /// </summary>
    private void ResolveWorldReferences()
    {
        PauseMenuManager sceneBinding = FindScenePauseMenuBinding();
        if (sceneBinding != null && sceneBinding != this)
        {
            InheritSceneReferences(sceneBinding);
            Destroy(sceneBinding.gameObject);
        }

        if (playerTransform == null)
        {
            PlayerRegistry registry = FindFirstObjectByType<PlayerRegistry>(FindObjectsInactive.Include);
            if (registry != null)
                playerTransform = registry.transform;
            else
            {
                PlayerHealth health = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Include);
                if (health != null)
                    playerTransform = health.transform;
            }
        }

        if (playerTransform == null)
            playerTransform = SharedInteractPromptUtility.GetPlayerTransform();

        if (pauseMainPanel == null)
            pauseMainPanel = FindSceneGameObject("PauseMenuScreen");

        if (mapScreen == null)
            mapScreen = FindSceneGameObject("FullMapScreen");

        if (quickSlotBar == null)
            quickSlotBar = FindSceneGameObject("QuickSlotsBar");

        if (settingsMenuUI == null)
            settingsMenuUI = FindSceneGameObject("SettingsScreen");

        if (questScreen == null)
            questScreen = FindSceneGameObject("QuestScreen");

        if (playerStatsScreen == null)
            playerStatsScreen = FindSceneGameObject("StatsScreen");

        if (fullMapCamera == null)
            fullMapCamera = FindSceneGameObject("FullMapCamera");

        RebindBackButton();
    }

    private PauseMenuManager FindScenePauseMenuBinding()
    {
        PauseMenuManager[] managers = FindObjectsByType<PauseMenuManager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (PauseMenuManager manager in managers)
        {
            if (manager == this)
                continue;

            if (manager.gameObject.scene.IsValid())
                return manager;
        }

        return null;
    }

    private static GameObject FindSceneGameObject(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
            return null;

        foreach (GameObject root in activeScene.GetRootGameObjects())
        {
            foreach (Transform descendant in root.GetComponentsInChildren<Transform>(true))
            {
                if (descendant.name == objectName)
                    return descendant.gameObject;
            }
        }

        return null;
    }

    private void RebindBackButton()
    {
        if (backBtn == null)
            return;

        backBtn.onClick.RemoveListener(HandleBackButton);
        backBtn.onClick.AddListener(HandleBackButton);
    }
}