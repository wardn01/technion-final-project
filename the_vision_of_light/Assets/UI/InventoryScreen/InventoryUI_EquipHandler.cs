using UnityEngine;

public class InventoryUI_EquipHandler : MonoBehaviour
{
    public void Handle(InventoryUIManager ui)
    {
        var item = ui.currentlySelectedItem;
        if (item == null) return;

        if (QuickSlotManager.Instance.IsItemEquipped(item))
            Remove(ui, item);
        else
            Equip(ui, item);

        ui.DisplayItemDetails(item, true); 
        ui.RefreshUI();
    }

    private void Remove(InventoryUIManager ui, ItemData item)
    {
        QuickSlotManager.Instance.ClearItemFromAllSlots(item);

        if (item.type == ItemType.Weapon)
        {
            ui.playerCombat.UnequipCurrentWeapon();
            ui.UpdateSkillHUD(null);
        }
    }

    private void Equip(InventoryUIManager ui, ItemData item)
    {
        if (!ui.playerCombat.IsSafeToEquip())
        {
            Debug.Log("Finish attack first!");
            return;
        }

        QuickSlotManager.Instance.AssignToFirstEmptySlot(item);

        if (item is WeaponItemData weapon)
            ui.DisplayItemDetails(item, ui.isItemClickedFromGrid);    }
}