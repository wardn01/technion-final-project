using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponUpgradeUI : MonoBehaviour
{
    [System.Serializable]
    public class UpgradeMaterialSlot
    {
        public GameObject slotObject;
        public Image icon;
        public TextMeshProUGUI amountText;
    }

    [Header("Left Panel - Grid")]
    public Transform slotsParent;
    public GameObject loadoutSlotPrefab;
    
    [Header("Right Panel - Details")]
    public Image detailIcon;
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailLevelText;
    public TextMeshProUGUI detailStatsText;
    public TextMeshProUGUI detailDescriptionText;

    [Header("Right Panel - Ascend Group")]
    public GameObject ascendGroupPanel; 
    public Button ascendBtn;
    public ItemData goldItemData; 
    public UpgradeMaterialSlot[] materialSlots; 

    [Header("Action Buttons")]
    public Button equipBtn;
    public TextMeshProUGUI equipBtnText;

    private List<GameObject> pool = new List<GameObject>();
    private ItemData currentlySelectedItem;
    private bool showingWeapons = true;
    private int maxWeaponLevel = 10; 

    public void OnEnable() => RefreshGrid();

    public void ShowWeaponsTab() { showingWeapons = true; RefreshGrid(); }
    public void ShowPotionsTab() { showingWeapons = false; RefreshGrid(); }

    public void RefreshGrid()
    {
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

            GameObject slot;
            if (index < pool.Count)
            {
                slot = pool[index];
                slot.SetActive(true);
            }
            else
            {
                slot = Instantiate(loadoutSlotPrefab, slotsParent);
                pool.Add(slot);
            }

            slot.GetComponent<InventorySlotUI>().Setup(item, amount, this);
            index++;
        }

        for (int i = index; i < pool.Count; i++) pool[i].SetActive(false);

        if (isSelectedStillValid) DisplayItemDetails(currentlySelectedItem, false);
        else if (firstValidItem != null) DisplayItemDetails(firstValidItem, false);
        else ClearDetails();
    }

    public void DisplayItemDetails(ItemData item, bool fromUserClick = false)
    {
        if (item == null) return;
        currentlySelectedItem = item;

        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.currentlySelectedItem = item;
            InventoryUIManager.Instance.isItemClickedFromGrid = fromUserClick;
        }

        detailIcon.sprite = item.itemIcon;
        detailIcon.color = Color.white;
        detailNameText.text = item.itemName;

        equipBtn.gameObject.SetActive(true);
        equipBtnText.text = QuickSlotManager.Instance.IsItemEquipped(item) ? "Remove" : "Equip";
        equipBtn.onClick.RemoveAllListeners();
        equipBtn.onClick.AddListener(() => HandleEquip(item));

        if (item is WeaponItemData weapon)
        {
            ascendGroupPanel.SetActive(true);
            detailLevelText.gameObject.SetActive(true);
            detailStatsText.gameObject.SetActive(true);
            if (detailDescriptionText != null) detailDescriptionText.gameObject.SetActive(false);

            int currentLvl = PlayerData.Instance.GetWeaponLevel(weapon.itemName);
            bool isMaxed = currentLvl >= maxWeaponLevel;

            detailLevelText.text = isMaxed ? $"Level {maxWeaponLevel} (MAX)" : $"Level {currentLvl} / {maxWeaponLevel}";

            int currentDamage = weapon.normalAttackDamage + ((currentLvl - 1) * 5);
            int nextDamage = currentDamage + 5;

            detailStatsText.text = isMaxed ? $"Damage: {currentDamage}" : $"Damage: {currentDamage} ➔ <color=#00FF00>{nextDamage}</color>";

            SetupMaterialSlots(weapon, currentLvl, isMaxed);

            ascendBtn.onClick.RemoveAllListeners();
            if (!isMaxed) ascendBtn.onClick.AddListener(() => UpgradeWeapon(weapon));
            else ascendBtn.interactable = false;    
        }
        else
        {
            ascendGroupPanel.SetActive(false);
            detailLevelText.gameObject.SetActive(false);
            detailStatsText.gameObject.SetActive(false);

            if (detailDescriptionText != null)
            {
                detailDescriptionText.gameObject.SetActive(true);
                detailDescriptionText.text = item.description;
            }
        }
    }

    private void SetupMaterialSlots(WeaponItemData weapon, int currentLvl, bool isMaxed)
    {
        foreach (var slot in materialSlots) slot.slotObject.SetActive(false);
        if (isMaxed) { ascendBtn.interactable = false; return; }

        int nextLevelIndex = currentLvl - 1;
        if (nextLevelIndex >= weapon.upgradeLevels.Length) return;

        var nextLevelData = weapon.upgradeLevels[nextLevelIndex];
        int slotIndex = 0;

        if (goldItemData != null && slotIndex < materialSlots.Length)
        {
            UpdateSlotUI(slotIndex, goldItemData, nextLevelData.goldCost);
            slotIndex++;
        }

        foreach (var req in nextLevelData.materials)
        {
            if (slotIndex < materialSlots.Length)
            {
                UpdateSlotUI(slotIndex, req.item, req.amount);
                slotIndex++;
            }
        }

        CheckCanUpgrade(nextLevelData);
    }

    private void UpdateSlotUI(int index, ItemData item, int requiredAmount)
    {
        materialSlots[index].slotObject.SetActive(true);
        materialSlots[index].icon.sprite = item.itemIcon;
        int currentAmount = InventoryManager.Instance.GetItemAmount(item);
        
        string colorTag = currentAmount >= requiredAmount ? "<color=green>" : "<color=red>";
        materialSlots[index].amountText.text = $"{colorTag}{currentAmount}</color>/{requiredAmount}";
    }

    private void CheckCanUpgrade(WeaponUpgradeLevel reqs)
    {
        bool hasGold = InventoryManager.Instance.GetItemAmount(goldItemData) >= reqs.goldCost;
        bool hasMaterials = true;

        foreach (var m in reqs.materials)
        {
            if (InventoryManager.Instance.GetItemAmount(m.item) < m.amount)
                hasMaterials = false;
        }

        ascendBtn.interactable = hasGold && hasMaterials;
    }

    private void UpgradeWeapon(WeaponItemData weapon)
    {
        int currentLvl = PlayerData.Instance.GetWeaponLevel(weapon.itemName);
        var reqs = weapon.upgradeLevels[currentLvl - 1];

        InventoryManager.Instance.RemoveItem(goldItemData, reqs.goldCost);
        foreach (var m in reqs.materials)
            InventoryManager.Instance.RemoveItem(m.item, m.amount);

        PlayerData.Instance.LevelUpWeapon(weapon.itemName);
        RefreshGrid(); 
    }

    private void HandleEquip(ItemData item)
    {
        if (QuickSlotManager.Instance.IsItemEquipped(item))
        {
            QuickSlotManager.Instance.ClearItemFromAllSlots(item);
            if (item.type == ItemType.Weapon) FindFirstObjectByType<PlayerCombat>()?.UnequipCurrentWeapon();
        }
        else QuickSlotManager.Instance.AssignToFirstEmptySlot(item);

        DisplayItemDetails(item, false);
    }

    private void ClearDetails()
    {
        currentlySelectedItem = null;
        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.currentlySelectedItem = null;
            InventoryUIManager.Instance.isItemClickedFromGrid = false;
        }

        detailIcon.color = new Color(1, 1, 1, 0);
        detailNameText.text = "Empty";
        if (detailLevelText != null) detailLevelText.gameObject.SetActive(false);
        if (detailStatsText != null) detailStatsText.gameObject.SetActive(false);
        if (detailDescriptionText != null) detailDescriptionText.gameObject.SetActive(false);
        ascendGroupPanel.SetActive(false);
        equipBtn.gameObject.SetActive(false);
    }
}