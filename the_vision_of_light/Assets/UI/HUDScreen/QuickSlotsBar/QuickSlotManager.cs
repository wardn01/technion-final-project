using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuickSlotManager : MonoBehaviour
{
    public static QuickSlotManager Instance { get; private set; }

    [Header("References")]
    public PlayerCombat playerCombat;

    [Header("Slots Data")]
    public ItemData[] slots = new ItemData[4];

    [Header("UI Icons")]
    public Image[] slotIcons = new Image[4];

    [Header("UI Amount Texts")]
    public TextMeshProUGUI[] slotTexts = new TextMeshProUGUI[4];

    private PlayerHealth playerHp;
    private InventoryUIManager inventoryUI;

    private int selectedSlotIndex = -1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        playerHp = FindFirstObjectByType<PlayerHealth>();
        inventoryUI = FindFirstObjectByType<InventoryUIManager>();
        UpdateUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ExecuteSlotAction(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ExecuteSlotAction(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ExecuteSlotAction(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ExecuteSlotAction(3);
    }

    public void OnQuickSlotClicked(int index)
    {
        if (selectedSlotIndex == -1)
        {
            if (slots[index] == null) return;

            selectedSlotIndex = index;
            UpdateUI();
            return;
        }

        if (selectedSlotIndex == index)
        {
            slots[index] = null;
            selectedSlotIndex = -1;
            UpdateUI();
            return;
        }

        ItemData temp = slots[index];
        slots[index] = slots[selectedSlotIndex];
        slots[selectedSlotIndex] = temp;

        selectedSlotIndex = -1;
        UpdateUI();
    }

    public void AssignItem(ItemData item, int slotIndex)
    {
        if (item == null) return;

        int oldIndex = System.Array.IndexOf(slots, item);
        ItemData existingItem = slots[slotIndex];

        if (oldIndex != -1 && oldIndex != slotIndex)
        {
            slots[oldIndex] = existingItem;
        }

        slots[slotIndex] = item;
        UpdateUI();
    }

    private void ExecuteSlotAction(int index)
    {
        ItemData item = slots[index];
        if (item == null) return;

        if (item is WeaponItemData weapon)
        {
            playerCombat?.EquipWeapon(weapon);
        }
        else if (item is ConsumableItemData cons)
        {
            int amount = InventoryManager.Instance.GetItemAmount(item);

            if (amount > 0)
            {
                if (playerHp != null && playerHp.currentHealth < playerHp.maxHealth)
                {
                    playerHp.HealPlayer(cons.healAmount);
                    InventoryManager.Instance.AddItem(item, -1);

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
    }

    private void SetupSlot(ItemData item, Image icon, TextMeshProUGUI text, int index)
    {
        if (item != null)
        {
            icon.sprite = item.itemIcon;

            // 🔥 Highlight
            icon.color = (index == selectedSlotIndex) ? Color.gray : Color.white;

            if (text != null)
            {
                int amt = InventoryManager.Instance.GetItemAmount(item);
                text.text = (item is WeaponItemData) ? "" : (amt > 0 ? "x" + amt : "");
            }
        }
        else
        {
            icon.color = new Color(1, 1, 1, 0);
            if (text != null) text.text = "";
        }
    }

    public void AssignToFirstEmptySlot(ItemData item)
    {
        if (IsItemEquipped(item)) return;

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
            if (s == item) return true;
        }
        return false;
    }
}