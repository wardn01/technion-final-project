using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerAttributesUI : MonoBehaviour
{
    [Header("Player Data Reference")]
    public PlayerData playerData;

    [System.Serializable]
    public class AscensionItemSlotUI
    {
        public GameObject slotObject;
        public Image itemIcon;
        public TextMeshProUGUI amountText;
    }

    [Header("Text References")]
    public TextMeshProUGUI levelInfoText;
    public TextMeshProUGUI availablePointsText;
    public TextMeshProUGUI hpText, attackText, defenseText, staminaText;
    public TextMeshProUGUI goldText;
    public ItemData goldItemData;

    [Header("XP Bar UI")]
    public Image xpBarFill;
    public TextMeshProUGUI xpText;

    [Header("Ascension UI")]
    public Image[] ascensionStars; 
    public Color litStarColor = Color.white; 
    public Color unlitStarColor = new Color(0.5f, 0.5f, 0.5f, 0.3f); 
    public GameObject ascendGroup; 
    public Button ascendBtn;
    public AscensionItemSlotUI[] requiredItemSlots; 

    [Header("Stat Buttons (Plus & Minus)")]
    public Button hpPlusBtn, atkPlusBtn, defPlusBtn, stmPlusBtn;
    public Button hpMinusBtn, atkMinusBtn, defMinusBtn, stmMinusBtn;
    public Button resetBtn;

    private void OnEnable()
    {
        Invoke(nameof(RefreshUI), 0.02f);
    }

    void Start()
    {
        if(hpPlusBtn) hpPlusBtn.onClick.AddListener(() => { playerData.AllocateStatPoint(PlayerData.StatType.HP); RefreshUI(); SyncPlayerStats(); });
        if(atkPlusBtn) atkPlusBtn.onClick.AddListener(() => { playerData.AllocateStatPoint(PlayerData.StatType.Attack); RefreshUI(); });
        if(defPlusBtn) defPlusBtn.onClick.AddListener(() => { playerData.AllocateStatPoint(PlayerData.StatType.Defense); RefreshUI(); });
        if(stmPlusBtn) stmPlusBtn.onClick.AddListener(() => { playerData.AllocateStatPoint(PlayerData.StatType.Stamina); RefreshUI(); SyncPlayerStats(); });
        
        if(hpMinusBtn) hpMinusBtn.onClick.AddListener(() => { playerData.RemoveStatPoint(PlayerData.StatType.HP); RefreshUI(); SyncPlayerStats(); });
        if(atkMinusBtn) atkMinusBtn.onClick.AddListener(() => { playerData.RemoveStatPoint(PlayerData.StatType.Attack); RefreshUI(); });
        if(defMinusBtn) defMinusBtn.onClick.AddListener(() => { playerData.RemoveStatPoint(PlayerData.StatType.Defense); RefreshUI(); });
        if(stmMinusBtn) stmMinusBtn.onClick.AddListener(() => { playerData.RemoveStatPoint(PlayerData.StatType.Stamina); RefreshUI(); SyncPlayerStats(); });
        
        if(resetBtn) resetBtn.onClick.AddListener(() => { playerData.ResetStatPoints(); RefreshUI(); SyncPlayerStats(); });
        if(ascendBtn) ascendBtn.onClick.AddListener(() => { playerData.TryAscend(); RefreshUI(); SyncPlayerStats(); });
    }

    private void SyncPlayerStats()
    {
        PlayerHealth pHealth = FindFirstObjectByType<PlayerHealth>();
        if (pHealth != null)
        {
            pHealth.UpdateStatsFromData(); 
        }
    }

    public void RefreshUI()
    {
        if (playerData == null) return;
        var data = playerData;

        levelInfoText.text = $"Level {data.currentLevel} / {data.maxLevelCap}";
        availablePointsText.text = $"Stat Points: {data.availableStatPoints}";

        int totalHP = data.baseMaxHealth + (data.investedHPPoints * data.healthPerPoint);
        int totalAtk = data.baseAttack + (data.investedAtkPoints * data.attackPerPoint);
        int totalDef = data.baseDefense + (data.investedDefPoints * data.defensePerPoint);
        int totalStm = (int)(data.baseMaxStamina + (data.investedStaminaPoints * data.staminaPerPoint));
        
        hpText.text = $"Max HP: {totalHP} <color=#00FF00>(+{data.investedHPPoints})</color>";
        attackText.text = $"Attack: {totalAtk} <color=#00FF00>(+{data.investedAtkPoints})</color>";
        defenseText.text = $"Defense: {totalDef} <color=#00FF00>(+{data.investedDefPoints})</color>";
        staminaText.text = $"Stamina: {totalStm} <color=#00FF00>(+{data.investedStaminaPoints})</color>";

        if (goldText != null && goldItemData != null)
            goldText.text = InventoryManager.Instance.GetItemAmount(goldItemData).ToString("N0");

        UpdateXPBar();
        UpdateStarsUI();
        UpdateAscensionUI();
    }

    private void UpdateXPBar()
    {
        if (playerData == null) return;
        var data = playerData;
        if (xpBarFill) xpBarFill.fillAmount = (float)data.currentXP / data.xpToNextLevel;
        if (xpText) xpText.text = (data.currentLevel >= data.maxLevelCap) ? "MAX" : $"{data.currentXP} / {data.xpToNextLevel}";
    }

    private void UpdateStarsUI()
    {
        if (ascensionStars == null || playerData == null) return;
        for (int i = 0; i < ascensionStars.Length; i++)
            ascensionStars[i].color = (i < playerData.currentAscensionIndex) ? litStarColor : unlitStarColor;
    }

    private void UpdateAscensionUI()
    {
        if (playerData == null) return;
        var data = playerData;
        if (data.currentAscensionIndex < data.ascensionPhases.Length)
        {
            if (ascendGroup) ascendGroup.SetActive(true);
            var phase = data.ascensionPhases[data.currentAscensionIndex];

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
            if (ascendBtn) ascendBtn.interactable = data.CanAscend();
        }
        else if (ascendGroup) ascendGroup.SetActive(false);
    }
}