using UnityEngine;

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
            // Restore Quest Progress
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.mainQuestState = data.mainQuestState;
            }

            // Restore Player Data
            if (activePlayerData != null && !string.IsNullOrEmpty(data.playerDataJson))
            {
                JsonUtility.FromJsonOverwrite(data.playerDataJson, activePlayerData);
                activePlayerData.RestoreAfterLoad(); 
            }
        }
        else
        {
            // Initialize Default Values for a New Game
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.mainQuestState = 0;
            }

            if (activePlayerData != null)
            {
                activePlayerData.ResetToDefault();
            }
        }
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