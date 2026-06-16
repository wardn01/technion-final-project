using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using VisionOfLight.Player;

/// <summary>
/// The central controller for the player's Quick Access Bar (Hotbar).
/// Manages the binding of inventory items to actionable slots, handles 
/// item swapping/selection logic, and bridges the UI with core gameplay 
/// systems like Combat (equipping) and Health (consuming with cooldowns).
/// </summary>
public class QuickSlotManager : MonoBehaviour
{
    #region Singleton

    /// <summary>
    /// Global singleton instance ensuring consistent access to the quick slot state 
    /// across UI interactions and keyboard inputs.
    /// </summary>
    public static QuickSlotManager Instance { get; private set; }

    #endregion

    #region References & UI

    [Header("References")]
    
    /// <summary>
    /// Reference to the player's combat controller to trigger weapon equips.
    /// </summary>
    public PlayerCombat playerCombat;

    [Header("Slots Data")]
    
    /// <summary>
    /// The underlying data array representing the 4 active quick slots.
    /// Null entries represent empty slots.
    /// </summary>
    public ItemData[] slots = new ItemData[4];

    [Header("UI Icons")]
    public Image[] slotIcons = new Image[4];

    [Header("UI Weapon Icons")]
    [Tooltip("Dedicated larger/tilted weapon icons. Leave empty to reuse slotIcons.")]
    public Image[] weaponSlotIcons = new Image[4];

    [Header("UI Amount Texts")]
    public TextMeshProUGUI[] slotTexts = new TextMeshProUGUI[4];

    [Header("UI Cooldowns")]
    public Image[] cooldownOverlays = new Image[4];
    public TextMeshProUGUI[] cooldownTexts = new TextMeshProUGUI[4];

    /// <summary>
    /// Tracks the index of the currently highlighted slot for swapping purposes.
    /// A value of -1 indicates no slot is currently selected.
    /// </summary>
    [HideInInspector]
    public int selectedSlotIndex { get; private set; } = -1;

    /// <summary>
    /// Cached reference to the player's health component to execute healing logic.
    /// Cached to avoid expensive GameObject.Find calls during combat.
    /// </summary>
    private PlayerHealth playerHp;

