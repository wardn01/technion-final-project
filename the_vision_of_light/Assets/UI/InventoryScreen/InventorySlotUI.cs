using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class InventorySlotUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI amountText;
    
    private ItemData itemData;

    public void Setup(ItemData item, int amount)
    {
        itemData = item;
        if (icon != null) icon.sprite = item.itemIcon;
        if (amountText != null) amountText.text = amount.ToString();
        
        Button btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnSlotClicked);
    }

    private void OnSlotClicked()
    {
        InventoryUIManager uiManager = FindObjectOfType<InventoryUIManager>();
        if (uiManager != null)
        {
            uiManager.DisplayItemDetails(itemData);
        }
    }
}