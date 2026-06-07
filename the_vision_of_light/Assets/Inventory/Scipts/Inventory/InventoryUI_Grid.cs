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

    /// <summary>
    /// A persistent cache of instantiated UI slot objects.
    /// Reusing these objects instead of destroying/instantiating them 
    /// significantly improves UI frame-rate stability.
    /// </summary>
    private List<GameObject> pool = new List<GameObject>();

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
    /// <param name="prefab">The slot prefab to instantiate if pool size is insufficient.</param>
    /// <param name="selected">Reference to the currently highlighted item (by-reference).</param>
    /// <param name="onSlotClickAction">Event to trigger when an item slot is clicked.</param>
    /// <param name="ui">Reference to the InventoryUIManager for detail panel updates.</param>
    public void Refresh(
        InventoryUIManager.TabFilter filter,
        ItemData goldItem,
        TextMeshProUGUI goldText,
        Transform parent,
        GameObject prefab,
        ref ItemData selected,
        Action<ItemData> onSlotClickAction,
        InventoryUIManager ui)
    {
        var inv = InventoryManager.Instance.GetInventory();

        // Update top-bar gold display
        if (goldItem != null && goldText != null)
            goldText.text = InventoryManager.Instance.GetItemAmount(goldItem).ToString();

        // Filter inventory collection using LINQ for readability and performance
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

            GameObject slot;

            // Retrieve from pool or instantiate new slot
            if (index < pool.Count)
            {
                slot = pool[index];
                slot.SetActive(true);
            }
            else
            {
                slot = Instantiate(prefab, parent);
                pool.Add(slot);
            }

            // Assign data to the slot UI component
            slot.GetComponent<InventorySlotUI>()
                .Setup(item, amount, onSlotClickAction);

            index++;
        }

        // Hide slots that are not needed in this filter state
        for (int i = index; i < pool.Count; i++)
            pool[i].SetActive(false);

        // Synchronize the detail panel state
        if (isSelectedValid)
            ui.DisplayItemDetails(selected, ui.isItemClickedFromGrid);
        else if (first != null)
            ui.DisplayItemDetails(first, false);
        else
            ui.ShowEmptyCategoryDetails();
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