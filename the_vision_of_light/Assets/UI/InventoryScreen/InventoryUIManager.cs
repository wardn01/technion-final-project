using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUIManager : MonoBehaviour
{
    public static InventoryUIManager Instance { get; private set; }

    [Header("References")]
    public PlayerCombat playerCombat;

    [Header("Currency")]
    public ItemData goldCoinItem;
    public TextMeshProUGUI goldAmountText;

    [Header("UI")]
    public GameObject inventoryWindow;
    public GameObject hudScreen;
    public Transform slotsParent;
    public GameObject slotPrefab;

    [Header("Quick Slots Movement")]
    public RectTransform quickSlotsBar;
    public Vector2 inventoryOpenPosition;
    private Vector2 normalPosition;

    [Header("Tabs")]
    public TextMeshProUGUI tabTitleText;
    public Image[] tabIcons;
    public Color selectedTabColor = Color.white;
    public Color unselectedTabColor = Color.gray;

    [Header("Details")]
    public GameObject detailsPanel;
    public TextMeshProUGUI detailNameText;
    public Image detailIconImage;
    public TextMeshProUGUI detailDescriptionText;

    [Header("Skills")]
    public GameObject windSwordSkillsGroup;
    public GameObject iceSwordSkillsGroup;

    [Header("Equip Button")]
    public GameObject equipButtonObject;
    public TextMeshProUGUI equipButtonText;
    
    private InventoryUI_Grid grid;
    private InventoryUI_EquipHandler equipHandler;
    public bool isItemClickedFromGrid = false;

    public enum TabFilter { All, Material, Consumable, Weapon }
    private TabFilter currentFilter = TabFilter.All;

    public ItemData currentlySelectedItem;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        grid = GetComponent<InventoryUI_Grid>();
        equipHandler = GetComponent<InventoryUI_EquipHandler>();
    }

    void Start()
    {
        inventoryWindow.SetActive(false);

        if (detailsPanel != null)
        {
            detailsPanel.SetActive(true);
            ShowEmptyCategoryDetails();
        }

        if (quickSlotsBar != null)
            normalPosition = quickSlotsBar.anchoredPosition;

        UpdateSkillHUD(null);
    }

    public void ToggleInventory()
    {
        bool isOpening = !inventoryWindow.activeSelf;
        inventoryWindow.SetActive(isOpening);

        if (hudScreen != null)
            hudScreen.SetActive(!isOpening);

        if (quickSlotsBar != null)
            quickSlotsBar.anchoredPosition = isOpening ? inventoryOpenPosition : normalPosition;

        Cursor.lockState = isOpening ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isOpening;
        Time.timeScale = isOpening ? 0f : 1f;

        if (isOpening)
            OnTabClicked((int)currentFilter);
        if (!isOpening)
            QuickSlotManager.Instance.ResetSelection();
    }

    public void OnTabClicked(int tabIndex)
    {
        currentFilter = (TabFilter)tabIndex;

        if (tabTitleText != null)
        {
            tabTitleText.text = currentFilter switch
            {
                TabFilter.All => "All Items",
                TabFilter.Material => "Materials",
                TabFilter.Consumable => "Food & Potions",
                TabFilter.Weapon => "Weapons",
                _ => ""
            };
        }

        for (int i = 0; i < tabIcons.Length; i++)
        {
            tabIcons[i].color = (i == tabIndex) ? selectedTabColor : unselectedTabColor;
        }

        currentlySelectedItem = null;
        RefreshUI();
    }

    public void RefreshUI()
    {
        grid.Refresh(currentFilter, goldCoinItem, goldAmountText, slotsParent, slotPrefab, ref currentlySelectedItem, this);
    }

    public void DisplayItemDetails(ItemData item, bool fromUserClick = false)
    {
        if (item == null) return;

        currentlySelectedItem = item;
        isItemClickedFromGrid = fromUserClick;

        detailNameText.text = item.itemName;
        detailIconImage.sprite = item.itemIcon;
        detailIconImage.color = Color.white;
        detailDescriptionText.text = item.description;

        if (item.type == ItemType.Weapon || item.type == ItemType.Consumable)
        {
            equipButtonObject.SetActive(true);
            equipButtonText.text = QuickSlotManager.Instance.IsItemEquipped(item) ? "Remove" :
                (item.type == ItemType.Weapon ? "Equip" : "Use");
        }
        else equipButtonObject.SetActive(false);
    }

    public void OnEquipButtonClicked()
    {
        equipHandler.Handle(this);
    }

    public void ShowEmptyCategoryDetails()
    {
        detailNameText.text = "Empty";
        detailDescriptionText.text = "No items here.";
        detailIconImage.color = new Color(1,1,1,0);
        equipButtonObject.SetActive(false);
        
        currentlySelectedItem = null;
        isItemClickedFromGrid = false;
    }

    public void UpdateSkillHUD(WeaponItemData weapon)
    {
        windSwordSkillsGroup.SetActive(false);
        iceSwordSkillsGroup.SetActive(false);

        if (weapon == null) return;

        if (weapon.weaponElement == WeaponItemData.WeaponElement.Wind)
            windSwordSkillsGroup.SetActive(true);
        else if (weapon.weaponElement == WeaponItemData.WeaponElement.Ice)
            iceSwordSkillsGroup.SetActive(true);
    }
}