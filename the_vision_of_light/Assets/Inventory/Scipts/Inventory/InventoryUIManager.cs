using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VisionOfLight.Player;

/// <summary>
/// The central orchestrator for the Inventory User Interface.
/// Manages tab navigation, item detail rendering, HUD transitions, 
/// and delegates specific responsibilities to specialized handlers (Grid and Equip).
/// </summary>
public class InventoryUIManager : MonoBehaviour
{
    #region Singleton

    /// <summary>
    /// Global singleton instance ensuring only one Inventory UI manager operates at runtime.
    /// </summary>
    public static InventoryUIManager Instance { get; private set; }

    #endregion

    #region References & UI Elements

    [Header("References")]
    /// <summary>Reference to the player's combat system to update skills and weapon states.</summary>
    public PlayerCombat playerCombat;
    /// <summary>Reference to persistent player data for weapon level and damage display.</summary>
    public PlayerData playerData;

    [Header("Currency")]
    /// <summary>The data definition for the gold currency used to query the player's balance.</summary>
    public ItemData goldCoinItem;
    /// <summary>The UI text element displaying the current gold balance.</summary>
    public TextMeshProUGUI goldAmountText;

    [Header("UI Windows")]
    public GameObject inventoryWindow;
    public GameObject hudScreen;
    public Transform slotsParent;
    public GameObject slotPrefab;

    [Header("Weapon Slot")]
    [Tooltip("Optional dedicated slot prefab for weapons. Leave empty to reuse slotPrefab.")]
    public GameObject weaponSlotPrefab;

    [Header("Quick Slots Movement")]
    /// <summary>The transform of the quick slots bar, dynamically repositioned when the inventory opens.</summary>
    public RectTransform quickSlotsBar;
    /// <summary>The anchored position of the quick slots bar when the inventory is actively displayed.</summary>
    public Vector2 inventoryOpenPosition;
    private Vector2 normalPosition;

    [Header("Tabs")]
    public TextMeshProUGUI tabTitleText;
    public Image[] tabIcons;
    public Color selectedTabColor = Color.white;
    public Color unselectedTabColor = Color.gray;

    [Header("Details Panel - General")]
    public GameObject detailsPanel;
    public TextMeshProUGUI detailNameText;
    public Image detailIconImage;
    public TextMeshProUGUI detailDescriptionText;

    [Header("Details Panel - Weapon Only")]
    public Image weaponDetailsIconImage;
    public TextMeshProUGUI weaponDetailLevelText;

    [Header("Skills HUD")]
    public GameObject windSwordSkillsGroup;
    public GameObject iceSwordSkillsGroup;
    public GameObject fireSwordSkillsGroup;

    [Header("Equip Button")]
    public GameObject equipButtonObject;
    public TextMeshProUGUI equipButtonText;

    #endregion

    #region Managers & State

    private InventoryUI_Grid grid;
    private InventoryUI_EquipHandler equipHandler;

    /// <summary>
    /// Tracks if the currently displayed item was clicked directly from the grid, 
    /// preventing unintentional auto-equips from background refreshes.
    /// </summary>
    [HideInInspector]
    public bool isItemClickedFromGrid = false;

    /// <summary>
    /// Defines the available filtering categories for the inventory grid.
    /// </summary>
    public enum TabFilter
    {
        All,
        Material,
        Consumable,
        Weapon
    }

    private TabFilter currentFilter = TabFilter.All;
    
