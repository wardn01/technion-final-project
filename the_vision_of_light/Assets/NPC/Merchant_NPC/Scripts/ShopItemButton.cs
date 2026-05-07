using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemButton : MonoBehaviour
{
    public ItemData item;
    
    [Header("UI Elements")]
    public Image itemIcon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;

    private void Start()
    {
        if (item != null)
        {
            if (itemIcon != null) itemIcon.sprite = item.itemIcon;
            if (nameText != null) nameText.text = item.itemName;
            if (priceText != null) priceText.text = item.value.ToString();
        }
    }

    public void SelectThisItem()
    {
        if (ShopManager.Instance != null && item != null)
        {
            ShopManager.Instance.SelectItem(item);
        }
    }
}