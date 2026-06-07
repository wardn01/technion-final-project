using UnityEngine;

/// <summary>
/// Orchestrates the equipment lifecycle for inventory items.
/// Acts as a bridge between the Inventory UI and the QuickSlot/Combat systems,
/// ensuring safe item state transitions (equipping/unequipping).
/// </summary>
public class InventoryUI_EquipHandler : MonoBehaviour
{
    #region Equipment Handling

    /// <summary>
    /// Executes the primary logic for toggling an item's equipped status.
    /// Redirects to internal <see cref="Equip"/> or <see cref="Remove"/> methods 
    /// based on the item's current state.
    /// </summary>
    /// <param name="ui">Reference to the active InventoryUIManager instance.</param>
    public void Handle(InventoryUIManager ui)
    {
        var item = ui.currentlySelectedItem;

        if (item == null)
            return;

        // Determine state based on presence in QuickSlotManager
        if (QuickSlotManager.Instance.IsItemEquipped(item))
            Remove(ui, item);
        else
            Equip(ui, item);

        // Refresh UI to reflect the new state (e.g., updating button text)
        ui.DisplayItemDetails(item, true);
        ui.RefreshUI();
    }

    /// <summary>
    /// Handles the cleanup of an unequipped item.
    /// Clears the item from quick slots and signals the PlayerCombat system 
    /// to update weapon visuals and active skills.
    /// </summary>
    /// <param name="ui">Reference to the InventoryUIManager.</param>
    /// <param name="item">The ItemData of the weapon or consumable being unequipped.</param>
    private void Remove(InventoryUIManager ui, ItemData item)
    {
        QuickSlotManager.Instance.ClearItemFromAllSlots(item);

        // Specific handling for weapons to reset combat state
        if (item.type == ItemType.Weapon)
        {
            ui.playerCombat.UnequipCurrentWeapon();
            ui.UpdateSkillHUD(null);
        }
    }

    /// <summary>
    /// Attempts to equip an item by assigning it to the QuickSlot system.
    /// Validates combat state to prevent equipping weapons mid-attack.
    /// </summary>
    /// <param name="ui">Reference to the InventoryUIManager.</param>
    /// <param name="item">The ItemData to be equipped.</param>
    private void Equip(InventoryUIManager ui, ItemData item)
    {
        // Prevent state corruption by checking if player is currently locked in an animation
        if (!ui.playerCombat.IsSafeToEquip())
        {
            Debug.Log("Equip aborted: Player is currently attacking.");
            return;
        }

        QuickSlotManager.Instance.AssignToFirstEmptySlot(item);

        // Update detail panel if it's a weapon to show potential stats/skill changes
        if (item is WeaponItemData)
            ui.DisplayItemDetails(item, ui.isItemClickedFromGrid);
    }

    #endregion
}