    /// <summary>
    /// Dictionary tracking global cooldowns (e.g., "SharedPotionCD") using Time.time timestamps.
    /// </summary>
    private Dictionary<string, float> cooldownTimers = new Dictionary<string, float>();

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // Cache the player health component dynamically on startup
        playerHp = PlayerRegistry.Instance?.Health;
        UpdateUI();
    }

    /// <summary>
    /// Continuously evaluates and updates the visual representation of active cooldowns.
    /// </summary>
    private void Update()
    {
        UpdateCooldownUI();
    }

    #endregion

    #region Visuals & Cooldowns

    /// <summary>
    /// Calculates remaining cooldown times for consumable items and updates 
    /// the radial fill overlays and countdown text.
    /// </summary>
    private void UpdateCooldownUI()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (cooldownOverlays[i] == null || cooldownTexts[i] == null)
                continue;

            ItemData item = slots[i];

            if (item is ConsumableItemData cons)
            {
                string id = "SharedPotionCD"; // Uses a shared cooldown ID to prevent potion spamming

                if (cooldownTimers.ContainsKey(id) && cooldownTimers[id] > Time.time)
                {
                    float remaining = cooldownTimers[id] - Time.time;
                    float totalCooldown = cooldownTimers.TryGetValue("LastCooldownUsed", out float lastCooldown) && lastCooldown > 0f 
                        ? lastCooldown 
                        : 1f;

                    // Activate and scale the radial cooldown mask
                    cooldownOverlays[i].gameObject.SetActive(true);
                    cooldownOverlays[i].fillAmount = remaining / totalCooldown;

                    cooldownTexts[i].text = Mathf.CeilToInt(remaining).ToString();
                }
                else
                {
                    // Cooldown finished, hide UI
                    cooldownOverlays[i].gameObject.SetActive(false);
                    cooldownTexts[i].text = "";
                }
            }
            else
            {
                // Not a consumable, ensure cooldown UI is hidden
                cooldownOverlays[i].gameObject.SetActive(false);
                cooldownTexts[i].text = "";
            }
        }
    }

    /// <summary>
    /// Forces a complete visual rebuild of all 4 quick slots based on current data.
    /// </summary>
    public void UpdateUI()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            Image weaponIcon = i < weaponSlotIcons.Length ? weaponSlotIcons[i] : null;
            SetupSlot(slots[i], slotIcons[i], weaponIcon, slotTexts[i], i);
        }

        UpdateCooldownUI();
    }

    /// <summary>
    /// Maps the underlying ItemData to the specific UI elements of a slot.
    /// Uses a dedicated weapon icon when configured, otherwise falls back to the standard icon.
    /// </summary>
    private void SetupSlot(ItemData item, Image icon, Image weaponIcon, TextMeshProUGUI text, int index)
    {
        bool isWeapon = item != null && item.type == ItemType.Weapon;
        bool useWeaponIcon = isWeapon && weaponIcon != null;
        Color tint = index == selectedSlotIndex ? Color.gray : Color.white;

        if (icon != null)
        {
            bool showStandardIcon = item != null && !useWeaponIcon;
            icon.gameObject.SetActive(showStandardIcon || item == null);

            if (showStandardIcon)
            {
                icon.sprite = item.itemIcon;
                icon.color = tint;
            }
            else if (item == null)
            {
                icon.color = index == selectedSlotIndex
                    ? new Color(0.5f, 0.5f, 0.5f, 0.5f)
                    : new Color(1, 1, 1, 0);
            }
        }

        if (weaponIcon != null)
        {
            weaponIcon.gameObject.SetActive(useWeaponIcon);

            if (useWeaponIcon)
            {
                weaponIcon.sprite = item.itemIcon;
                weaponIcon.color = tint;
            }
        }
        else if (isWeapon && icon != null)
        {
            icon.gameObject.SetActive(true);
            icon.sprite = item.itemIcon;
            icon.color = tint;
        }

        if (text != null)
        {
            if (item == null)
            {
                text.text = "";
                return;
            }

            int amt = InventoryManager.Instance.GetItemAmount(item);
            text.text = (item is WeaponItemData || amt <= 0) ? "" : "x" + amt;
        }
    }

    #endregion

    #region Slot Selection & Swapping

    /// <summary>
    /// Implements a two-click swap mechanic. The first click selects a slot, 
    /// the second click swaps the contents of the selected slot with the target slot.
    /// Also dynamically updates the side details panel of the currently active menu.
    /// </summary>
    /// <param name="index">The index of the slot clicked by the user.</param>
    public void OnQuickSlotClicked(int index)
    {
        // Placement mode: an item was actively picked from an open inventory/weapons grid.
        // Clicking a quick slot drops it there, swapping out whatever currently occupies it.
        ItemData pendingItem = GetPendingInventoryItem();
        if (pendingItem != null)
        {
            PlaceItemInSlot(pendingItem, index);
            return;
        }

        // If a slot is already selected, attempt to swap
        if (selectedSlotIndex != -1)
        {
            if (selectedSlotIndex == index)
            {
                // Clicking the same slot twice cancels the selection
                selectedSlotIndex = -1;
                UpdateUI();
                return;
            }

            // Perform the swap
            ItemData temp = slots[index];
            slots[index] = slots[selectedSlotIndex];
            slots[selectedSlotIndex] = temp;

            selectedSlotIndex = -1; // Reset selection state
            UpdateUI();

            // Notify the active UIs to refresh their grid/details state
            if (InventoryUIManager.Instance != null && InventoryUIManager.Instance.inventoryWindow.activeSelf)
            {
                InventoryUIManager.Instance.RefreshUI();
            }
            if (CharacterMenuController.Instance != null && CharacterMenuController.Instance.attributesScreen.activeSelf)
            {
                CharacterMenuController.Instance.weaponsSubPanel.GetComponent<WeaponUpgradeUI>()?.RefreshGrid();
            }

            return;
        }

        // If no slot is selected, select the clicked slot (if it contains an item)
        if (slots[index] != null)
        {
            selectedSlotIndex = index;
            UpdateUI();

            // Update the side details panel in the Inventory if it's open
            if (InventoryUIManager.Instance != null && InventoryUIManager.Instance.inventoryWindow.activeSelf)
            {
                InventoryUIManager.Instance.DisplayItemDetails(slots[index], false);
            }
            
            // Update the side details panel in the Weapons UI if it's open
            if (CharacterMenuController.Instance != null && CharacterMenuController.Instance.attributesScreen.activeSelf)
            {
                CharacterMenuController.Instance.weaponsSubPanel.GetComponent<WeaponUpgradeUI>()?.DisplayItemDetails(slots[index], false);
            }
        }
    }

    /// <summary>
    /// Returns the item the player actively selected from an open inventory/weapons grid,
    /// or null if the player isn't currently in "place an item" mode.
    /// </summary>
    private ItemData GetPendingInventoryItem()
    {
        if (InventoryUIManager.Instance == null) return null;

        // Only treat it as placement when the player explicitly clicked a grid item
        // (auto-selected items on refresh have this flag set to false).
        if (!InventoryUIManager.Instance.isItemClickedFromGrid) return null;

        bool inventoryOpen = InventoryUIManager.Instance.inventoryWindow != null
                             && InventoryUIManager.Instance.inventoryWindow.activeSelf;
        bool weaponsOpen = CharacterMenuController.Instance != null
                           && CharacterMenuController.Instance.attributesScreen != null
                           && CharacterMenuController.Instance.attributesScreen.activeSelf;

        if (!inventoryOpen && !weaponsOpen) return null;

        ItemData item = InventoryUIManager.Instance.currentlySelectedItem;

        // Only weapons and consumables belong on the quick bar.
        if (item == null || (item.type != ItemType.Weapon && item.type != ItemType.Consumable))
            return null;

        return item;
    }

    /// <summary>
    /// Drops a grid-selected item into a specific quick slot, swapping out its current
    /// occupant. Enforces the 2-weapon cap with a warning when it would be exceeded.
    /// </summary>
    private void PlaceItemInSlot(ItemData item, int index)
    {
        if (WouldExceedWeaponLimit(item, index))
        {
            ShowQuickSlotWarning(MaxWeaponsWarning);
            return;
        }

        AssignItem(item, index);
        selectedSlotIndex = -1;

        // The placement consumed the active selection; return to normal slot behavior.
        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.isItemClickedFromGrid = false;

            if (InventoryUIManager.Instance.inventoryWindow != null
                && InventoryUIManager.Instance.inventoryWindow.activeSelf)
            {
                InventoryUIManager.Instance.RefreshUI();
            }
        }

        if (CharacterMenuController.Instance != null
            && CharacterMenuController.Instance.attributesScreen != null
            && CharacterMenuController.Instance.attributesScreen.activeSelf)
        {
            CharacterMenuController.Instance.weaponsSubPanel.GetComponent<WeaponUpgradeUI>()?.RefreshGrid();
        }
    }

    /// <summary>
    /// Clears any pending slot selection/swap state.
    /// </summary>
    public void ResetSelection()
    {
        selectedSlotIndex = -1;
        UpdateUI();
    }

    #endregion

    #region Slot Assignment

    private const int MaxQuickSlotWeapons = 2;
    private const string MaxWeaponsWarning = "You can only equip 2 weapons!";
    private const string QuickBarFullWarning = "Quick bar is full. Click a slot to replace an item.";

    /// <summary>
    /// Assigns the item to the first empty slot.
    /// Shows warnings when the 2-weapon cap or a full quick bar blocks placement.
    /// </summary>
    /// <param name="item">The item to bind to the hotbar.</param>
    /// <returns>True when the item was placed successfully.</returns>
    public bool TryAssignToFirstEmptySlot(ItemData item)
    {
        if (item == null || IsItemEquipped(item))
            return false;

        if (WouldExceedWeaponLimit(item, -1))
        {
            ShowQuickSlotWarning(MaxWeaponsWarning);
            return false;
        }

        int emptyIndex = -1;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                emptyIndex = i;
                break;
            }
        }

        if (emptyIndex == -1)
        {
            ShowQuickSlotWarning(QuickBarFullWarning);
            return false;
        }

        AssignItem(item, emptyIndex);
        return true;
    }

    /// <summary>
    /// Backward-compatible wrapper around <see cref="TryAssignToFirstEmptySlot"/>.
    /// </summary>
    public void AssignToFirstEmptySlot(ItemData item)
    {
        TryAssignToFirstEmptySlot(item);
    }

    /// <summary>
    /// Directly assigns an item to a specific slot index.
    /// Enforces gameplay rules, such as limiting the player to a maximum of 2 equipped weapons.
    /// </summary>
    public void AssignItem(ItemData item, int slotIndex)
    {
        if (item == null) return;

        if (WouldExceedWeaponLimit(item, slotIndex))
        {
            ShowQuickSlotWarning(MaxWeaponsWarning);
            return;
        }

        // If the item is already somewhere else on the hotbar, swap them to prevent duplicates
        int oldIndex = System.Array.IndexOf(slots, item);
        ItemData existingItem = slots[slotIndex];

        if (oldIndex != -1 && oldIndex != slotIndex)
        {
            slots[oldIndex] = existingItem;
        }

        slots[slotIndex] = item;
        UpdateUI();
    }

    /// <summary>
    /// Purges all instances of a specific item from the hotbar.
    /// Usually called when an item is fully consumed, dropped, or unequipped.
    /// </summary>
    public void ClearItemFromAllSlots(ItemData item)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == item)
                slots[i] = null;
        }
        UpdateUI();
    }

    /// <summary>
    /// Checks if a specific item is currently bound anywhere on the hotbar.
    /// </summary>
    public bool IsItemEquipped(ItemData item)
    {
        foreach (var s in slots)
        {
            if (s == item) return true;
        }
        return false;
    }

    /// <summary>
    /// Utility method to count how many weapons are currently bound to the hotbar.
    /// </summary>
    private int CountWeapons()
    {
        int count = 0;
        foreach (var item in slots)
        {
            if (item is WeaponItemData) count++;
        }
        return count;
    }

    /// <summary>
    /// Returns true when placing this weapon would exceed the quick bar weapon cap.
    /// A target slot that already contains a weapon is treated as a replacement, not a third weapon.
    /// </summary>
    private bool WouldExceedWeaponLimit(ItemData item, int targetSlotIndex)
    {
        if (item is not WeaponItemData || IsItemEquipped(item))
            return false;

        if (CountWeapons() < MaxQuickSlotWeapons)
            return false;

        if (targetSlotIndex < 0 || targetSlotIndex >= slots.Length)
            return true;

        return slots[targetSlotIndex] is not WeaponItemData;
    }

    private void ShowQuickSlotWarning(string message)
    {
        if (NotificationManager.Instance != null)
            NotificationManager.Instance.ShowWarning(message);
    }

    #endregion

    #region Gameplay Execution

    /// <summary>
    /// The core execution hook triggered by keyboard inputs (1, 2, 3, 4).
    /// Routes the execution logic based on the item type (Equip Weapon vs. Consume Potion).
    /// </summary>
    /// <param name="index">The zero-based index of the slot to execute.</param>
    public void ExecuteSlotAction(int index)
    {
        ItemData item = slots[index];
        if (item == null) return;

        // Ensure PlayerHealth reference is valid
        if (playerHp == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerHp = p.GetComponent<PlayerHealth>();
        }

        // --- Weapon Execution Logic ---
        if (item is WeaponItemData weapon)
        {
            if (playerCombat != null && playerCombat.IsSafeToEquip())
            {
                playerCombat.EquipWeapon(weapon);

                if (InventoryUIManager.Instance != null)
                {
                    InventoryUIManager.Instance.UpdateSkillHUD(weapon);
                }
            }
        }
        // --- Consumable Execution Logic ---
        else if (item is ConsumableItemData cons)
        {
            string id = "SharedPotionCD";

            // Enforce Global Cooldown
            if (cooldownTimers.ContainsKey(id) && cooldownTimers[id] > Time.time)
                return;

            int amount = InventoryManager.Instance.GetItemAmount(item);

            if (amount > 0)
            {
                if (playerHp != null)
                {
                    // Calculate max HP dynamically from PlayerData if available
                    float currentMaxHp = playerHp.playerData != null 
                        ? playerHp.playerData.GetTotalMaxHealth() 
                        : playerHp.maxHealth;

                    // Only consume if health is not already full
                    if (playerHp.currentHealth < currentMaxHp)
                    {
                        playerHp.HealPlayer(
                            cons.instantHeal,
                            cons.tickHealAmount,
                            cons.tickInterval,
                            cons.totalTicks);

                        // Apply Cooldown timestamps
                        cooldownTimers[id] = Time.time + cons.cooldownTime;
                        cooldownTimers["LastCooldownUsed"] = cons.cooldownTime;

                        // Deduct item from inventory
                        InventoryManager.Instance.RemoveItem(item, 1);

                        // Clean up slot if stack reached zero
                        if (InventoryManager.Instance.GetItemAmount(item) <= 0)
                        {
                            ClearItemFromAllSlots(item);
                        }

                        UpdateUI();

                        // Sync with Inventory UI if it happens to be open
                        if (InventoryUIManager.Instance != null && InventoryUIManager.Instance.inventoryWindow.activeSelf)
                        {
                            InventoryUIManager.Instance.RefreshUI();
                        }
                        
                        // Sync with Weapons UI if it happens to be open
                        if (CharacterMenuController.Instance != null && CharacterMenuController.Instance.attributesScreen.activeSelf)
                        {
                            CharacterMenuController.Instance.weaponsSubPanel.GetComponent<WeaponUpgradeUI>()?.RefreshGrid();
                        }
                    }
                    else
                    {
                        // Notify player that healing is redundant
                        if (NotificationManager.Instance != null)
                        {
                            NotificationManager.Instance.ShowWarning("Your health is already full!");
                        }
                    }
                }
            }
            else
            {
                // Fallback: If amount is 0 but it was still in the slot, clear it
                ClearItemFromAllSlots(item);
                UpdateUI();
            }
        }
    }

    #endregion
}