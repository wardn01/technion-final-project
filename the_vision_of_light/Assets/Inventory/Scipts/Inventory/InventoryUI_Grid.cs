using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

/// <summary>
/// Orchestrates the dynamic rendering of the inventory grid.
/// Implements the Object Pooling pattern to minimize garbage collection 
/// and utilizes LINQ to perform efficient item filtering by category.
/// </summary>
public class InventoryUI_Grid : MonoBehaviour
{
    #region Pooling

    private class PooledSlot
    {
        public GameObject Object;
        public bool IsWeaponSlot;
    }

    /// <summary>
    /// A persistent cache of instantiated UI slot objects.
    /// Reusing these objects instead of destroying/instantiating them 
    /// significantly improves UI frame-rate stability.
    /// </summary>
    private readonly List<PooledSlot> pool = new List<PooledSlot>();

    #endregion

    #region Grid Refresh

    /// <summary>
    /// Rebuilds the inventory grid from scratch. 
    /// Filters the inventory data based on the provided tab filter,
    /// populates the UI using pooled objects, and manages selection state.
    /// </summary>
    /// <param name="filter">The current category filter active in the UI.</param>
    /// <param name="goldItem">Data reference to the gold currency item.</param>
    /// <param name="goldText">TMP text element for displaying gold count.</param>
    /// <param name="parent">The transform serving as the layout parent for the grid.</param>
    /// <param name="defaultPrefab">Prefab used for materials, consumables, and mixed tabs.</param>
    /// <param name="weaponPrefab">Optional dedicated weapon slot prefab. Falls back to defaultPrefab when null.</param>
    /// <param name="selected">Reference to the currently highlighted item (by-reference).</param>
    /// <param name="onSlotClickAction">Event to trigger when an item slot is clicked.</param>
    /// <param name="ui">Reference to the InventoryUIManager for detail panel updates.</param>
    public void Refresh(
        InventoryUIManager.TabFilter filter,
        ItemData goldItem,
        TextMeshProUGUI goldText,
        Transform parent,
        GameObject defaultPrefab,
        GameObject weaponPrefab,
        ref ItemData selected,
        Action<ItemData> onSlotClickAction,
        InventoryUIManager ui)
    {
        var inv = InventoryManager.Instance.GetInventory();

        if (goldItem != null && goldText != null)
            goldText.text = InventoryManager.Instance.GetItemAmount(goldItem).ToString();

        var filteredItems = inv.Where(kvp =>
            kvp.Key != goldItem &&
            IsMatchFilter(kvp.Key.type, filter)
        ).ToList();

        int index = 0;
        ItemData first = null;
        bool isSelectedValid = false;

        foreach (var kvp in filteredItems)
        {
            ItemData item = kvp.Key;
            int amount = kvp.Value;

            if (first == null) first = item;
            if (selected == item && amount > 0) isSelectedValid = true;

            GameObject slot = AcquireSlot(index, item, parent, defaultPrefab, weaponPrefab);
            slot.GetComponent<InventorySlotUI>()?.Setup(item, amount, onSlotClickAction);

            index++;
        }

        for (int i = index; i < pool.Count; i++)
            pool[i].Object.SetActive(false);

        if (isSelectedValid)
            ui.DisplayItemDetails(selected, ui.isItemClickedFromGrid);
        else if (first != null)
            ui.DisplayItemDetails(first, false);
        else
            ui.ShowEmptyCategoryDetails();
    }

    /// <summary>
    /// Returns a pooled slot of the correct prefab type for the given item.
    /// Recreates the slot when the required prefab type changes (e.g. All tab mix).
    /// </summary>
    private GameObject AcquireSlot(int index, ItemData item, Transform parent, GameObject defaultPrefab, GameObject weaponPrefab)
    {
        bool useWeaponSlot = item.type == ItemType.Weapon && weaponPrefab != null;
        GameObject prefab = useWeaponSlot ? weaponPrefab : defaultPrefab;

        while (pool.Count <= index)
            pool.Add(null);

        PooledSlot entry = pool[index];

        if (entry != null && entry.IsWeaponSlot == useWeaponSlot)
        {
            entry.Object.SetActive(true);
            return entry.Object;
        }

        if (entry?.Object != null)
            Destroy(entry.Object);

        GameObject slot = Instantiate(prefab, parent);
        pool[index] = new PooledSlot
        {
            Object = slot,
            IsWeaponSlot = useWeaponSlot
        };

        return slot;
    }

    #endregion

    #region Filtering

    /// <summary>
    /// Checks if a specific item type should be displayed under the current tab filter.
    /// </summary>
    /// <param name="type">The ItemType of the item being checked.</param>
    /// <param name="filter">The active tab filter.</param>
    /// <returns>True if the item belongs to the filtered category.</returns>
    private bool IsMatchFilter(ItemType type, InventoryUIManager.TabFilter filter)
    {
        if (filter == InventoryUIManager.TabFilter.All) return true;

        return filter switch
        {
            InventoryUIManager.TabFilter.Material => type == ItemType.Material,
            InventoryUIManager.TabFilter.Consumable => type == ItemType.Consumable,
            InventoryUIManager.TabFilter.Weapon => type == ItemType.Weapon,
            _ => false
        };
    }

    #endregion
}
