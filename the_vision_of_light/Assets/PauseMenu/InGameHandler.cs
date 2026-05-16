using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class InGameHandler : MonoBehaviour
{
    public static InGameHandler Instance;

    public GameObject pauseMenuUI;
    public Transform playerTransform; 
    public PlayerData playerProfile; 
    [HideInInspector] public bool isPaused = false; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        Resume();
        LoadPlayerData();
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; 
        isPaused = true;
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void SaveAndExit()
    {
        int currentSlot = PlayerPrefs.GetInt("SelectedSlot", 1);
        GameData dataCube = new GameData(); 

        dataCube.worldName = PlayerPrefs.GetString("Slot_" + currentSlot + "_Name", "World " + currentSlot);
        dataCube.currentTime = DayNightCycle.Instance != null ? DayNightCycle.Instance.currentTime : 12f;
        
        if (playerTransform != null)
        {
            dataCube.playerPos[0] = playerTransform.position.x;
            dataCube.playerPos[1] = playerTransform.position.y;
            dataCube.playerPos[2] = playerTransform.position.z;
        }

        if (InventoryManager.Instance != null)
        {
            foreach (var kvp in InventoryManager.Instance.GetInventory())
            {
                SavedItem sItem = new SavedItem();
                sItem.itemName = kvp.Key.name; 
                sItem.amount = kvp.Value;
                dataCube.inventoryItems.Add(sItem);
            }
        }

        if (playerProfile != null)
        {
            playerProfile.PrepareForSave(); 
            dataCube.playerDataJson = JsonUtility.ToJson(playerProfile);
        }

        SaveManager.SaveGame(currentSlot, dataCube);

        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    void LoadPlayerData()
    {
        int currentSlot = PlayerPrefs.GetInt("SelectedSlot", 1);
        GameData dataCube = SaveManager.LoadGame(currentSlot); 

        if (playerProfile != null) playerProfile.ResetToDefault();
        if (InventoryManager.Instance != null) InventoryManager.Instance.ClearInventory();
        if (QuickSlotManager.Instance != null)
        {
            for (int i = 0; i < 4; i++) QuickSlotManager.Instance.slots[i] = null;
            QuickSlotManager.Instance.UpdateUI();
        }

        if (dataCube != null)
        {
            if (playerTransform != null)
            {
                playerTransform.position = new Vector3(dataCube.playerPos[0], dataCube.playerPos[1], dataCube.playerPos[2]);
            }
            if (DayNightCycle.Instance != null)
            {
                DayNightCycle.Instance.currentTime = dataCube.currentTime;
            }

            if (InventoryManager.Instance != null)
            {
                ItemData[] allItems = Resources.LoadAll<ItemData>(""); 
                
                foreach (SavedItem savedItem in dataCube.inventoryItems)
                {
                    foreach (ItemData itemAsset in allItems)
                    {
                        if (itemAsset.name == savedItem.itemName)
                        {
                            InventoryManager.Instance.AddItem(itemAsset, savedItem.amount);
                            break;
                        }
                    }
                }
            }

            if (playerProfile != null && !string.IsNullOrEmpty(dataCube.playerDataJson))
            {
                JsonUtility.FromJsonOverwrite(dataCube.playerDataJson, playerProfile);
                playerProfile.RestoreAfterLoad(); 
                playerProfile.LoadBuild(playerProfile.currentActiveLoadout);
            }
        }
        else
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.ApplyStartingItems();
            }
        }
    }
}