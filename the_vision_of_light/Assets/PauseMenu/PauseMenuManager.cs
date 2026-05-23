using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance;

    [Header("Menu Screens")]
    public GameObject pauseMenuUI;
    public GameObject settingsMenuUI;
    public GameObject mapScreen;
    public GameObject inventoryScreen;
    public GameObject setupScreen;

    [Header("UI Buttons")]
    public Button backBtn;

    [Header("Player & Data")]
    public Transform playerTransform;
    public PlayerData playerProfile;

    [HideInInspector] public bool isPaused;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (backBtn != null)
            backBtn.onClick.AddListener(Resume);

        Resume();
        LoadPlayerData();
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);

        if (settingsMenuUI != null)
            settingsMenuUI.SetActive(false);

        Time.timeScale = 0f;
        isPaused = true;
    }

    public void Resume()
    {
        if (settingsMenuUI != null)
            settingsMenuUI.SetActive(false);

        pauseMenuUI.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
    }

    public void OpenSettings()
    {
        if (settingsMenuUI != null)
            settingsMenuUI.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsMenuUI != null)
            settingsMenuUI.SetActive(false);
    }

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
                data.inventoryItems.Add(new SavedItem
                {
                    itemName = kvp.Key.name,
                    amount = kvp.Value
                });
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

    public void SaveAndExit()
    {
        SaveGameSilently();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void LoadPlayerData()
    {
        int currentSlot = PlayerPrefs.GetInt("SelectedSlot", 1);
        GameData data = SaveManager.LoadGame(currentSlot);

        if (playerProfile != null)
            playerProfile.ResetToDefault();

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.ClearInventory();

        if (QuickSlotManager.Instance != null)
        {
            for (int i = 0; i < 4; i++)
                QuickSlotManager.Instance.slots[i] = null;

            QuickSlotManager.Instance.UpdateUI();
        }

        if (data != null)
        {
            if (playerTransform != null)
            {
                CharacterController cc = playerTransform.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                playerTransform.position = new Vector3(
                    data.playerPos[0],
                    data.playerPos[1],
                    data.playerPos[2]
                );

                if (cc != null) cc.enabled = true;
            }

            if (DayNightCycle.Instance != null)
                DayNightCycle.Instance.currentTime = data.currentTime;

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
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.ApplyStartingItems();
        }
    }
}