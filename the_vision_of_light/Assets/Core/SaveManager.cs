using UnityEngine;
using System.IO;

/// <summary>
/// Static gateway for JSON save files (one file per slot in
/// <see cref="Application.persistentDataPath"/>).
///
/// Writes are atomic: data is first written to <c>save_N.json.tmp</c>, validated,
/// and then swapped over the real file while the previous version is kept as
/// <c>save_N.json.bak</c>. If the main file is missing or corrupted, loading
/// falls back to the backup, so a crash mid-write can never lose the whole slot.
/// </summary>
public static class SaveManager
{
    /// <summary>Defensive cap — a valid save is a few KB; anything huge is corrupt/tampered.</summary>
    private const long MaxSaveFileBytes = 16 * 1024 * 1024;

    private static string GetSavePath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, "save_" + slotIndex + ".json");
    }

    /// <summary>True when a save file (or its backup) exists on disk for this slot.</summary>
    public static bool SaveFileExists(int slotIndex)
    {
        if (slotIndex < 0)
            return false;

        string path = GetSavePath(slotIndex);
        return File.Exists(path) || File.Exists(path + ".bak");
    }

    /// <summary>Serializes and writes the slot atomically. Keeps the previous file as .bak.</summary>
    public static void SaveGame(int slotIndex, GameData data)
    {
        if (data == null)
        {
            Debug.LogWarning($"SaveGame failed: GameData is null for slot {slotIndex}.");
            return;
        }

        if (slotIndex < 0)
        {
            Debug.LogError($"SaveGame failed: invalid slot index {slotIndex}.");
            return;
        }

        string path = GetSavePath(slotIndex);
        string tempPath = path + ".tmp";
        string backupPath = path + ".bak";

        try
        {
            string json = JsonUtility.ToJson(data);
            File.WriteAllText(tempPath, json);

            // Validate the temp file before touching the real save.
            FileInfo tempInfo = new FileInfo(tempPath);
            if (!tempInfo.Exists || tempInfo.Length == 0)
            {
                Debug.LogError($"Failed to save slot {slotIndex}: temporary save file was not written.");
                return;
            }

            if (File.Exists(path))
            {
                // Atomic swap; previous save becomes the backup.
                File.Replace(tempPath, path, backupPath);
            }
            else
            {
                File.Move(tempPath, path);
            }

            PlayerPrefs.SetInt("Slot_" + slotIndex + "_Exists", 1);
            PlayerPrefs.Save();
            Debug.Log("Game Saved to Slot " + slotIndex);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save game to slot {slotIndex}: {ex.Message}");
        }
    }

    /// <summary>Loads the slot, falling back to the .bak file when the main file is missing or corrupt.</summary>
    public static GameData LoadGame(int slotIndex)
    {
        if (slotIndex < 0)
            return null;

        string path = GetSavePath(slotIndex);

        GameData data = TryReadSaveFile(path, slotIndex);
        if (data != null)
            return data;

        // Main file missing/corrupt — try the backup from the last successful save.
        string backupPath = path + ".bak";
        data = TryReadSaveFile(backupPath, slotIndex);
        if (data != null)
            Debug.LogWarning($"Save slot {slotIndex} was recovered from its backup file.");

        return data;
    }

    private static GameData TryReadSaveFile(string path, int slotIndex)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            FileInfo info = new FileInfo(path);
            if (info.Length == 0 || info.Length > MaxSaveFileBytes)
            {
                Debug.LogError($"Save file for slot {slotIndex} has an invalid size ({info.Length} bytes).");
                return null;
            }

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

    /// <summary>Deletes the slot's save, backup, temp file, and PlayerPrefs metadata.</summary>
    public static void DeleteGame(int slotIndex)
    {
        if (slotIndex < 0)
            return;

        string path = GetSavePath(slotIndex);

        try
        {
            if (File.Exists(path))
                File.Delete(path);

            // Also remove backup/temp so a deleted slot cannot resurrect from them.
            if (File.Exists(path + ".bak"))
                File.Delete(path + ".bak");

            if (File.Exists(path + ".tmp"))
                File.Delete(path + ".tmp");
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
