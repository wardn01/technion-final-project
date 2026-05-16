using UnityEngine;
using System.IO;

public static class SaveManager
{
    public static void SaveGame(int slotIndex, GameData data)
    {
        string json = JsonUtility.ToJson(data);
        string path = Application.persistentDataPath + "/save_" + slotIndex + ".json";
        File.WriteAllText(path, json);
        PlayerPrefs.SetInt("Slot_" + slotIndex + "_Exists", 1);
        Debug.Log("Game Saved to Slot " + slotIndex);
    }

    public static GameData LoadGame(int slotIndex)
    {
        string path = Application.persistentDataPath + "/save_" + slotIndex + ".json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<GameData>(json);
        }
        return null;
    }

    public static void DeleteGame(int slotIndex)
    {
        string path = Application.persistentDataPath + "/save_" + slotIndex + ".json";
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        PlayerPrefs.DeleteKey("Slot_" + slotIndex + "_Exists");
        PlayerPrefs.DeleteKey("Slot_" + slotIndex + "_Name");
        Debug.Log("Deleted save file for Slot " + slotIndex);
    }
}