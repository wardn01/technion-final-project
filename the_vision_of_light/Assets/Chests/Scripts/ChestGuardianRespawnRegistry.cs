using System.Collections.Generic;

namespace VisionOfLight.Chest
{
    /// <summary>
    /// Persists when all guardians for a chest were last defeated (UTC seconds).
    /// Used to respawn guardians on a timer even after the chest was opened.
    /// </summary>
    public static class ChestGuardianRespawnRegistry
    {
        private static readonly Dictionary<string, double> defeatedAtUtcByChestId = new Dictionary<string, double>();

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

        public static bool TryGetDefeatedTime(string chestId, out double defeatedAtUtc)
        {
            defeatedAtUtc = 0d;

            if (string.IsNullOrEmpty(chestId))
                return false;

            return defeatedAtUtcByChestId.TryGetValue(chestId, out defeatedAtUtc);
        }

        public static void MarkAllDefeated(string chestId)
        {
            if (string.IsNullOrEmpty(chestId))
                return;

            defeatedAtUtcByChestId[chestId] = GetUtcNow();
        }

        public static void ClearDefeatedTime(string chestId)
        {
            if (string.IsNullOrEmpty(chestId))
                return;

            defeatedAtUtcByChestId.Remove(chestId);
        }

        public static double GetUtcNow()
        {
            return System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
