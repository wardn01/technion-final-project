using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class InventorySlotUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI amountText;
    
    private ItemData itemData;

    private InventoryUIManager invManager;
    private WeaponUpgradeUI weaponManager;

    public void Setup(ItemData item, int amount, InventoryUIManager manager)
    {
        invManager = manager;
        weaponManager = null; 
        InternalSetup(item, amount);
    }

    public void Setup(ItemData item, int amount, WeaponUpgradeUI manager)
    {
        weaponManager = manager;
        invManager = null; 
        InternalSetup(item, amount);
    }

    private void InternalSetup(ItemData item, int amount)
    {
        itemData = item;
        if (icon != null) icon.sprite = item.itemIcon;
        
        if (amountText != null)
        {
            if (item.type == ItemType.Weapon)
            {
                int lvl = PlayerData.Instance.GetWeaponLevel(item.itemName);
                amountText.text = "Lv." + lvl;
            }
            else
            {
                amountText.text = amount.ToString();
            }
        }
        
        Button btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnSlotClicked);
    }

    private void OnSlotClicked()
    {
        if (invManager != null)
        {
            invManager.DisplayItemDetails(itemData, true);
        }
        else if (weaponManager != null)
        {
            weaponManager.DisplayItemDetails(itemData, true);
        }
    }
}