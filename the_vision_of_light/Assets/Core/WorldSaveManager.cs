using UnityEngine;
using UnityEngine.SceneManagement;
using VisionOfLight.Enemy;
using VisionOfLight.Player;
using VisionOfLight.Chest;

/// <summary>
/// A persistent Singleton manager that handles loading and saving the current world's state, 
/// including player data and quest progress across scene loads.
/// </summary>
[DefaultExecutionOrder(-500)]
public class WorldSaveManager : MonoBehaviour
{
    #region Singleton
    public static WorldSaveManager Instance { get; private set; }
    #endregion

    #region Data References
    [HideInInspector]
    public int currentSlot;

    [Header("Player Data Reference")]
    /// <summary>The active ScriptableObject holding the player's runtime data.</summary>
    public PlayerData activePlayerData;
    #endregion

    #region Pending Quest Load
    private bool hasPendingQuestProgress;
    private int pendingQuestState;
    private int pendingQuestStep;
    private bool pendingChapter01AwakeningComplete;
    #endregion

    public bool HasCompletedChapter01Awakening { get; private set; }

    #region Unity Lifecycle
    /// <summary>
    /// Initializes the Singleton, persists it across scenes, and loads the selected slot's data.
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

        // Retrieve the slot selected from the PlayMenuManager
        currentSlot = PlayerPrefs.GetInt("SelectedSlot", 1);
        if (currentSlot <= 0)
        {
            currentSlot = 1; // Fallback to slot 1 if invalid
        }

        LoadWorldData();
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
        if (scene.name == "World")
            ReloadSelectedSlot();
    }

    /// <summary>Reloads save data for the slot selected in the play menu.</summary>
    public void ReloadSelectedSlot()
    {
        currentSlot = PlayerPrefs.GetInt("SelectedSlot", 1);
        if (currentSlot <= 0)
            currentSlot = 1;

        LoadWorldData();
    }
    #endregion

    #region Save & Load Logic
    /// <summary>
    /// Loads the game data for the current slot. Overwrites active PlayerData and Quest states, 
    /// or resets them to default if no save data exists.
    /// </summary>
    public void LoadWorldData()
    {
        // Fallback: the scene reference can be left unassigned — load the shared
        // profile asset from Resources so save data always overwrites stale values.
        if (activePlayerData == null)
            activePlayerData = Resources.Load<PlayerData>("Player Data");

        GameData data = SaveManager.LoadGame(currentSlot);

        if (data != null)
        {
            ChallengeTrialRegistry.ApplyFromSave(data);
            ChestRegistry.ApplyFromSave(data);
            ChestGuardianRespawnRegistry.ApplyFromSave(data);
            TeleportUnlockRegistry.ApplyFromSave(data);
            PlayerStatsTracker.ApplyFromSave(data);
            ApplyQuestProgress(data.mainQuestState, data.questStepIndex, data.hasCompletedChapter01Awakening);

            // Restore Player Data
            if (activePlayerData != null)
            {
                // Use the BeforeSceneLoad snapshot — never clone the live asset after JSON may have wiped refs.
                AscensionPhase[] configuredPhases = PlayerData.GetConfiguredAscensionPhases();

                // Always start from defaults: FromJsonOverwrite only writes fields present in
                // the JSON, and this is a shared asset — without the reset, fields missing from
                // older or partial saves would keep values from a previously played slot.
                activePlayerData.ResetToDefault();

                if (!string.IsNullOrEmpty(data.playerDataJson))
                {
                    JsonUtility.FromJsonOverwrite(data.playerDataJson, activePlayerData);
                    activePlayerData.ascensionPhases = configuredPhases;
                    activePlayerData.EnsureAscensionPhasesConfigured();
                    activePlayerData.RestoreAfterLoad();
                }
                else
                {
                    activePlayerData.ascensionPhases = configuredPhases;
                    activePlayerData.EnsureAscensionPhasesConfigured();
                }
            }
        }
        else
        {
            ChallengeTrialRegistry.ApplyFromSave(null);
            ChestRegistry.ApplyFromSave(null);
            ChestGuardianRespawnRegistry.ApplyFromSave(null);
            TeleportUnlockRegistry.ApplyFromSave(null);
            PlayerStatsTracker.ApplyFromSave(null);
            ApplyQuestProgress(0, 0, false);

            if (activePlayerData != null)
            {
                activePlayerData.ResetToDefault();
                activePlayerData.ascensionPhases = PlayerData.GetConfiguredAscensionPhases();
            }
        }
    }

    /// <summary>
    /// Applies saved quest progress immediately, or stores it until <see cref="QuestManager"/> is ready.
    /// </summary>
    private void ApplyQuestProgress(int state, int step, bool chapter01AwakeningComplete)
    {
        HasCompletedChapter01Awakening = chapter01AwakeningComplete;

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.mainQuestState = state;
            QuestManager.Instance.questStepIndex = step;
            hasPendingQuestProgress = false;
            return;
        }

        pendingQuestState = state;
        pendingQuestStep = step;
        pendingChapter01AwakeningComplete = chapter01AwakeningComplete;
        hasPendingQuestProgress = true;
    }

    public void MarkChapter01AwakeningComplete()
    {
        HasCompletedChapter01Awakening = true;

        if (PauseMenuManager.Instance != null)
        {
            SaveCurrentWorld();
            return;
        }

        GameData data = SaveManager.LoadGame(currentSlot) ?? new GameData();
        data.hasCompletedChapter01Awakening = true;

        if (QuestManager.Instance != null)
        {
            data.mainQuestState = QuestManager.Instance.mainQuestState;
            data.questStepIndex = QuestManager.Instance.questStepIndex;
        }

        // Also persist live registry state so this fallback never writes a
        // near-empty file over real progress (e.g. when the base file failed to parse).
        ChallengeTrialRegistry.WriteToSave(data);
        ChestRegistry.WriteToSave(data);
        ChestGuardianRespawnRegistry.WriteToSave(data);
        TeleportUnlockRegistry.WriteToSave(data);
        PlayerStatsTracker.WriteToSave(data);

        SaveManager.SaveGame(currentSlot, data);
    }

    /// <summary>
    /// Called by <see cref="QuestManager"/> on startup when quest data was loaded before it existed.
    /// </summary>
    public void ApplyPendingQuestProgress()
    {
        if (!hasPendingQuestProgress || QuestManager.Instance == null) return;

        QuestManager.Instance.mainQuestState = pendingQuestState;
        QuestManager.Instance.questStepIndex = pendingQuestStep;
        HasCompletedChapter01Awakening = pendingChapter01AwakeningComplete;
        hasPendingQuestProgress = false;
    }

    /// <summary>
    /// Triggers a silent save event, typically called before switching scenes or quitting.
    /// </summary>
    public void SaveCurrentWorld()
    {
        if (PauseMenuManager.Instance != null)
        {
            PauseMenuManager.Instance.SaveGameSilently();
        }
        else
        {
            Debug.LogWarning("PauseMenuManager instance not found. Cannot save game silently.");
        }
    }
    #endregion
}