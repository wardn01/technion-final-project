using System.Collections.Generic;

namespace VisionOfLight.Chest
{
    /// <summary>
    /// Persists when all guardians for a chest were last defeated (UTC seconds).
    /// Used to respawn guardians on a timer even after the chest was opened.
    /// </summary>
    public static class ChestGuardianRespawnRegistry
    {
        #region Private State
        private static readonly Dictionary<string, double> defeatedAtUtcByChestId = new Dictionary<string, double>();
        #endregion

        #region Save / Load
        /// <summary>Restores guardian defeat timestamps from the active save slot.</summary>
        public static void ApplyFromSave(GameData data)
        {
            defeatedAtUtcByChestId.Clear();

            if (data?.chestGuardianDefeatTimes == null)
                return;

            foreach (ChestGuardianDefeatTime entry in data.chestGuardianDefeatTimes)
            {
                if (entry == null || string.IsNullOrEmpty(entry.chestId) || entry.defeatedAtUtc <= 0d)
                    continue;

                defeatedAtUtcByChestId[entry.chestId] = entry.defeatedAtUtc;
            }
        }

        /// <summary>Writes guardian defeat timestamps back into the active save slot.</summary>
        public static void WriteToSave(GameData data)
        {
            if (data == null)
                return;

            data.chestGuardianDefeatTimes = new List<ChestGuardianDefeatTime>();

            foreach (KeyValuePair<string, double> pair in defeatedAtUtcByChestId)
            {
                if (string.IsNullOrEmpty(pair.Key) || pair.Value <= 0d)
                    continue;

                data.chestGuardianDefeatTimes.Add(new ChestGuardianDefeatTime
                {
                    chestId = pair.Key,
                    defeatedAtUtc = pair.Value
                });
            }
        }
        #endregion

        #region Defeat Tracking
        /// <summary>Returns the UTC defeat time for a chest, if recorded.</summary>
        public static bool TryGetDefeatedTime(string chestId, out double defeatedAtUtc)
        {
            defeatedAtUtc = 0d;

            if (string.IsNullOrEmpty(chestId))
                return false;

            return defeatedAtUtcByChestId.TryGetValue(chestId, out defeatedAtUtc);
        }

        /// <summary>Records that all guardians for the chest were defeated at the current UTC time.</summary>
        public static void MarkAllDefeated(string chestId)
        {
            if (string.IsNullOrEmpty(chestId))
                return;

            defeatedAtUtcByChestId[chestId] = GetUtcNow();
        }

        /// <summary>Clears the stored defeat time for a chest (e.g. after guardians respawn).</summary>
        public static void ClearDefeatedTime(string chestId)
        {
            if (string.IsNullOrEmpty(chestId))
                return;

            defeatedAtUtcByChestId.Remove(chestId);
        }
        #endregion

        #region Time
        /// <summary>Returns the current UTC time as Unix seconds.</summary>
        public static double GetUtcNow()
        {
            return System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        #endregion
    }
}
