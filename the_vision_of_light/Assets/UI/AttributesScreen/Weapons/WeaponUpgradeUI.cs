using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponUpgradeUI : MonoBehaviour
{
    [Header("Player Data Reference")]
    public PlayerData playerData;

    [System.Serializable]
    public class UpgradeMaterialSlot
    {
        public GameObject slotObject;
        public Image icon;
        public TextMeshProUGUI amountText;
    }

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

    [Header("Slot Highlight Colors")]
    public Color selectedSlotColor = Color.white;
    public Color unselectedSlotColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    private Image currentSelectedSlotImage;

    [Header("Right Panel - Details")]
    public Image detailIcon;
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailLevelText;
    public TextMeshProUGUI detailStatsText;
    public TextMeshProUGUI detailDescriptionText;

    [Header("Right Panel - Ascend Group")]
    public GameObject ascendGroupPanel;
    public Button ascendBtn;
    public UpgradeMaterialSlot[] materialSlots;

    [Header("Action Buttons")]
    public Button equipBtn;
    public TextMeshProUGUI equipBtnText;

    private List<GameObject> pool = new List<GameObject>();
    private ItemData currentlySelectedItem;
    private bool showingWeapons = true;

    public void OnEnable()
    {
        UpdateTabColors();
        RefreshGrid();
    }

    public void ShowWeaponsTab()
    {
        showingWeapons = true;
        UpdateTabColors();
        ResetHighlight();
        RefreshGrid();
    }

    public void ShowPotionsTab()
    {
        showingWeapons = false;
        UpdateTabColors();
        ResetHighlight();
        RefreshGrid();
    }

    private void UpdateTabColors()
    {
        if (weaponsTabIcon != null) weaponsTabIcon.color = showingWeapons ? activeTabColor : inactiveTabColor;
        if (potionsTabIcon != null) potionsTabIcon.color = !showingWeapons ? activeTabColor : inactiveTabColor;
    }

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
            if (index < pool.Count) { slot = pool[index]; slot.SetActive(true); }
            else { slot = Instantiate(loadoutSlotPrefab, slotsParent); pool.Add(slot); }

            slot.GetComponent<InventorySlotUI>().Setup(item, amount, this);
            index++;
        }

        for (int i = index; i < pool.Count; i++) pool[i].SetActive(false);

        if (topGoldText != null && goldCoinData != null)
            topGoldText.text = InventoryManager.Instance.GetItemAmount(goldCoinData).ToString("N0");

        if (isSelectedStillValid) DisplayItemDetails(currentlySelectedItem, false);
        else if (firstValidItem != null) DisplayItemDetails(firstValidItem, false);
        else ClearDetails();
    }

    public void DisplayItemDetails(ItemData item, bool fromUserClick = false)
    {
        if (item == null || playerData == null) return;
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

            int currentLvl = playerData.GetWeaponLevel(weapon.itemName);
            bool hasNextUpgrade = weapon.upgradeLevels != null && currentLvl <= weapon.upgradeLevels.Length;

            if (hasNextUpgrade)
            {
                WeaponUpgradeLevel upgradeData = weapon.upgradeLevels[currentLvl - 1];
                detailLevelText.text = "Level " + currentLvl;

                int currentDamage = weapon.weaponBaseAttack + GetTotalBoostUntil(weapon, currentLvl - 1);
                int nextDamage = currentDamage + upgradeData.damageBoost;
                detailStatsText.text = $"Damage: {currentDamage} -> <color=#00FF00>{nextDamage}</color>";

                bool isAscensionLocked = currentLvl > playerData.currentAscensionIndex;

                SetupUpgradeUI(upgradeData);
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
            else
            {
                detailLevelText.text = $"Level {currentLvl} (MAX)";
                int finalDamage = weapon.weaponBaseAttack + GetTotalBoostUntil(weapon, currentLvl - 1);
                detailStatsText.text = $"Damage: {finalDamage}";
                ClearMaterialSlots();
                ascendBtn.interactable = false;
            }
        }
        else
        {
            ascendGroupPanel.SetActive(false);
            detailLevelText.gameObject.SetActive(false);
            detailStatsText.gameObject.SetActive(false);
            if (detailDescriptionText != null) { detailDescriptionText.gameObject.SetActive(true); detailDescriptionText.text = item.description; }
        }
    }

    public void HighlightSlot(Image clickedBgImage)
    {
        if (currentSelectedSlotImage != null) currentSelectedSlotImage.color = unselectedSlotColor;
        currentSelectedSlotImage = clickedBgImage;
        if (currentSelectedSlotImage != null) currentSelectedSlotImage.color = selectedSlotColor;
    }

    public void ResetHighlight()
    {
        if (currentSelectedSlotImage != null) { currentSelectedSlotImage.color = unselectedSlotColor; currentSelectedSlotImage = null; }
    }

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

    private void UpdateSlotUI(int index, ItemData item, int requiredAmount)
    {
        if (item == null) return;
        materialSlots[index].slotObject.SetActive(true);
        materialSlots[index].icon.sprite = item.itemIcon;
        int currentAmount = InventoryManager.Instance.GetItemAmount(item);
        string colorTag = currentAmount >= requiredAmount ? "<color=green>" : "<color=red>";
        materialSlots[index].amountText.text = $"{colorTag}{currentAmount}</color>/{requiredAmount}";
    }

    private void CheckCanUpgrade(WeaponUpgradeLevel data)
    {
        bool hasMaterials = true;
        if (data.materials != null)
        {
            foreach (var m in data.materials)
            {
                if (m.item != null && InventoryManager.Instance.GetItemAmount(m.item) < m.amount) hasMaterials = false;
            }
        }
        ascendBtn.interactable = hasMaterials;
    }

    private void UpgradeWeapon(WeaponItemData weapon, WeaponUpgradeLevel data)
    {
        if (data.materials != null)
        {
            foreach (var m in data.materials)
            {
                if (m.item != null) InventoryManager.Instance.RemoveItem(m.item, m.amount);
            }
        }

        if (playerData != null) playerData.LevelUpWeapon(weapon.itemName);
        RefreshGrid();
    }

    private int GetTotalBoostUntil(WeaponItemData weapon, int levelIndex)
    {
        int total = 0;
        for (int i = 0; i < levelIndex; i++)
        {
            if (i < weapon.upgradeLevels.Length) total += weapon.upgradeLevels[i].damageBoost;
        }
        return total;
    }

    private void ClearMaterialSlots()
    {
        foreach (var slot in materialSlots) slot.slotObject.SetActive(false);
    }

    private void HandleEquip(ItemData item)
    {
        if (QuickSlotManager.Instance.IsItemEquipped(item))
        {
            QuickSlotManager.Instance.ClearItemFromAllSlots(item);
            if (item.type == ItemType.Weapon) FindFirstObjectByType<PlayerCombat>()?.UnequipCurrentWeapon();
        }
        else
        {
            QuickSlotManager.Instance.AssignToFirstEmptySlot(item);
        }
        DisplayItemDetails(item, false);
    }

    private void ClearDetails()
    {
        currentlySelectedItem = null;
        if (InventoryUIManager.Instance != null) { InventoryUIManager.Instance.currentlySelectedItem = null; InventoryUIManager.Instance.isItemClickedFromGrid = false; }
        detailIcon.color = new Color(1, 1, 1, 0);
        detailNameText.text = "Empty";
        ascendGroupPanel.SetActive(false);
        equipBtn.gameObject.SetActive(false);
        ResetHighlight();
    }
}