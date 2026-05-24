using UnityEngine;
using System.IO;

public static class SaveManager
{
    public static void SaveGame(int slotIndex, GameData data)
    {
        if (data == null)
        {
            Debug.LogWarning($"SaveGame failed: GameData is null for slot {slotIndex}.");
            return;
        }

        string path = Application.persistentDataPath + "/save_" + slotIndex + ".json";
        try
        {
            string json = JsonUtility.ToJson(data);
            File.WriteAllText(path, json);
            PlayerPrefs.SetInt("Slot_" + slotIndex + "_Exists", 1);
            PlayerPrefs.Save();
            Debug.Log("Game Saved to Slot " + slotIndex);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save game to slot {slotIndex}: {ex.Message}");
        }
    }

    public static GameData LoadGame(int slotIndex)
    {
        string path = Application.persistentDataPath + "/save_" + slotIndex + ".json";
        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonUtility.FromJson<GameData>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to load game from slot {slotIndex}: {ex.Message}");
            return null;
        }
    }

    public static void DeleteGame(int slotIndex)
    {
        string path = Application.persistentDataPath + "/save_" + slotIndex + ".json";
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to delete save file for slot {slotIndex}: {ex.Message}");
        }

        PlayerPrefs.DeleteKey("Slot_" + slotIndex + "_Exists");
        PlayerPrefs.DeleteKey("Slot_" + slotIndex + "_Name");
        PlayerPrefs.Save();
        Debug.Log("Deleted save file for Slot " + slotIndex);
    }
}