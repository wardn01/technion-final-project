using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class QuickSlotManager : MonoBehaviour
{
    public static QuickSlotManager Instance { get; private set; }

    [Header("References")]
    public PlayerCombat playerCombat;
    public WeaponUpgradeUI weaponUI;

    [Header("Slots Data")]
    public ItemData[] slots = new ItemData[4];

    [Header("UI Icons")]
    public Image[] slotIcons = new Image[4];

    [Header("UI Amount Texts")]
    public TextMeshProUGUI[] slotTexts = new TextMeshProUGUI[4];

    [Header("UI Cooldowns")]
    public Image[] cooldownOverlays = new Image[4];
    public TextMeshProUGUI[] cooldownTexts = new TextMeshProUGUI[4];

    private int selectedSlotIndex = -1;

    private PlayerHealth playerHp;
    private InventoryUIManager inventoryUI;

    private Dictionary<string, float> cooldownTimers = new Dictionary<string, float>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        playerHp = FindAnyObjectByType<PlayerHealth>();
        inventoryUI = InventoryUIManager.Instance;

        if (weaponUI == null)
            weaponUI = FindAnyObjectByType<WeaponUpgradeUI>();

        UpdateUI();
    }

    private void Update()
    {
        UpdateCooldownUI();
    }

    private void UpdateCooldownUI()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (cooldownOverlays[i] == null || cooldownTexts[i] == null)
            {
                continue;
            }

            ItemData item = slots[i];

            if (item is ConsumableItemData cons)
            {
                string id = "SharedPotionCD";

                if (cooldownTimers.ContainsKey(id) && cooldownTimers[id] > Time.time)
                {
                    float remaining = cooldownTimers[id] - Time.time;
                    float totalCooldown = cooldownTimers.TryGetValue("LastCooldownUsed", out float lastCooldown) && lastCooldown > 0f ? lastCooldown : 1f;

                    cooldownOverlays[i].gameObject.SetActive(true);
                    cooldownOverlays[i].fillAmount = remaining / totalCooldown;
                    cooldownTexts[i].text = Mathf.CeilToInt(remaining).ToString();
                }
                else
                {
                    cooldownOverlays[i].gameObject.SetActive(false);
                    cooldownTexts[i].text = "";
                }
            }
            else
            {
                cooldownOverlays[i].gameObject.SetActive(false);
                cooldownTexts[i].text = "";
            }
        }
    }

    public void OnQuickSlotClicked(int index)
    {
        if (selectedSlotIndex != -1)
        {
            if (selectedSlotIndex == index)
            {
                selectedSlotIndex = -1;
                UpdateUI();
                return;
            }

            ItemData temp = slots[index];
            slots[index] = slots[selectedSlotIndex];
            slots[selectedSlotIndex] = temp;

            selectedSlotIndex = -1;
            UpdateUI();
            inventoryUI?.RefreshUI();
            return;
        }

        bool isInventoryOpen = inventoryUI != null && inventoryUI.inventoryWindow.activeSelf;
        bool isWeaponPageOpen = weaponUI != null && weaponUI.gameObject.activeInHierarchy;

        if ((isInventoryOpen || isWeaponPageOpen) &&
            inventoryUI.currentlySelectedItem != null &&
            inventoryUI.isItemClickedFromGrid)
        {
            ItemData invItem = inventoryUI.currentlySelectedItem;

            if (invItem.type != ItemType.Weapon && invItem.type != ItemType.Consumable)
            {
                inventoryUI.isItemClickedFromGrid = false;
                return;
            }

            AssignItem(invItem, index);

            if (invItem is WeaponItemData weaponData)
            {
                inventoryUI.UpdateSkillHUD(weaponData);
            }

            inventoryUI.isItemClickedFromGrid = false;
            UpdateUI();
            inventoryUI.RefreshUI();

            if (isWeaponPageOpen)
                weaponUI.RefreshGrid();

            return;
        }

        if (slots[index] != null)
        {
            selectedSlotIndex = index;
            UpdateUI();
        }
    }

    private bool IsAnyMenuOpen()
    {
        bool invOpen = inventoryUI != null && inventoryUI.inventoryWindow.activeInHierarchy;
        bool weaponOpen = weaponUI != null && weaponUI.gameObject.activeInHierarchy;
        return invOpen || weaponOpen;
    }

    public void AssignItem(ItemData item, int slotIndex)
    {
        if (item == null)
            return;

        if (item is WeaponItemData)
        {
            int currentWeaponCount = CountWeapons();
            ItemData targetSlotItem = slots[slotIndex];
            bool isAlreadyInSlots = IsItemEquipped(item);

            if (!isAlreadyInSlots && currentWeaponCount >= 2 && !(targetSlotItem is WeaponItemData))
            {
                return;
            }
        }

        int oldIndex = System.Array.IndexOf(slots, item);
        ItemData existingItem = slots[slotIndex];

        if (oldIndex != -1 && oldIndex != slotIndex)
        {
            slots[oldIndex] = existingItem;
        }

        slots[slotIndex] = item;
        UpdateUI();
    }

    public void ExecuteSlotAction(int index)
    {
        ItemData item = slots[index];

        if (item == null)
            return;

        if (item is WeaponItemData weapon)
        {
            if (playerCombat != null && playerCombat.IsSafeToEquip())
            {
                playerCombat.EquipWeapon(weapon);
                inventoryUI?.UpdateSkillHUD(weapon);
            }
        }
        else if (item is ConsumableItemData cons)
        {
            string id = "SharedPotionCD";

            if (cooldownTimers.ContainsKey(id) && cooldownTimers[id] > Time.time)
            {
                return;
            }

            int amount = InventoryManager.Instance.GetItemAmount(item);

            if (amount > 0)
            {
                if (playerHp != null && playerHp.currentHealth < playerHp.maxHealth)
                {
                    playerHp.HealPlayer(
                        cons.instantHeal,
                        cons.tickHealAmount,
                        cons.tickInterval,
                        cons.totalTicks
                    );

                    cooldownTimers[id] = Time.time + cons.cooldownTime;
                    cooldownTimers["LastCooldownUsed"] = cons.cooldownTime;

                    InventoryManager.Instance.RemoveItem(item, 1);

                    if (InventoryManager.Instance.GetItemAmount(item) <= 0)
                    {
                        ClearItemFromAllSlots(item);
                    }

                    UpdateUI();
                    inventoryUI?.RefreshUI();
                }
            }
            else
            {
                ClearItemFromAllSlots(item);
                UpdateUI();
            }
        }
    }

    public void UpdateUI()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            SetupSlot(slots[i], slotIcons[i], slotTexts[i], i);
        }

        UpdateCooldownUI();
    }

    private void SetupSlot(ItemData item, Image icon, TextMeshProUGUI text, int index)
    {
        if (item != null)
        {
            icon.sprite = item.itemIcon;
            icon.color = index == selectedSlotIndex ? Color.gray : Color.white;

            if (text != null)
            {
                int amt = InventoryManager.Instance.GetItemAmount(item);

                if (item is WeaponItemData)
                    text.text = "";
                else
                    text.text = amt > 0 ? "x" + amt : "";
            }
        }
        else
        {
            icon.color = index == selectedSlotIndex ? new Color(0.5f, 0.5f, 0.5f, 0.5f) : new Color(1, 1, 1, 0);

            if (text != null)
                text.text = "";
        }
    }

    public void AssignToFirstEmptySlot(ItemData item)
    {
        if (IsItemEquipped(item))
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                AssignItem(item, i);
                return;
            }
        }

        AssignItem(item, 0);
    }

    public void ClearItemFromAllSlots(ItemData item)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == item)
                slots[i] = null;
        }

        UpdateUI();
    }

    public bool IsItemEquipped(ItemData item)
    {
        foreach (var s in slots)
        {
            if (s == item)
                return true;
        }

        return false;
    }

    public void ResetSelection()
    {
        selectedSlotIndex = -1;
        UpdateUI();
    }

    private int CountWeapons()
    {
        int count = 0;

        foreach (var item in slots)
        {
            if (item is WeaponItemData)
                count++;
        }

        return count;
    }
}