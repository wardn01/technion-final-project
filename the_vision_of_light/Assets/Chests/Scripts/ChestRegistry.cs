using System.Collections.Generic;

namespace VisionOfLight.Chest
{
    /// <summary>
    /// Tracks one-time opened world chests for save/load.
    /// </summary>
    public static class ChestRegistry
    {
        #region Private State
        private static readonly HashSet<string> openedChestIds = new HashSet<string>();
        #endregion

        #region Save / Load
        /// <summary>Restores opened chest ids from the active save slot.</summary>
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

        /// <summary>Writes opened chest ids back into the active save slot.</summary>
        public static void WriteToSave(GameData data)
        {
            if (data == null)
                return;

            data.openedChestIds = new List<string>(openedChestIds);
        }
        #endregion

        #region Opened State
        /// <summary>Returns true when the chest has already been opened in this save.</summary>
        public static bool IsOpened(string chestId)
        {
            return !string.IsNullOrEmpty(chestId) && openedChestIds.Contains(chestId);
        }

        /// <summary>Marks a chest as opened for the active save.</summary>
        public static void MarkOpened(string chestId)
        {
            if (string.IsNullOrEmpty(chestId))
                return;

            openedChestIds.Add(chestId);
        }
        #endregion
    }
}
