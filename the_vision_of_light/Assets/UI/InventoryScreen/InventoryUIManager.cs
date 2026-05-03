using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUIManager : MonoBehaviour
{
    [Header("References")]
    public PlayerCombat playerCombat;

    [Header("Currency Settings")]
    public ItemData goldCoinItem;       
    public TextMeshProUGUI goldAmountText; 

    [Header("UI Panels")]
    public GameObject inventoryWindow; 
    public GameObject hudScreen;       
    public Transform slotsParent;      
    public GameObject slotPrefab;      

    [Header("Quick Slots UI Movement")]
    public RectTransform quickSlotsBar; 
    public Vector2 inventoryOpenPosition; 
    private Vector2 normalPosition; 

    [Header("Tab Title Settings")]
    public TextMeshProUGUI tabTitleText; 
    
    [Header("Tab Visuals")]
    public Image[] tabIcons; 
    public Color selectedTabColor = Color.white; 
    public Color unselectedTabColor = new Color(0.5f, 0.5f, 0.5f, 1f); 

    [Header("Item Details UI")]
    public GameObject detailsPanel;           
    public TextMeshProUGUI detailNameText;    
    public Image detailIconImage;             
    public TextMeshProUGUI detailDescriptionText; 
    
    public GameObject equipButtonObject;
    public TextMeshProUGUI equipButtonText;

    public enum TabFilter { All, Material, Consumable, Weapon }
    private TabFilter currentFilter = TabFilter.All;

    public ItemData currentlySelectedItem; 

    void Start()
    {
        inventoryWindow.SetActive(false);
        if (detailsPanel != null) 
        {
            detailsPanel.SetActive(true); 
            ShowEmptyCategoryDetails();
        }

        if (quickSlotsBar != null)
        {
            normalPosition = quickSlotsBar.anchoredPosition;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I)) ToggleInventory();
    }

    private void ToggleInventory()
    {
        bool isOpening = !inventoryWindow.activeSelf;
        inventoryWindow.SetActive(isOpening);
        
        if (hudScreen != null) hudScreen.SetActive(!isOpening);

        if (quickSlotsBar != null)
        {
            quickSlotsBar.anchoredPosition = isOpening ? inventoryOpenPosition : normalPosition;
        }

        if (isOpening)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
            
            OnTabClicked((int)currentFilter); 
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f;
        }
    }

    public void OnTabClicked(int tabIndex)
    {
        currentFilter = (TabFilter)tabIndex;

        if (tabTitleText != null)
        {
            switch (currentFilter)
            {
                case TabFilter.All: tabTitleText.text = "All Items"; break;
                case TabFilter.Material: tabTitleText.text = "Materials"; break;
                case TabFilter.Consumable: tabTitleText.text = "Food & Potions"; break;
                case TabFilter.Weapon: tabTitleText.text = "Weapons"; break;
            }
        }

        if (tabIcons != null && tabIcons.Length > 0)
        {
            for (int i = 0; i < tabIcons.Length; i++)
            {
                if (tabIcons[i] != null)
                {
                    tabIcons[i].color = (i == tabIndex) ? selectedTabColor : unselectedTabColor;
                }
            }
        }

        currentlySelectedItem = null; 
        RefreshUI();
    }

    public void RefreshUI()
    {
        foreach (Transform child in slotsParent) Destroy(child.gameObject);

        if (goldCoinItem != null && goldAmountText != null)
        {
            int goldCount = InventoryManager.Instance.GetItemAmount(goldCoinItem);
            goldAmountText.text = goldCount.ToString();
        }

        Dictionary<ItemData, int> currentInv = InventoryManager.Instance.GetInventory();
        
        ItemData firstItemFound = null; 
        bool isCurrentlySelectedItemStillValid = false;

        foreach (var kvp in currentInv)
        {
            ItemData item = kvp.Key;
            int amount = kvp.Value;

            if (item == goldCoinItem) continue;

            if (currentFilter != TabFilter.All)
            {
                if (currentFilter == TabFilter.Material && item.type != ItemType.Material) continue;
                if (currentFilter == TabFilter.Consumable && item.type != ItemType.Consumable) continue;
                if (currentFilter == TabFilter.Weapon && item.type != ItemType.Weapon) continue;
            }

            if (firstItemFound == null) firstItemFound = item;
            
            if (currentlySelectedItem == item && amount > 0) isCurrentlySelectedItemStillValid = true;

            GameObject newSlot = Instantiate(slotPrefab, slotsParent);
            newSlot.GetComponent<InventorySlotUI>().Setup(item, amount);
        }

        if (isCurrentlySelectedItemStillValid)
        {
            DisplayItemDetails(currentlySelectedItem);
        }
        else if (firstItemFound != null)
        {
            DisplayItemDetails(firstItemFound);
        }
        else
        {
            ShowEmptyCategoryDetails();
        }
    }

    private void ShowEmptyCategoryDetails()
    {
        if (detailNameText != null) detailNameText.text = "Empty";
        if (detailDescriptionText != null) detailDescriptionText.text = "There are no items in this category.";
        if (detailIconImage != null) detailIconImage.color = new Color(1, 1, 1, 0);

        currentlySelectedItem = null;

        if (equipButtonObject != null) equipButtonObject.SetActive(false);
    }

    public void DisplayItemDetails(ItemData item)
    {
        if (item == null) return;

        currentlySelectedItem = item;

        if (detailNameText != null) detailNameText.text = item.itemName;

        if (detailIconImage != null)
        {
            detailIconImage.sprite = item.itemIcon;
            detailIconImage.color = Color.white;
        }

        if (detailDescriptionText != null) detailDescriptionText.text = item.description;

        if (equipButtonObject != null)
        {
            if (item.type == ItemType.Weapon || item.type == ItemType.Consumable)
            {
                equipButtonObject.SetActive(true);

                if (QuickSlotManager.Instance.IsItemEquipped(item))
                {
                    if (equipButtonText != null) equipButtonText.text = "Remove";
                }
                else
                {
                    if (equipButtonText != null) 
                        equipButtonText.text = (item.type == ItemType.Weapon) ? "Equip" : "Use";
                }
            }
            else
            {
                equipButtonObject.SetActive(false);
            }
        }
    }

    public void OnEquipButtonClicked()
    {
        if (currentlySelectedItem == null) return;

        if (QuickSlotManager.Instance.IsItemEquipped(currentlySelectedItem))
        {
            QuickSlotManager.Instance.ClearItemFromAllSlots(currentlySelectedItem);
            DisplayItemDetails(currentlySelectedItem); 
        }
        else
        {
            QuickSlotManager.Instance.AssignItem(currentlySelectedItem, 1);
            DisplayItemDetails(currentlySelectedItem); 
        }
    }
}