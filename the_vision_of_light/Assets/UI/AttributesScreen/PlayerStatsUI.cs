using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStatsUI : MonoBehaviour
{
    [System.Serializable]
    public class AscensionItemSlotUI
    {
        public GameObject slotObject;
        public Image itemIcon;
        public TextMeshProUGUI amountText;
    }

    [Header("Main Navigation")]
    public GameObject attributesScreen;
    public GameObject attributesSubPanel;
    public GameObject weaponsSubPanel;
    
    [Header("Tab Buttons")]
    public Button openAttributesBtn;
    public Button openWeaponsBtn;
    public TextMeshProUGUI attributesBtnText; 
    public TextMeshProUGUI weaponsBtnText;
    public Color activeTabColor = Color.white;
    public Color inactiveTabColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);

    [Header("Currency UI")]
    public TextMeshProUGUI goldText;
    public ItemData goldItemData;

    [Header("Text References")]
    public TextMeshProUGUI levelInfoText;
    
    [Header("XP Bar UI")]
    public Image xpBarFill;
    public TextMeshProUGUI xpText;

    [Header("Stats References")]
    public TextMeshProUGUI availablePointsText;
    public TextMeshProUGUI hpText, attackText, defenseText, staminaText;

    [Header("Ascension Stars")]
    public Image[] ascensionStars; 
    public Color litStarColor = Color.white; 
    public Color unlitStarColor = new Color(0.5f, 0.5f, 0.5f, 0.3f); 

    [Header("Buttons (Plus & Minus)")]
    public Button hpPlusBtn, atkPlusBtn, defPlusBtn, stmPlusBtn;
    public Button hpMinusBtn, atkMinusBtn, defMinusBtn, stmMinusBtn;
    public Button resetBtn;

    [Header("Ascension UI")]
    public GameObject ascendGroup; 
    public Button ascendBtn;
    public AscensionItemSlotUI[] requiredItemSlots; 

    void Start()
    {
        if (attributesScreen != null) attributesScreen.SetActive(false);

        if(openAttributesBtn) openAttributesBtn.onClick.AddListener(() => SwitchTab(true));
        if(openWeaponsBtn) openWeaponsBtn.onClick.AddListener(() => SwitchTab(false));

        if(hpPlusBtn) hpPlusBtn.onClick.AddListener(() => { PlayerData.Instance.AllocateStatPoint(PlayerData.StatType.HP); RefreshUI(); });
        if(atkPlusBtn) atkPlusBtn.onClick.AddListener(() => { PlayerData.Instance.AllocateStatPoint(PlayerData.StatType.Attack); RefreshUI(); });
        if(defPlusBtn) defPlusBtn.onClick.AddListener(() => { PlayerData.Instance.AllocateStatPoint(PlayerData.StatType.Defense); RefreshUI(); });
        if(stmPlusBtn) stmPlusBtn.onClick.AddListener(() => { PlayerData.Instance.AllocateStatPoint(PlayerData.StatType.Stamina); RefreshUI(); });
        
        if(hpMinusBtn) hpMinusBtn.onClick.AddListener(() => { PlayerData.Instance.RemoveStatPoint(PlayerData.StatType.HP); RefreshUI(); });
        if(atkMinusBtn) atkMinusBtn.onClick.AddListener(() => { PlayerData.Instance.RemoveStatPoint(PlayerData.StatType.Attack); RefreshUI(); });
        if(defMinusBtn) defMinusBtn.onClick.AddListener(() => { PlayerData.Instance.RemoveStatPoint(PlayerData.StatType.Defense); RefreshUI(); });
        if(stmMinusBtn) stmMinusBtn.onClick.AddListener(() => { PlayerData.Instance.RemoveStatPoint(PlayerData.StatType.Stamina); RefreshUI(); });
        
        if(resetBtn) resetBtn.onClick.AddListener(() => { PlayerData.Instance.ResetStatPoints(); RefreshUI(); });
        if(ascendBtn) ascendBtn.onClick.AddListener(() => { PlayerData.Instance.TryAscend(); RefreshUI(); });
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (UIManager.Instance != null && UIManager.Instance.IsAnyOtherPanelOpen(attributesScreen)) return;
            if (Time.timeScale == 0f && !attributesScreen.activeSelf) return;

            ToggleScreen();
        }
    }

    private void ToggleScreen()
    {
        bool isOpening = !attributesScreen.activeSelf;
        attributesScreen.SetActive(isOpening);

        Cursor.lockState = isOpening ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isOpening;
        Time.timeScale = isOpening ? 0f : 1f;

        if (isOpening)
        {
            SwitchTab(true);
            RefreshUI();
        }
    }

    public void SwitchTab(bool showAttributes)
    {
        if(attributesSubPanel) attributesSubPanel.SetActive(showAttributes);
        if(weaponsSubPanel) weaponsSubPanel.SetActive(!showAttributes);

        if (attributesBtnText) 
        {
            attributesBtnText.color = showAttributes ? activeTabColor : inactiveTabColor;
            attributesBtnText.text = showAttributes ? "♦ Attributes" : "  Attributes"; 
            attributesBtnText.transform.localScale = showAttributes ? new Vector3(1.1f, 1.1f, 1f) : Vector3.one; 
        }

        if (weaponsBtnText) 
        {
            weaponsBtnText.color = !showAttributes ? activeTabColor : inactiveTabColor;
            weaponsBtnText.text = !showAttributes ? "♦ Weapons" : "  Weapons"; 
            weaponsBtnText.transform.localScale = !showAttributes ? new Vector3(1.1f, 1.1f, 1f) : Vector3.one;
        }

        if (showAttributes) RefreshUI();
    }

    public void RefreshUI()
    {
        if (PlayerData.Instance == null || !attributesSubPanel.activeSelf) return;

        levelInfoText.text = $"Level {PlayerData.Instance.currentLevel} / {PlayerData.Instance.maxLevelCap}";
        availablePointsText.text = $"Stat Points: {PlayerData.Instance.availableStatPoints}";

        int baseHP = PlayerData.Instance.baseMaxHealth;
        int bonusHP = PlayerData.Instance.investedHPPoints * PlayerData.Instance.healthPerPoint;
        hpText.text = $"Max HP: {baseHP} <color=#00FF00>(+{bonusHP})</color>";

        int baseAtk = PlayerData.Instance.baseAttack;
        int bonusAtk = PlayerData.Instance.investedAtkPoints * PlayerData.Instance.attackPerPoint;
        attackText.text = $"Attack: {baseAtk} <color=#00FF00>(+{bonusAtk})</color>";

        int baseDef = PlayerData.Instance.baseDefense;
        int bonusDef = PlayerData.Instance.investedDefPoints * PlayerData.Instance.defensePerPoint;
        defenseText.text = $"Defense: {baseDef} <color=#00FF00>(+{bonusDef})</color>";

        float baseStm = PlayerData.Instance.baseMaxStamina;
        float bonusStm = PlayerData.Instance.investedStaminaPoints * PlayerData.Instance.staminaPerPoint;
        staminaText.text = $"Stamina: {baseStm} <color=#00FF00>(+{bonusStm})</color>";

        if (goldText != null && goldItemData != null)
        {
            int goldAmount = InventoryManager.Instance.GetItemAmount(goldItemData);
            goldText.text = goldAmount.ToString("N0"); 
        }

        UpdateStarsUI();
        UpdateAscensionUI();
        UpdateXPBar();
    }

    private void UpdateXPBar()
    {
        if (PlayerData.Instance == null) return;

        if (xpBarFill != null)
        {
            xpBarFill.fillAmount = (float)PlayerData.Instance.currentXP / PlayerData.Instance.xpToNextLevel;
        }

        if (xpText != null)
        {
            bool isMaxed = PlayerData.Instance.currentLevel >= PlayerData.Instance.maxLevelCap;
            xpText.text = isMaxed ? "MAX" : $"{PlayerData.Instance.currentXP} / {PlayerData.Instance.xpToNextLevel}";
        }
    }

    private void UpdateStarsUI()
    {
        if (ascensionStars == null) return;
        for (int i = 0; i < ascensionStars.Length; i++)
        {
            ascensionStars[i].color = (i < PlayerData.Instance.currentAscensionIndex) ? litStarColor : unlitStarColor;
        }
    }

    private void UpdateAscensionUI()
    {
        if (PlayerData.Instance.currentAscensionIndex < PlayerData.Instance.ascensionPhases.Length)
        {
            if (ascendGroup) ascendGroup.SetActive(true);
            AscensionPhase phase = PlayerData.Instance.ascensionPhases[PlayerData.Instance.currentAscensionIndex];

            for (int i = 0; i < requiredItemSlots.Length; i++)
            {
                if (requiredItemSlots[i]?.slotObject == null) continue;
                if (i < phase.requiredItems.Length)
                {
                    var req = phase.requiredItems[i];
                    requiredItemSlots[i].slotObject.SetActive(true);
                    requiredItemSlots[i].itemIcon.sprite = req.item.itemIcon;
                    int currentAmount = InventoryManager.Instance.GetItemAmount(req.item);
                    string colorTag = currentAmount >= req.amount ? "<color=green>" : "<color=red>";
                    requiredItemSlots[i].amountText.text = $"{colorTag}{currentAmount}</color>/{req.amount}";
                }
                else requiredItemSlots[i].slotObject.SetActive(false);
            }
            if (ascendBtn) ascendBtn.interactable = PlayerData.Instance.CanAscend();
        }
        else if (ascendGroup) ascendGroup.SetActive(false);
    }
}