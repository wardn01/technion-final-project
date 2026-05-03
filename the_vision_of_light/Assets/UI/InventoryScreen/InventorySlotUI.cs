using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class InventorySlotUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI amountText;
    
    private ItemData itemData;

    private InventoryUIManager uiManager;

    public void Setup(ItemData item, int amount, InventoryUIManager manager)
    {
        itemData = item;
        uiManager = manager;
        if (icon != null) icon.sprite = item.itemIcon;
        
        if (amountText != null)
        {
            if (item.type == ItemType.Weapon)
            {
                amountText.text = "-";
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
        if (uiManager != null)
            uiManager.DisplayItemDetails(itemData, true);
    }
}