    /// <summary>
    /// The item currently selected and displayed in the details panel.
    /// </summary>
    [HideInInspector]
    public ItemData currentlySelectedItem;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Initializes the singleton and binds required helper components.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        grid = GetComponent<InventoryUI_Grid>();
        equipHandler = GetComponent<InventoryUI_EquipHandler>();
    }

    /// <summary>
    /// Sets initial UI states, caches default positions, and hides the inventory on startup.
    /// </summary>
    private void Start()
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

    #endregion

    #region Window & Tab Management

    /// <summary>
    /// Toggles the main inventory window visibility.
    /// Handles visual transitions of the HUD and Quick Slots, while leaving 
    /// time scale and input locking management to the PauseMenuManager.
    /// </summary>
    public void ToggleInventory()
    {
        bool isOpening = !inventoryWindow.activeSelf;
        inventoryWindow.SetActive(isOpening);

        if (hudScreen != null)
            hudScreen.SetActive(!isOpening);

        if (quickSlotsBar != null)
        {
            quickSlotsBar.anchoredPosition = isOpening ? inventoryOpenPosition : normalPosition;
        }

        if (isOpening)
            OnTabClicked((int)currentFilter);

        if (!isOpening)
            QuickSlotManager.Instance.ResetSelection();
    }

    /// <summary>
    /// Updates the active category filter, applies visual highlights to the selected tab, 
    /// and triggers a grid rebuild.
    /// </summary>
    /// <param name="tabIndex">The integer value corresponding to the TabFilter enum.</param>
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

    #endregion

    #region Grid & Details Rendering

    /// <summary>
    /// Rebuilds the inventory grid by passing the current state and dependencies to the Grid handler.
    /// </summary>
    public void RefreshUI()
    {
        grid.Refresh(
            currentFilter,
            goldCoinItem,
            goldAmountText,
            slotsParent,
            slotPrefab,
            weaponSlotPrefab,
            ref currentlySelectedItem,
            HandleSlotClick,
            this);
    }

    /// <summary>
    /// Callback method passed to individual slots to handle user clicks.
    /// </summary>
    /// <param name="clickedItem">The item data associated with the clicked slot.</param>
    private void HandleSlotClick(ItemData clickedItem)
    {
        DisplayItemDetails(clickedItem, true);
    }

    /// <summary>
    /// Populates the side details panel with the selected item's specific data.
    /// Evaluates whether the item can be equipped or consumed to contextualize the action button.
    /// </summary>
    /// <param name="item">The item to display.</param>
    /// <param name="fromUserClick">True if triggered by a direct click, False if auto-selected.</param>
    public void DisplayItemDetails(ItemData item, bool fromUserClick = false)
    {
        if (item == null) return;

        currentlySelectedItem = item;
        isItemClickedFromGrid = fromUserClick;

        detailNameText.text = item.itemName;

        bool isWeapon = item.type == ItemType.Weapon;
        ShowGeneralDetails(isWeapon, item);
        ShowWeaponDetails(isWeapon, item);

        if (item.type == ItemType.Weapon || item.type == ItemType.Consumable)
        {
            equipButtonObject.SetActive(true);

            equipButtonText.text = QuickSlotManager.Instance.IsItemEquipped(item) 
                ? "Remove" 
                : (item.type == ItemType.Weapon ? "Equip" : "Use");
        }
        else
        {
            equipButtonObject.SetActive(false);
        }
    }

    /// <summary>
    /// Shows the standard icon for non-weapons and the shared description for all item types.
    /// </summary>
    private void ShowGeneralDetails(bool isWeapon, ItemData item)
    {
        if (detailIconImage != null)
        {
            detailIconImage.gameObject.SetActive(!isWeapon && item != null);

            if (!isWeapon && item != null)
            {
                detailIconImage.sprite = item.itemIcon;
                detailIconImage.color = Color.white;
            }
        }

        if (detailDescriptionText != null)
        {
            detailDescriptionText.gameObject.SetActive(item != null);

            if (item != null)
                detailDescriptionText.text = item.description;
        }
    }

    /// <summary>
    /// Shows the dedicated weapon preview and level text.
    /// </summary>
    private void ShowWeaponDetails(bool isWeapon, ItemData item)
    {
        if (weaponDetailsIconImage != null)
        {
            weaponDetailsIconImage.gameObject.SetActive(isWeapon);

            if (isWeapon && item != null)
            {
                weaponDetailsIconImage.sprite = item.itemIcon;
                weaponDetailsIconImage.color = Color.white;
            }
        }

        if (weaponDetailLevelText != null)
            weaponDetailLevelText.gameObject.SetActive(isWeapon);

        if (!isWeapon || item is not WeaponItemData weapon)
            return;

        int level = playerData != null ? playerData.GetWeaponLevel(weapon.itemName) : 1;

        if (weaponDetailLevelText != null)
            weaponDetailLevelText.text = "Level " + level;
    }

    /// <summary>
    /// Clears the details panel, presenting a blank state when navigating to an empty category.
    /// </summary>
    public void ShowEmptyCategoryDetails()
    {
        detailNameText.text = "Empty";

        ShowGeneralDetails(false, null);
        ShowWeaponDetails(false, null);

        if (detailDescriptionText != null)
            detailDescriptionText.text = "No items here.";

        if (detailIconImage != null)
            detailIconImage.color = new Color(1, 1, 1, 0);

        equipButtonObject.SetActive(false);
        currentlySelectedItem = null;
        isItemClickedFromGrid = false;
    }

    #endregion

    #region Actions & HUD Updates

    /// <summary>
    /// Forwards the equip/use action to the specialized EquipHandler.
    /// </summary>
    public void OnEquipButtonClicked()
    {
        equipHandler.Handle(this);
    }

    /// <summary>
    /// Dynamically toggles the visibility of elemental skill HUD overlays 
    /// based on the weapon currently equipped by the player.
    /// </summary>
    /// <param name="weapon">The active weapon data, or null if unarmed.</param>
    public void UpdateSkillHUD(WeaponItemData weapon)
    {
        if (windSwordSkillsGroup != null) windSwordSkillsGroup.SetActive(false);
        if (iceSwordSkillsGroup != null) iceSwordSkillsGroup.SetActive(false);
        if (fireSwordSkillsGroup != null) fireSwordSkillsGroup.SetActive(false);

        if (weapon == null) return;

        switch (weapon.weaponElement)
        {
            case WeaponItemData.WeaponElement.Wind:
                if (windSwordSkillsGroup != null) windSwordSkillsGroup.SetActive(true);
                break;
            case WeaponItemData.WeaponElement.Ice:
                if (iceSwordSkillsGroup != null) iceSwordSkillsGroup.SetActive(true);
                break;
            case WeaponItemData.WeaponElement.Fire:
                if (fireSwordSkillsGroup != null) fireSwordSkillsGroup.SetActive(true);
                break;
        }
    }

    #endregion
}