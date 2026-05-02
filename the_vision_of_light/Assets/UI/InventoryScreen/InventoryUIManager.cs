using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUIManager : MonoBehaviour
{
    [Header("References")]
    public PlayerCombat playerCombat;

    [Header("UI Panels")]
    public GameObject inventoryWindow; 
    public GameObject hudScreen;       
    public Transform slotsParent;      
    public GameObject slotPrefab;      

    [Header("Tab Title Settings")]
    public TextMeshProUGUI tabTitleText; 

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
            ShowDefaultDetails();
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

        if (isOpening)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
            RefreshUI();
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

        RefreshUI();
    }

    public void RefreshUI()
    {
        foreach (Transform child in slotsParent) Destroy(child.gameObject);

        Dictionary<ItemData, int> currentInv = InventoryManager.Instance.GetInventory();

        foreach (var kvp in currentInv)
        {
            ItemData item = kvp.Key;
            int amount = kvp.Value;

            if (currentFilter != TabFilter.All)
            {
                if (currentFilter == TabFilter.Material && item.type != ItemType.Material) continue;
                if (currentFilter == TabFilter.Consumable && item.type != ItemType.Consumable) continue;
                if (currentFilter == TabFilter.Weapon && item.type != ItemType.Weapon) continue;
            }

            GameObject newSlot = Instantiate(slotPrefab, slotsParent);
            newSlot.GetComponent<InventorySlotUI>().Setup(item, amount);
        }

        if (currentlySelectedItem != null)
        {
            if (InventoryManager.Instance.GetItemAmount(currentlySelectedItem) <= 0)
            {
                ShowDefaultDetails();
            }
        }
    }

    private void ShowDefaultDetails()
    {
        if (detailNameText != null) detailNameText.text = "Select an Item";
        if (detailDescriptionText != null) detailDescriptionText.text = "Click on any item to view its properties.";
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
            if (item.type == ItemType.Weapon)
            {
                equipButtonObject.SetActive(true);
                if (equipButtonText != null) equipButtonText.text = "Equip";
            }
            else if (item.type == ItemType.Consumable)
            {
                equipButtonObject.SetActive(true);
                if (equipButtonText != null) equipButtonText.text = "Use";
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

        if (currentlySelectedItem is WeaponItemData weaponData)
        {
            QuickSlotManager.Instance.AssignWeapon(weaponData, 1);
            
            ToggleInventory();
        }
        else if (currentlySelectedItem.type == ItemType.Consumable)
        {
            QuickSlotManager.Instance.AssignConsumable(currentlySelectedItem, 1);
            
            ToggleInventory();
        }
    }
}