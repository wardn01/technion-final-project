using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using VisionOfLight.Player;

/// <summary>
/// Manages the Play Menu UI, including creating, loading, deleting, and displaying world save slots.
/// </summary>
public class PlayMenuManager : MonoBehaviour
{
    #region Constants & State Variables
    private const int MAX_SLOTS = 4; 
    private int selectedSlot = -1;
    private int slotToDelete = -1;
    private Coroutine warningCoroutine;
    #endregion

    #region UI References
    [Header("Left Side Panels")]
    public GameObject panelCreate;
    public GameObject panelDetails;

    [Header("Create World Section")]
    public TMP_InputField nameInputField;
    public Button btnCreate;
    public GameObject nameExistsWarning;

    [Header("World Details Section")]
    public TMP_Text detailWorldNameText;
    public TMP_Text detailLevelText; 
    public GameObject[] ascensionStars;
    public TMP_Text worldLevelText;
    public Button btnPlayWorld;
    public Button btnDeleteWorld;

    [Header("Right Side (Load) Section")]
    public Transform loadGameContainer;
    public GameObject slotPrefab;
    public Button btnAddWorld; 
    public TMP_Text maxLimitText;

    [Header("Confirm Delete Popup")]
    public GameObject confirmDeletePanel;
    public TMP_Text confirmWorldNameText;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Initializes input validation for the world name field.
    /// </summary>
    private void Start()
    {
        if (nameInputField != null)
        {
            nameInputField.characterLimit = 20;
            nameInputField.onValidateInput += delegate (string text, int charIndex, char addedChar)
            {
                return ValidateEnglishOnly(addedChar);
            };
        }
    }

    /// <summary>
    /// Resets UI states and refreshes the slots whenever the menu is enabled.
    /// </summary>
    private void OnEnable()
    {
        if (confirmDeletePanel != null) confirmDeletePanel.SetActive(false);
        if (nameExistsWarning != null) nameExistsWarning.SetActive(false);
        RefreshSlots();
    }
    #endregion

    #region Input Validation
    /// <summary>
    /// Ensures only English letters, numbers, and spaces are typed into the input field.
    /// </summary>
    private char ValidateEnglishOnly(char charToValidate)
    {
        if ((charToValidate >= 'a' && charToValidate <= 'z') ||
            (charToValidate >= 'A' && charToValidate <= 'Z') ||
            (charToValidate >= '0' && charToValidate <= '9') ||
            charToValidate == ' ')
        {
            return charToValidate;
        }
        return '\0'; // Return null character if invalid
    }
    #endregion

    #region Slot Management & Display
    /// <summary>
    /// Clears the current slot UI and instantiates new ones based on saved data.
    /// </summary>
    public void RefreshSlots()
    {
        // Clear existing slots in the UI container
        foreach (Transform child in loadGameContainer)
        {
            Destroy(child.gameObject);
        }

        int activeWorldsCount = 0;
        int firstFoundSlot = -1;

        for (int i = 1; i <= MAX_SLOTS; i++)
        {
            if (PlayerPrefs.GetInt($"Slot_{i}_Exists", 0) == 1)
            {
                activeWorldsCount++;
                if (firstFoundSlot == -1) firstFoundSlot = i;

                // Instantiate and setup the slot prefab
                GameObject newSlot = Instantiate(slotPrefab, loadGameContainer);
                string worldName = PlayerPrefs.GetString($"Slot_{i}_Name", $"MyWorld {i}");
                
                TMP_Text slotText = newSlot.transform.Find("Title")?.GetComponent<TMP_Text>();
                if(slotText != null) slotText.text = worldName;

                int slotIndex = i; // Local copy for the closure
                
                // Setup Slot Click
                Button slotBtn = newSlot.GetComponent<Button>();
                if (slotBtn != null)
                {
                    slotBtn.onClick.AddListener(() => ShowWorldDetails(slotIndex, worldName));
                }

                // Setup Play Icon Click
                Transform playIcon = newSlot.transform.Find("Icon");
                if (playIcon != null)
                {
                    Button iconBtn = playIcon.GetComponent<Button>();
                    if (iconBtn != null)
                    {
                        iconBtn.onClick.AddListener(() => LoadWorld(slotIndex));
                    }
                }
            }
        }

        // Update limits and UI interactability
        if (maxLimitText != null) maxLimitText.text = $"{activeWorldsCount}/{MAX_SLOTS}";

        bool canCreate = activeWorldsCount < MAX_SLOTS;
        if (btnAddWorld != null) btnAddWorld.interactable = canCreate;
        if (btnCreate != null) btnCreate.interactable = canCreate;
        if (nameInputField != null) nameInputField.interactable = canCreate;

        // Auto-select logic
        if (activeWorldsCount == 0)
        {
            ShowCreatePanel();
        }
        else if (firstFoundSlot != -1)
        {
            ShowWorldDetails(firstFoundSlot, PlayerPrefs.GetString($"Slot_{firstFoundSlot}_Name"));
        }
    }

