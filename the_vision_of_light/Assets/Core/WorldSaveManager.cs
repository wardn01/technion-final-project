using UnityEngine;
using VisionOfLight.Enemy;
using VisionOfLight.Player;
using VisionOfLight.Chest;

/// <summary>
/// A persistent Singleton manager that handles loading and saving the current world's state, 
/// including player data and quest progress across scene loads.
/// </summary>
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
    #endregion

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
    #endregion

    #region Save & Load Logic
    /// <summary>
    /// Loads the game data for the current slot. Overwrites active PlayerData and Quest states, 
    /// or resets them to default if no save data exists.
    /// </summary>
    public void LoadWorldData()
    {
        GameData data = SaveManager.LoadGame(currentSlot);

        if (data != null)
        {
            ChallengeTrialRegistry.ApplyFromSave(data);
            ChestRegistry.ApplyFromSave(data);
            ApplyQuestProgress(data.mainQuestState, data.questStepIndex);

            // Restore Player Data
            if (activePlayerData != null && !string.IsNullOrEmpty(data.playerDataJson))
            {
                // Ascension phases are inspector-authored config, not runtime state. The saved JSON
                // is produced from a blank PlayerData whose phases array is empty, so cache the
                // configured phases and restore them after the overwrite to avoid wiping them.
                AscensionPhase[] configuredPhases = activePlayerData.ascensionPhases;

                JsonUtility.FromJsonOverwrite(data.playerDataJson, activePlayerData);

                activePlayerData.ascensionPhases = configuredPhases;

                activePlayerData.RestoreAfterLoad(); 
            }
        }
        else
        {
            ChallengeTrialRegistry.ApplyFromSave(null);
            ChestRegistry.ApplyFromSave(null);
            ApplyQuestProgress(0, 0);

            if (activePlayerData != null)
            {
                activePlayerData.ResetToDefault();
            }
        }
    }

    /// <summary>
    /// Applies saved quest progress immediately, or stores it until <see cref="QuestManager"/> is ready.
    /// </summary>
    private void ApplyQuestProgress(int state, int step)
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.mainQuestState = state;
            QuestManager.Instance.questStepIndex = step;
            hasPendingQuestProgress = false;
            return;
        }

        pendingQuestState = state;
        pendingQuestStep = step;
        hasPendingQuestProgress = true;
    }

    /// <summary>
    /// Called by <see cref="QuestManager"/> on startup when quest data was loaded before it existed.
    /// </summary>
    public void ApplyPendingQuestProgress()
    {
        if (!hasPendingQuestProgress || QuestManager.Instance == null) return;

        QuestManager.Instance.mainQuestState = pendingQuestState;
        QuestManager.Instance.questStepIndex = pendingQuestStep;
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