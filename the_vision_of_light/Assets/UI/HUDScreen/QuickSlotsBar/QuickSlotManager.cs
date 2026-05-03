using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuickSlotManager : MonoBehaviour
{
    public static QuickSlotManager Instance { get; private set; }

    [Header("References")]
    public PlayerCombat playerCombat;

    [Header("Slots Data")]
    public ItemData slot1;
    public ItemData slot2;
    public ItemData slot3;
    public ItemData slot4;

    [Header("UI Icons")]
    public Image slot1Icon;
    public Image slot2Icon;
    public Image slot3Icon;
    public Image slot4Icon;

    [Header("UI Amount Texts")]
    public TextMeshProUGUI slot1Text;
    public TextMeshProUGUI slot2Text;
    public TextMeshProUGUI slot3Text;
    public TextMeshProUGUI slot4Text;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start() => UpdateUI();

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ExecuteSlotAction(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ExecuteSlotAction(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ExecuteSlotAction(3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ExecuteSlotAction(4);
    }

    public void AssignItem(ItemData item, int slotID)
    {
        if (item is WeaponItemData)
        {
            ItemData currentTargetItem = GetItemAtSlot(slotID);
            ClearItemFromAllSlots(item);
            
            if (CountWeapons() >= 2 && !(currentTargetItem is WeaponItemData))
            {
                return;
            }
        }
        else
        {
            ClearItemFromAllSlots(item);
        }

        if (slotID == 1) slot1 = item;
        else if (slotID == 2) slot2 = item;
        else if (slotID == 3) slot3 = item;
        else if (slotID == 4) slot4 = item;

        UpdateUI();
    }

    private void ExecuteSlotAction(int slotIndex)
    {
        ItemData targetItem = GetItemAtSlot(slotIndex);
        if (targetItem == null) return;

        if (targetItem is WeaponItemData weapon)
        {
            if (playerCombat != null) playerCombat.EquipWeapon(weapon);
        }
        else if (targetItem is ConsumableItemData consData)
        {
            int currentAmount = InventoryManager.Instance.GetItemAmount(targetItem);
            if (currentAmount > 0)
            {
                PlayerHealth playerHp = FindFirstObjectByType<PlayerHealth>();
                if (playerHp != null)
                {
                    if (playerHp.currentHealth >= playerHp.maxHealth) return;
                    playerHp.HealPlayer(consData.healAmount);
                }

                InventoryManager.Instance.AddItem(targetItem, -1);
                
                if (InventoryManager.Instance.GetItemAmount(targetItem) <= 0)
                {
                    ClearItemFromAllSlots(targetItem);
                }

                UpdateUI();
                FindFirstObjectByType<InventoryUIManager>()?.RefreshUI();
            }
            else
            {
                ClearItemFromAllSlots(targetItem);
                UpdateUI();
            }
        }
    }

    public void OnQuickSlotClicked(int slotID)
    {
        InventoryUIManager invUI = FindFirstObjectByType<InventoryUIManager>();
        if (invUI == null || !invUI.inventoryWindow.activeSelf || invUI.currentlySelectedItem == null) return;

        AssignItem(invUI.currentlySelectedItem, slotID);

        invUI.DisplayItemDetails(invUI.currentlySelectedItem);
    }

    public void UpdateUI()
    {
        SetupSlot(slot1, slot1Icon, slot1Text);
        SetupSlot(slot2, slot2Icon, slot2Text);
        SetupSlot(slot3, slot3Icon, slot3Text);
        SetupSlot(slot4, slot4Icon, slot4Text);
    }

    private void SetupSlot(ItemData item, Image icon, TextMeshProUGUI amountText)
    {
        if (item != null)
        {
            icon.sprite = item.itemIcon;
            icon.color = Color.white;
            
            if (amountText != null)
            {
                int amt = InventoryManager.Instance.GetItemAmount(item);
                if (item is WeaponItemData) amountText.text = "";
                else amountText.text = amt > 0 ? "x" + amt.ToString() : "";
            }
        }
        else
        {
            icon.color = new Color(1, 1, 1, 0);
            if (amountText != null) amountText.text = "";
        }
    }

    private ItemData GetItemAtSlot(int slot)
    {
        if (slot == 1) return slot1;
        if (slot == 2) return slot2;
        if (slot == 3) return slot3;
        return slot4;
    }

    public void ClearItemFromAllSlots(ItemData item)
    {
        if (slot1 == item) slot1 = null;
        if (slot2 == item) slot2 = null;
        if (slot3 == item) slot3 = null;
        if (slot4 == item) slot4 = null;
        UpdateUI();
    }

    public bool IsItemEquipped(ItemData item)
    {
        return slot1 == item || slot2 == item || slot3 == item || slot4 == item;
    }

    private int CountWeapons()
    {
        int count = 0;
        if (slot1 is WeaponItemData) count++;
        if (slot2 is WeaponItemData) count++;
        if (slot3 is WeaponItemData) count++;
        if (slot4 is WeaponItemData) count++;
        return count;
    }
}