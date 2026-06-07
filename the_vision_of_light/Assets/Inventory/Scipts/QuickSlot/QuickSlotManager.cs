using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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
        playerHp = FindAnyObjectByType<PlayerHealth>();
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
            SetupSlot(slots[i], slotIcons[i], slotTexts[i], i);
        }

        UpdateCooldownUI();
    }

    /// <summary>
    /// Maps the underlying ItemData to the specific UI elements of a slot.
    /// Handles visual cues like selection highlights and dynamic stack counters.
    /// </summary>
    private void SetupSlot(ItemData item, Image icon, TextMeshProUGUI text, int index)
    {
        if (item != null)
        {
            icon.sprite = item.itemIcon;

            // Apply a gray tint if this slot is currently selected for swapping
            icon.color = index == selectedSlotIndex ? Color.gray : Color.white;

            if (text != null)
            {
                int amt = InventoryManager.Instance.GetItemAmount(item);

                // Hide quantity text for weapons or empty stacks
                text.text = (item is WeaponItemData || amt <= 0) ? "" : "x" + amt;
            }
        }
        else
        {
            // Apply semi-transparent gray if an empty slot is selected, else fully transparent
            icon.color = index == selectedSlotIndex ? new Color(0.5f, 0.5f, 0.5f, 0.5f) : new Color(1, 1, 1, 0);

            if (text != null) text.text = "";
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
        // Reject a 3rd weapon unless this slot already holds a weapon to replace.
        if (item is WeaponItemData && !IsItemEquipped(item)
            && CountWeapons() >= 2 && !(slots[index] is WeaponItemData))
        {
            NotificationManager.Instance?.ShowWarning("You can only equip 2 weapons!");
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

    /// <summary>
    /// Assigns the item to the first empty slot. If the bar is full, nothing happens;
    /// the player is expected to drop the item onto a specific slot to swap it instead.
    /// </summary>
    /// <param name="item">The item to bind to the hotbar.</param>
    public void AssignToFirstEmptySlot(ItemData item)
    {
        if (item == null || IsItemEquipped(item)) return;

        int emptyIndex = -1;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) { emptyIndex = i; break; }
        }

        // Bar is full: placement is handled by clicking a specific quick slot.
        if (emptyIndex == -1) return;

        if (item is WeaponItemData && CountWeapons() >= 2)
        {
            NotificationManager.Instance?.ShowWarning("You can only equip 2 weapons!");
            return;
        }

        AssignItem(item, emptyIndex);
    }

    /// <summary>
    /// Directly assigns an item to a specific slot index.
    /// Enforces gameplay rules, such as limiting the player to a maximum of 2 equipped weapons.
    /// </summary>
    public void AssignItem(ItemData item, int slotIndex)
    {
        if (item == null) return;

        // Weapon Limit Validation: Prevent equipping more than 2 weapons total
        if (item is WeaponItemData)
        {
            int currentWeaponCount = CountWeapons();
            ItemData targetSlotItem = slots[slotIndex];
            bool isAlreadyInSlots = IsItemEquipped(item);

            if (!isAlreadyInSlots && currentWeaponCount >= 2 && !(targetSlotItem is WeaponItemData))
            {
                // Abort assignment if trying to add a 3rd weapon into a non-weapon slot
                return;
            }
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