    /// <summary>
    /// Displays the panel for creating a new world.
    /// </summary>
    public void ShowCreatePanel()
    {
        if (panelDetails != null) panelDetails.SetActive(false);
        if (panelCreate != null) panelCreate.SetActive(true);
        if (nameInputField != null) nameInputField.text = "";
        if (nameExistsWarning != null) nameExistsWarning.SetActive(false);
    }

    /// <summary>
    /// Loads and displays the specific details (level, ascension) of the selected world.
    /// </summary>
    public void ShowWorldDetails(int slotIndex, string worldName)
    {
        selectedSlot = slotIndex;
        
        if (panelCreate != null) panelCreate.SetActive(false);
        if (panelDetails != null) panelDetails.SetActive(true);
        if (detailWorldNameText != null) detailWorldNameText.text = worldName;

        GameData data = SaveManager.LoadGame(slotIndex);
        if (data != null && !string.IsNullOrEmpty(data.playerDataJson))
        {
            PlayerData tempData = ScriptableObject.CreateInstance<PlayerData>();
            JsonUtility.FromJsonOverwrite(data.playerDataJson, tempData);

            if(detailLevelText != null) detailLevelText.text = $"Lv.{tempData.currentLevel}"; 
            if (worldLevelText != null) worldLevelText.text = tempData.currentAscensionIndex.ToString();

            // Setup Ascension Stars
            if (ascensionStars != null)
            {
                for (int i = 0; i < ascensionStars.Length; i++)
                {
                    if (ascensionStars[i] == null) continue;
                    
                    ascensionStars[i].SetActive(true);
                    Image starImage = ascensionStars[i].GetComponent<Image>();
                    
                    if (starImage != null)
                    {
                        starImage.color = i < tempData.currentAscensionIndex ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.8f);
                    }
                }
            }
            
            // Destroy the temporary instance to prevent memory leaks
            Destroy(tempData);
        }
        else
        {
            // Default Values if no data exists
            if(detailLevelText != null) detailLevelText.text = "Lv.1"; 
            if(worldLevelText != null) worldLevelText.text = "0";
            
            if (ascensionStars != null)
            {
                foreach (var star in ascensionStars) 
                {
                    if (star == null) continue;
                    star.SetActive(true);
                    Image starImage = star.GetComponent<Image>();
                    if (starImage != null) starImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
                }
            }
        }

        // Setup Buttons
        if (btnPlayWorld != null)
        {
            btnPlayWorld.onClick.RemoveAllListeners();
            btnPlayWorld.onClick.AddListener(() => LoadWorld(selectedSlot));
        }

