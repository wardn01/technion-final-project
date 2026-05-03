using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InventoryUI_Grid : MonoBehaviour
{
    private List<GameObject> pool = new List<GameObject>();

    public void Refresh(
        InventoryUIManager.TabFilter filter,
        ItemData goldItem,
        TextMeshProUGUI goldText,
        Transform parent,
        GameObject prefab,
        ref ItemData selected,
        InventoryUIManager ui)
    {
        var inv = InventoryManager.Instance.GetInventory();

        if (goldItem != null && goldText != null)
            goldText.text = InventoryManager.Instance.GetItemAmount(goldItem).ToString();

        int index = 0;
        ItemData first = null;
        bool valid = false;

        foreach (var kvp in inv)
        {
            ItemData item = kvp.Key;
            int amount = kvp.Value;

            if (item == goldItem) continue;

            if (filter != InventoryUIManager.TabFilter.All)
            {
                if (filter == InventoryUIManager.TabFilter.Material && item.type != ItemType.Material) continue;
                if (filter == InventoryUIManager.TabFilter.Consumable && item.type != ItemType.Consumable) continue;
                if (filter == InventoryUIManager.TabFilter.Weapon && item.type != ItemType.Weapon) continue;
            }

            if (first == null) first = item;
            if (selected == item && amount > 0) valid = true;

            GameObject slot;

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

            slot.GetComponent<InventorySlotUI>().Setup(item, amount, ui);            
            index++;
        }

        for (int i = index; i < pool.Count; i++)
            pool[i].SetActive(false);

        if (valid) ui.DisplayItemDetails(selected, ui.isItemClickedFromGrid);
        else if (first != null) ui.DisplayItemDetails(first, false);
        else ui.ShowEmptyCategoryDetails();
    }
}