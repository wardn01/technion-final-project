using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuickSlotManager : MonoBehaviour
{
    public static QuickSlotManager Instance { get; private set; }

    [Header("References")]
    public PlayerCombat playerCombat;

    [Header("Equipped Data")]
    public WeaponItemData weapon1;
    public WeaponItemData weapon2;
    public ItemData consumable1;
    public ItemData consumable2;

    [Header("UI Icons")]
    public Image weapon1Icon;
    public Image weapon2Icon;
    public Image consumable1Icon;
    public Image consumable2Icon;

    [Header("Consumable Amounts")]
    public TextMeshProUGUI consumable1AmountText;
    public TextMeshProUGUI consumable2AmountText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        UpdateUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipWeaponSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipWeaponSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) UseConsumableSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha4)) UseConsumableSlot(2);
    }

    public void AssignWeapon(WeaponItemData weapon, int slot)
    {
        if (slot == 1) weapon1 = weapon;
        else if (slot == 2) weapon2 = weapon;
        
        UpdateUI();
        
        if (slot == 1) EquipWeaponSlot(1); 
    }

    public void AssignConsumable(ItemData item, int slot)
    {
        if (slot == 1) consumable1 = item;
        else if (slot == 2) consumable2 = item;
        
        UpdateUI();
    }

    private void EquipWeaponSlot(int slotIndex)
    {
        WeaponItemData weaponToEquip = (slotIndex == 1) ? weapon1 : weapon2;
        if (weaponToEquip != null && playerCombat != null)
        {
            playerCombat.EquipWeapon(weaponToEquip);
        }
    }

    private void UseConsumableSlot(int slotIndex)
    {
        ItemData consumableToUse = (slotIndex == 1) ? consumable1 : consumable2;
        if (consumableToUse != null)
        {
            int currentAmount = InventoryManager.Instance.GetItemAmount(consumableToUse);
            if (currentAmount > 0)
            {
                if (consumableToUse is ConsumableItemData consData)
                {
                    PlayerHealth playerHp = FindFirstObjectByType<PlayerHealth>();
                    if (playerHp != null)
                    {
                        if (playerHp.currentHealth >= playerHp.maxHealth) return; 
                        
                        playerHp.HealPlayer(consData.healAmount);
                    }
                }

                InventoryManager.Instance.AddItem(consumableToUse, -1); 
                
                int newAmount = InventoryManager.Instance.GetItemAmount(consumableToUse);
                if (newAmount <= 0)
                {
                    if (slotIndex == 1) consumable1 = null;
                    else consumable2 = null;
                }

                UpdateUI();
                FindFirstObjectByType<InventoryUIManager>()?.RefreshUI(); 
            }
            else
            {
                if (slotIndex == 1) consumable1 = null;
                else consumable2 = null;
                UpdateUI();
            }
        }
    }

    public void UpdateUI()
    {
        SetupSlot(weapon1, weapon1Icon, null);
        SetupSlot(weapon2, weapon2Icon, null);
        SetupSlot(consumable1, consumable1Icon, consumable1AmountText);
        SetupSlot(consumable2, consumable2Icon, consumable2AmountText);
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
                amountText.text = amt > 0 ? amt.ToString() : "0";
            }
        }
        else
        {
            icon.color = new Color(1, 1, 1, 0); 
            if (amountText != null) amountText.text = "";
        }
    }

    public void OnQuickSlotClicked(int slotID)
    {
        InventoryUIManager invUI = FindFirstObjectByType<InventoryUIManager>();
        
        if (invUI == null || !invUI.inventoryWindow.activeSelf || invUI.currentlySelectedItem == null) return;

        ItemData selectedItem = invUI.currentlySelectedItem;

        if (selectedItem is WeaponItemData weapon)
        {
            if (slotID == 1) AssignWeapon(weapon, 1);
            else if (slotID == 2) AssignWeapon(weapon, 2);
        }
        else if (selectedItem is ConsumableItemData consumable)
        {
            if (slotID == 3) AssignConsumable(consumable, 1); 
            else if (slotID == 4) AssignConsumable(consumable, 2); 
        }
    }
}