        if (btnDeleteWorld != null)
        {
            btnDeleteWorld.onClick.RemoveAllListeners();
            btnDeleteWorld.onClick.AddListener(() => ShowConfirmDelete(slotIndex, worldName));
        }
    }
    #endregion

    #region World Creation & Loading
    /// <summary>
    /// Validates the input name, checks for duplicates, and creates a new game save in the first available slot.
    /// </summary>
    public void CreateNewGame()
    {
        string worldName = nameInputField.text.Trim();
        
        if (string.IsNullOrEmpty(worldName))
        {
            ShowWarning("Enter World Name!");
            return;
        }

        // Check for duplicate names
        for (int i = 1; i <= MAX_SLOTS; i++)
        {
            if (PlayerPrefs.GetInt($"Slot_{i}_Exists", 0) == 1)
            {
                string existingName = PlayerPrefs.GetString($"Slot_{i}_Name", "");
                if (existingName.Equals(worldName, System.StringComparison.OrdinalIgnoreCase))
                {
                    ShowWarning("Name Exists!");
                    return;
                }
            }
        }

        // Find the first empty slot
        int emptySlot = -1;
        for (int i = 1; i <= MAX_SLOTS; i++)
        {
            if (PlayerPrefs.GetInt($"Slot_{i}_Exists", 0) == 0)
            {
                emptySlot = i;
                break;
            }
        }

        // Create the world
        if (emptySlot != -1)
        {
            GameData newWorldData = new GameData();
            newWorldData.worldName = worldName;
            
            PlayerData defaultData = ScriptableObject.CreateInstance<PlayerData>();
            defaultData.ResetToDefault();
            newWorldData.playerDataJson = JsonUtility.ToJson(defaultData);
            
            // Destroy the temporary instance to prevent memory leaks
            Destroy(defaultData);

            SaveManager.SaveGame(emptySlot, newWorldData);
            PlayerPrefs.SetString($"Slot_{emptySlot}_Name", worldName);
            PlayerPrefs.Save();
            
            RefreshSlots();
        }
    }

    /// <summary>
    /// Sets the selected slot and loads the game scene via the Loading Screen.
    /// </summary>
    public void LoadWorld(int slotIndex)
    {
        PlayerPrefs.SetInt("SelectedSlot", slotIndex);
        PlayerPrefs.Save();
        
        if (SceneLoaderManager.Instance != null)
        {
            SceneLoaderManager.Instance.LoadWorldScene("World");
        }
        else
        {
            SceneManager.LoadScene("World");
        }
    }
    #endregion

    #region Deletion Logic
    /// <summary>
    /// Shows the deletion confirmation popup.
    /// </summary>
    public void ShowConfirmDelete(int slotIndex, string worldName)
    {
        slotToDelete = slotIndex;
        if (confirmWorldNameText != null) 
            confirmWorldNameText.text = $"Are you sure you want to delete this world? This action cannot be undone.\n\n<color=red>{worldName}</color>?";
        
        if (confirmDeletePanel != null) confirmDeletePanel.SetActive(true);
    }

    /// <summary>
    /// Confirms the deletion, wipes the save data, and refreshes the UI.
    /// </summary>
    public void ConfirmDelete()
    {
        if (slotToDelete != -1)
        {
            SaveManager.DeleteGame(slotToDelete);
            slotToDelete = -1;
            if (confirmDeletePanel != null) confirmDeletePanel.SetActive(false);
            RefreshSlots();
        }
    }

    /// <summary>
    /// Cancels the deletion process and closes the popup.
    /// </summary>
    public void CancelDelete()
    {
        slotToDelete = -1;
        if (confirmDeletePanel != null) confirmDeletePanel.SetActive(false);
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Displays a temporary warning message on the UI.
    /// </summary>
    private void ShowWarning(string message)
    {
        if (nameExistsWarning != null)
        {
            TMP_Text warningText = nameExistsWarning.GetComponent<TMP_Text>();
            if (warningText != null)
            {
                warningText.text = message;
            }
            
            // Stop existing coroutine if it's already running to prevent overlapping
            if (warningCoroutine != null) StopCoroutine(warningCoroutine);
            
            nameExistsWarning.SetActive(true);
            warningCoroutine = StartCoroutine(HideWarningRoutine());
        }
    }

    private IEnumerator HideWarningRoutine()
    {
        yield return new WaitForSeconds(3f);
        if (nameExistsWarning != null)
        {
            nameExistsWarning.SetActive(false);
        }
    }
    #endregion
}