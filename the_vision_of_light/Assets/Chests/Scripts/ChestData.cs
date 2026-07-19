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

            [Tooltip("Chance (0-100%) that this item drops at all when the chest is opened.")]
            [Range(0f, 100f)] public float dropChancePercent;

            [Tooltip("Amount granted. Also the minimum when Max Amount is higher.")]
            [Min(1)] public int amount;

            [Tooltip("If greater than Amount, a random amount between Amount and Max Amount is granted.")]
            [Min(0)] public int maxAmount;
        }

        [Tooltip("Items granted when a chest using this table is opened.")]
        public Entry[] entries;

        /// <summary>Adds every valid entry to the player inventory, applying drop chance and random amount.</summary>
        public void GrantToPlayer()
        {
            if (entries == null || entries.Length == 0 || InventoryManager.Instance == null)
                return;

            foreach (Entry entry in entries)
            {
                if (entry.item == null || entry.amount <= 0)
                    continue;

                float chance = entry.dropChancePercent <= 0f ? 100f : entry.dropChancePercent;
                if (Random.Range(0f, 100f) > chance)
                    continue;

                int rolledAmount = entry.maxAmount > entry.amount
                    ? Random.Range(entry.amount, entry.maxAmount + 1)
                    : entry.amount;

                if (rolledAmount <= 0)
                    continue;

                InventoryManager.Instance.AddItem(entry.item, rolledAmount);
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
