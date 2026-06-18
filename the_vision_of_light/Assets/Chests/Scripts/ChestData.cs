using UnityEngine;

namespace VisionOfLight.Chest
{
    public enum ChestVisualType
    {
        Wood,
        Stone,
        Gold
    }

    /// <summary>
    /// Reusable loot list for world chests. Assign one table per chest instead of duplicating entries.
    /// </summary>
    [CreateAssetMenu(fileName = "NewChestLootTable", menuName = "Vision Of Light/Chest Loot Table")]
    public class ChestLootTable : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public ItemData item;
            [Min(1)] public int amount;
        }

        [Tooltip("Items granted when a chest using this table is opened.")]
        public Entry[] entries;

        /// <summary>Adds every valid entry to the player inventory.</summary>
        public void GrantToPlayer()
        {
            if (entries == null || entries.Length == 0 || InventoryManager.Instance == null)
                return;

            foreach (Entry entry in entries)
            {
                if (entry.item == null || entry.amount <= 0)
                    continue;

                InventoryManager.Instance.AddItem(entry.item, entry.amount);
            }
        }

        public bool HasLoot()
        {
            if (entries == null || entries.Length == 0)
                return false;

            foreach (Entry entry in entries)
            {
                if (entry.item != null && entry.amount > 0)
                    return true;
            }

            return false;
        }
    }
}
