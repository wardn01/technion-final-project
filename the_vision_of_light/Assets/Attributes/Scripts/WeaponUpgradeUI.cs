using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VisionOfLight.Player;

/// <summary>
/// Manages the weapon and consumable loadout interface within the character setup screen.
/// Handles grid rendering, item equipping, and the weapon ascension/upgrade mechanics.
/// </summary>
public class WeaponUpgradeUI : MonoBehaviour
{
    #region Data Structures

    /// <summary>
    /// Represents a UI slot for a required material during the weapon upgrade process.
    /// </summary>
    [System.Serializable]
    public class UpgradeMaterialSlot
    {
        public GameObject slotObject;
        public Image icon;
        public TextMeshProUGUI amountText;
    }

    #endregion

    #region References & UI Elements

    [Header("Player Data Reference")]
    /// <summary>Reference to the persistent player data to read/write weapon levels.</summary>
    public PlayerData playerData;

    [Header("Top Bar UI")]
    public TextMeshProUGUI topGoldText;
    public ItemData goldCoinData;

    [Header("Tabs UI Colors")]
    public Image weaponsTabIcon;
    public Image potionsTabIcon;
    public Color activeTabColor = Color.white;
    public Color inactiveTabColor = Color.gray;

    [Header("Left Panel - Grid")]
    public Transform slotsParent;
    public GameObject loadoutSlotPrefab;

    [Tooltip("Optional dedicated slot prefab for weapons. Leave empty to reuse loadoutSlotPrefab.")]
    public GameObject weaponLoadoutSlotPrefab;

