using System.Collections.Generic;

namespace VisionOfLight.Chest
{
    /// <summary>
    /// Tracks one-time opened world chests for save/load.
    /// </summary>
    public static class ChestRegistry
    {
        private static readonly HashSet<string> openedChestIds = new HashSet<string>();

        public static void ApplyFromSave(GameData data)
        {
            openedChestIds.Clear();

            if (data?.openedChestIds == null)
                return;

            foreach (string id in data.openedChestIds)
            {
                if (!string.IsNullOrEmpty(id))
                    openedChestIds.Add(id);
            }
        }

        public static void WriteToSave(GameData data)
        {
            if (data == null)
                return;

            data.openedChestIds = new List<string>(openedChestIds);
        }

        public static bool IsOpened(string chestId)
        {
            return !string.IsNullOrEmpty(chestId) && openedChestIds.Contains(chestId);
        }

        public static void MarkOpened(string chestId)
        {
            if (string.IsNullOrEmpty(chestId))
                return;

            openedChestIds.Add(chestId);
        }
    }
}
