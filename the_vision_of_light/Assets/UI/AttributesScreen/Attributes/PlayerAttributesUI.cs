using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerAttributesUI : MonoBehaviour
{
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

    // 🔥 الحل لمشكلة التحديث أول مرة: OnEnable ينادي التحديث فور ظهور الشاشة
    private void OnEnable()
    {
        // تأخير بسيط جداً لضمان أن PlayerData جاهز في أول فريم
        Invoke(nameof(RefreshUI), 0.02f);
    }

    void Start()
    {
        // ربط كل الكبسات بـ PlayerData
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

    public void RefreshUI()
    {
        if (PlayerData.Instance == null) return;
        var data = PlayerData.Instance;

        levelInfoText.text = $"Level {data.currentLevel} / {data.maxLevelCap}";
        availablePointsText.text = $"Stat Points: {data.availableStatPoints}";

        hpText.text = $"Max HP: {data.baseMaxHealth} <color=#00FF00>(+{data.investedHPPoints * data.healthPerPoint})</color>";
        attackText.text = $"Attack: {data.baseAttack} <color=#00FF00>(+{data.investedAtkPoints * data.attackPerPoint})</color>";
        defenseText.text = $"Defense: {data.baseDefense} <color=#00FF00>(+{data.investedDefPoints * data.defensePerPoint})</color>";
        staminaText.text = $"Stamina: {data.baseMaxStamina} <color=#00FF00>(+{data.investedStaminaPoints * data.staminaPerPoint})</color>";

        if (goldText != null && goldItemData != null)
            goldText.text = InventoryManager.Instance.GetItemAmount(goldItemData).ToString("N0");

        UpdateXPBar();
        UpdateStarsUI();
        UpdateAscensionUI();
    }

    private void UpdateXPBar()
    {
        var data = PlayerData.Instance;
        if (xpBarFill) xpBarFill.fillAmount = (float)data.currentXP / data.xpToNextLevel;
        if (xpText) xpText.text = (data.currentLevel >= data.maxLevelCap) ? "MAX" : $"{data.currentXP} / {data.xpToNextLevel}";
    }

    private void UpdateStarsUI()
    {
        if (ascensionStars == null) return;
        for (int i = 0; i < ascensionStars.Length; i++)
            ascensionStars[i].color = (i < PlayerData.Instance.currentAscensionIndex) ? litStarColor : unlitStarColor;
    }

    private void UpdateAscensionUI()
    {
        var data = PlayerData.Instance;
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