    [Header("Slot Highlight Colors")]
    public Color selectedSlotColor = Color.white;
    public Color unselectedSlotColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("Right Panel - Details General")]
    public Image detailIcon;
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailDescriptionText;

    [Header("Right Panel - Details Weapon Only")]
    public Image weaponDetailsIconImage;
    public TextMeshProUGUI detailLevelText;
    public TextMeshProUGUI detailStatsText;

    [Header("Right Panel - Ascend Group")]
    public GameObject ascendGroupPanel;
    public Button ascendBtn;
    public UpgradeMaterialSlot[] materialSlots;

    [Header("Action Buttons")]
    public Button equipBtn;
    public TextMeshProUGUI equipBtnText;

    #endregion

    #region State Variables

    private class PooledSlot
    {
        public GameObject Object;
        public bool IsWeaponSlot;
    }

    private Image currentSelectedSlotImage;
    private readonly List<PooledSlot> pool = new List<PooledSlot>();
    private ItemData currentlySelectedItem;
    private bool showingWeapons = true;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Refreshes the UI state whenever this panel is activated.
    /// Ensures data consistency when switching between main menu tabs.
    /// </summary>
    public void OnEnable()
    {
        UpdateTabColors();
        RefreshGrid();
    }

    #endregion

    #region Tab Navigation

    /// <summary>
    /// Switches the active filter to Weapons and triggers a grid rebuild.
    /// </summary>
    public void ShowWeaponsTab()
    {
        showingWeapons = true;
        UpdateTabColors();
        ResetHighlight();
        RefreshGrid();
    }

    /// <summary>
    /// Switches the active filter to Potions/Consumables and triggers a grid rebuild.
    /// </summary>
    public void ShowPotionsTab()
    {
        showingWeapons = false;
        UpdateTabColors();
        ResetHighlight();
        RefreshGrid();
    }

    /// <summary>
    /// Updates the visual state of the tab buttons to reflect the current filter.
    /// </summary>
    private void UpdateTabColors()
    {
        if (weaponsTabIcon != null) weaponsTabIcon.color = showingWeapons ? activeTabColor : inactiveTabColor;
        if (potionsTabIcon != null) potionsTabIcon.color = !showingWeapons ? activeTabColor : inactiveTabColor;
    }

    #endregion

    #region Grid Management

    /// <summary>
    /// Rebuilds the left-side inventory grid using object pooling based on the active tab filter.
    /// Manages the selection state to ensure the details panel displays correct information.
    /// </summary>
    public void RefreshGrid()
    {
        if (InventoryManager.Instance == null) return;

        var inv = InventoryManager.Instance.GetInventory();
        int index = 0;
        ItemData firstValidItem = null;
        bool isSelectedStillValid = false;

        foreach (var kvp in inv)
        {
            ItemData item = kvp.Key;
            int amount = kvp.Value;

            if (showingWeapons && item.type != ItemType.Weapon) continue;
            if (!showingWeapons && item.type != ItemType.Consumable) continue;

            if (firstValidItem == null) firstValidItem = item;
            if (currentlySelectedItem == item && amount > 0) isSelectedStillValid = true;

            GameObject slot = AcquireSlot(index, item);
            slot.GetComponent<InventorySlotUI>()?.Setup(item, amount, HandleSlotClick);

            index++;
        }

        for (int i = index; i < pool.Count; i++)
            pool[i].Object.SetActive(false);

        if (topGoldText != null && goldCoinData != null)
            topGoldText.text = InventoryManager.Instance.GetItemAmount(goldCoinData).ToString("N0");

        if (isSelectedStillValid) DisplayItemDetails(currentlySelectedItem, false);
        else if (firstValidItem != null) DisplayItemDetails(firstValidItem, false);
        else ClearDetails();
    }

    /// <summary>
    /// Callback triggered by the slot prefab when the player clicks it.
    /// </summary>
    private void HandleSlotClick(ItemData clickedItem)
    {
        DisplayItemDetails(clickedItem, true);
    }

    /// <summary>
    /// Returns a pooled slot using the weapon prefab for weapons when configured.
    /// </summary>
    private GameObject AcquireSlot(int index, ItemData item)
    {
        bool useWeaponSlot = item.type == ItemType.Weapon && weaponLoadoutSlotPrefab != null;
        GameObject prefab = useWeaponSlot ? weaponLoadoutSlotPrefab : loadoutSlotPrefab;

        while (pool.Count <= index)
            pool.Add(null);

        PooledSlot entry = pool[index];

        if (entry != null && entry.IsWeaponSlot == useWeaponSlot)
        {
            entry.Object.SetActive(true);
            return entry.Object;
        }

        if (entry?.Object != null)
            Destroy(entry.Object);

        GameObject slot = Instantiate(prefab, slotsParent);
        pool[index] = new PooledSlot
        {
            Object = slot,
            IsWeaponSlot = useWeaponSlot
        };

        return slot;
    }

    #endregion

    #region Item Details & Equipment

    /// <summary>
    /// Populates the right-side panel with the selected item's data.
    /// Toggles visibility of ascension panels and formats stats based on whether the item is a weapon or a consumable.
    /// </summary>
    /// <param name="item">The item to display.</param>
    /// <param name="fromUserClick">Indicates if the selection was a direct player action vs an auto-refresh.</param>
    public void DisplayItemDetails(ItemData item, bool fromUserClick = false)
    {
        if (item == null || playerData == null) return;
        currentlySelectedItem = item;

        // Synchronize state with main inventory UI if applicable
        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.currentlySelectedItem = item;
            InventoryUIManager.Instance.isItemClickedFromGrid = fromUserClick;
        }

        if (detailNameText != null)
            detailNameText.text = item.itemName;

        bool isWeapon = item.type == ItemType.Weapon;
        ShowGeneralDetails(isWeapon, item);
        ShowWeaponDetails(isWeapon, item);

        if (equipBtn != null)
        {
            equipBtn.gameObject.SetActive(true);
            equipBtn.onClick.RemoveAllListeners();
            equipBtn.onClick.AddListener(() => HandleEquip(item));
        }
        if (equipBtnText != null)
            equipBtnText.text = (QuickSlotManager.Instance != null && QuickSlotManager.Instance.IsItemEquipped(item)) ? "Remove" : "Equip";
    }

    /// <summary>
    /// Shows the standard icon and description used for consumables in the loadout panel.
    /// </summary>
    private void ShowGeneralDetails(bool isWeapon, ItemData item)
    {
        if (detailIcon != null)
        {
            detailIcon.gameObject.SetActive(!isWeapon && item != null);

            if (!isWeapon && item != null)
            {
                detailIcon.sprite = item.itemIcon;
                detailIcon.color = Color.white;
            }
        }

        if (detailDescriptionText != null)
        {
            bool showDescription = !isWeapon && item != null;
            detailDescriptionText.gameObject.SetActive(showDescription);

            if (showDescription)
                detailDescriptionText.text = item.description;
        }
    }

    /// <summary>
    /// Shows the dedicated weapon preview, level, upgrade stats, and ascension panel.
    /// </summary>
    private void ShowWeaponDetails(bool isWeapon, ItemData item)
    {
        if (weaponDetailsIconImage != null)
        {
            weaponDetailsIconImage.gameObject.SetActive(isWeapon);

            if (isWeapon)
            {
                weaponDetailsIconImage.sprite = item.itemIcon;
                weaponDetailsIconImage.color = Color.white;
            }
        }

        if (detailLevelText != null)
            detailLevelText.gameObject.SetActive(isWeapon);

        if (detailStatsText != null)
            detailStatsText.gameObject.SetActive(isWeapon);

        if (ascendGroupPanel != null)
            ascendGroupPanel.SetActive(isWeapon);

        if (!isWeapon || item is not WeaponItemData weapon)
            return;

        int currentLvl = playerData.GetWeaponLevel(weapon.itemName);
        bool hasNextUpgrade = weapon.upgradeLevels != null && currentLvl <= weapon.upgradeLevels.Length;

        if (hasNextUpgrade)
        {
            WeaponUpgradeLevel upgradeData = weapon.upgradeLevels[currentLvl - 1];

            if (detailLevelText != null)
                detailLevelText.text = "Level " + currentLvl;

            int currentDamage = weapon.weaponBaseAttack + GetTotalBoostUntil(weapon, currentLvl - 1);
            int nextDamage = currentDamage + upgradeData.damageBoost;

            if (detailStatsText != null)
                detailStatsText.text = $"Damage: {currentDamage} -> <color=#00FF00>{nextDamage}</color>";

            bool isAscensionLocked = currentLvl > playerData.currentAscensionIndex;

            SetupUpgradeUI(upgradeData);

            if (ascendBtn != null)
            {
                ascendBtn.onClick.RemoveAllListeners();
                ascendBtn.onClick.AddListener(() =>
                {
                    if (isAscensionLocked)
                    {
                        if (NotificationManager.Instance != null)
                            NotificationManager.Instance.ShowWarning($"Requires Ascension {currentLvl} to unlock!");
                    }
                    else
                    {
                        UpgradeWeapon(weapon, upgradeData);
                    }
                });
            }
        }
        else
        {
            if (detailLevelText != null)
                detailLevelText.text = $"Level {currentLvl} (MAX)";

            int finalDamage = weapon.weaponBaseAttack + GetTotalBoostUntil(weapon, currentLvl - 1);

            if (detailStatsText != null)
                detailStatsText.text = $"Damage: {finalDamage}";

            ClearMaterialSlots();

            if (ascendBtn != null)
                ascendBtn.interactable = false;
        }
    }

    /// <summary>
    /// Resets the right panel to a default empty state when no items of the current category exist.
    /// </summary>
    private void ClearDetails()
    {
        currentlySelectedItem = null;
        if (InventoryUIManager.Instance != null) 
        { 
            InventoryUIManager.Instance.currentlySelectedItem = null; 
            InventoryUIManager.Instance.isItemClickedFromGrid = false; 
        }
        
        ShowGeneralDetails(false, null);
        ShowWeaponDetails(false, null);

        if (detailIcon != null)
            detailIcon.color = new Color(1, 1, 1, 0);

        if (detailNameText != null)
            detailNameText.text = "Empty";

        if (detailDescriptionText != null)
            detailDescriptionText.text = "No items here.";

        if (equipBtn != null)
            equipBtn.gameObject.SetActive(false);

        ResetHighlight();
    }

    /// <summary>
    /// Interacts with the QuickSlotManager to toggle the equipped state of the provided item.
    /// </summary>
    private void HandleEquip(ItemData item)
    {
        if (QuickSlotManager.Instance == null) return;

        if (QuickSlotManager.Instance.IsItemEquipped(item))
        {
            QuickSlotManager.Instance.ClearItemFromAllSlots(item);
            if (item.type == ItemType.Weapon)
            {
                PlayerCombat combat = PlayerRegistry.Instance?.Combat;
                combat?.UnequipCurrentWeapon();
            }
        }
        else
        {
            QuickSlotManager.Instance.AssignToFirstEmptySlot(item);
        }
        
        DisplayItemDetails(item, false);
    }

    #endregion

    #region Visual Highlighting

    /// <summary>
    /// Updates the background color of the clicked slot to indicate selection.
    /// </summary>
    public void HighlightSlot(Image clickedBgImage)
    {
        if (currentSelectedSlotImage != null) currentSelectedSlotImage.color = unselectedSlotColor;
        currentSelectedSlotImage = clickedBgImage;
        if (currentSelectedSlotImage != null) currentSelectedSlotImage.color = selectedSlotColor;
    }

    /// <summary>
    /// Clears the visual selection state from the currently active slot.
    /// </summary>
    public void ResetHighlight()
    {
        if (currentSelectedSlotImage != null) 
        { 
            currentSelectedSlotImage.color = unselectedSlotColor; 
            currentSelectedSlotImage = null; 
        }
    }

    #endregion

    #region Weapon Ascension Logic

    /// <summary>
    /// Configures the material requirement UI slots based on the target upgrade level.
    /// </summary>
    private void SetupUpgradeUI(WeaponUpgradeLevel data)
    {
        foreach (var slot in materialSlots) slot.slotObject.SetActive(false);
        if (data.materials == null) return;

        int uiSlotIndex = 0;
        for (int i = 0; i < data.materials.Length; i++)
        {
            if (data.materials[i].item != null && uiSlotIndex < materialSlots.Length)
            {
                UpdateSlotUI(uiSlotIndex, data.materials[i].item, data.materials[i].amount);
                uiSlotIndex++;
            }
        }
        
        CheckCanUpgrade(data);
    }

    /// <summary>
    /// Populates a single material slot with icon and quantity data.
    /// Applies green/red text formatting based on player inventory amounts.
    /// </summary>
    private void UpdateSlotUI(int index, ItemData item, int requiredAmount)
    {
        if (item == null) return;
        materialSlots[index].slotObject.SetActive(true);
        materialSlots[index].icon.sprite = item.itemIcon;
        
        int currentAmount = InventoryManager.Instance != null ? InventoryManager.Instance.GetItemAmount(item) : 0;
        string colorTag = currentAmount >= requiredAmount ? "<color=green>" : "<color=red>";
        materialSlots[index].amountText.text = $"{colorTag}{currentAmount}</color>/{requiredAmount}";
    }

    /// <summary>
    /// Evaluates whether the player has sufficient materials to perform the upgrade,
    /// enabling or disabling the ascend button accordingly.
    /// </summary>
    private void CheckCanUpgrade(WeaponUpgradeLevel data)
    {
        bool hasMaterials = true;
        if (data.materials != null && InventoryManager.Instance != null)
        {
            foreach (var m in data.materials)
            {
                if (m.item != null && InventoryManager.Instance.GetItemAmount(m.item) < m.amount) 
                    hasMaterials = false;
            }
        }
        if (ascendBtn != null) ascendBtn.interactable = hasMaterials;
    }

    /// <summary>
    /// Executes the weapon upgrade sequence: deducts materials, signals the data layer, and refreshes the UI.
    /// </summary>
    private void UpgradeWeapon(WeaponItemData weapon, WeaponUpgradeLevel data)
    {
        if (data.materials != null && InventoryManager.Instance != null)
        {
            foreach (var m in data.materials)
            {
                if (m.item != null) 
                    InventoryManager.Instance.RemoveItem(m.item, m.amount);
            }
        }

        if (playerData != null) playerData.LevelUpWeapon(weapon.itemName);
        RefreshGrid();
    }

    /// <summary>
    /// Calculates the cumulative damage boost from level 1 up to the current level index.
    /// </summary>
    private int GetTotalBoostUntil(WeaponItemData weapon, int levelIndex)
    {
        if (weapon == null || weapon.upgradeLevels == null) return 0;

        int total = 0;
        for (int i = 0; i < levelIndex; i++)
        {
            if (i < weapon.upgradeLevels.Length) 
                total += weapon.upgradeLevels[i].damageBoost;
        }
        return total;
    }

    /// <summary>
    /// Hides all material requirement slots. Used when a weapon is fully upgraded.
    /// </summary>
    private void ClearMaterialSlots()
    {
        foreach (var slot in materialSlots) slot.slotObject.SetActive(false);
    }

    #endregion
}