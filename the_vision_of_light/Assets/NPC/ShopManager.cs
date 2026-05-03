using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("UI References")]
    public GameObject shopPanel;
    public TextMeshProUGUI goldText;
    public Slider amountSlider;
    public TextMeshProUGUI totalPriceText;

    [Header("Economy References")]
    public ItemData goldItemData; 

    private ItemData selectedItem;
    private int currentAmount = 1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void OpenShop()
    {
        shopPanel.SetActive(true);
        UpdateGoldUI();
        amountSlider.value = 1; 
        UpdateTotalPrice();

        Time.timeScale = 0f; 

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);

        Time.timeScale = 1f; 

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SelectItem(ItemData item)
    {
        selectedItem = item;
        amountSlider.value = 1; 
        UpdateTotalPrice();
    }

    public void OnSliderValueChanged()
    {
        currentAmount = (int)amountSlider.value; 
        UpdateTotalPrice();
    }

    private void UpdateTotalPrice()
    {
        if (selectedItem != null)
        {
            int total = selectedItem.value * currentAmount; 
            totalPriceText.text = "Total: " + total + " Gold";
        }
        else
        {
            totalPriceText.text = "Select an item";
        }
    }

    public void BuySelectedItem()
    {
        if (selectedItem == null || goldItemData == null) return;

        int totalCost = selectedItem.value * currentAmount;

        int currentGold = InventoryManager.Instance.GetItemAmount(goldItemData);

        if (currentGold >= totalCost)
        {
            InventoryManager.Instance.RemoveItem(goldItemData, totalCost);
            
            InventoryManager.Instance.AddItem(selectedItem, currentAmount);

            UpdateGoldUI();
        }

    }

    public void UpdateGoldUI()
    {
        if (goldText != null && goldItemData != null)
        {
            int currentGold = InventoryManager.Instance.GetItemAmount(goldItemData);
            goldText.text = "Gold: " + currentGold;
        }
    }
}