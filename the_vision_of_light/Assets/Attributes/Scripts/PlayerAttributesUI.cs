using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the user interface for player attributes, level progression, and the ascension system.
/// Synchronizes visual data with the underlying PlayerData and updates PlayerHealth upon stat allocation.
/// </summary>
public class PlayerAttributesUI : MonoBehaviour
{
    #region Data Structures

    /// <summary>
    /// Represents a UI slot required for the character ascension process.
    /// Binds the physical UI elements to the required material data.
    /// </summary>
    [System.Serializable]
    public class AscensionItemSlotUI
    {
        public GameObject slotObject;
        public Image itemIcon;
        public TextMeshProUGUI amountText;
    }

    #endregion

    #region References

    [Header("Player Data Reference")]
    /// <summary>Reference to the persistent player data profile containing all core stats.</summary>
    public PlayerData playerData;

    [Header("Text References")]
    public TextMeshProUGUI levelInfoText;
    public TextMeshProUGUI availablePointsText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI defenseText;
    public TextMeshProUGUI staminaText;
    
    [Header("Currency UI")]
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
    public Button hpPlusBtn;
    public Button atkPlusBtn;
    public Button defPlusBtn;
    public Button stmPlusBtn;
    
    public Button hpMinusBtn;
    public Button atkMinusBtn;
    public Button defMinusBtn;
    public Button stmMinusBtn;
    
    public Button resetBtn;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Delays the UI refresh slightly to ensure all dependent data components have finished initialization.
    /// </summary>
    private void OnEnable()
    {
        Invoke(nameof(RefreshUI), 0.02f);
    }

    /// <summary>
    /// Binds listener events to all stat allocation and management buttons.
    /// Automatically triggers UI refreshes and gameplay stat synchronization upon interaction.
    /// </summary>
    private void Start()
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

    #endregion

    #region Data Synchronization & UI Refresh

    /// <summary>
    /// Forces the active PlayerHealth component in the scene to recalculate its maximum values 
    /// based on newly allocated or removed stat points.
    /// </summary>
    private void SyncPlayerStats()
    {
        PlayerHealth pHealth = FindAnyObjectByType<PlayerHealth>();
        if (pHealth != null)
        {
            pHealth.UpdateStatsFromData(); 
        }
    }

    /// <summary>
    /// Rebuilds the entire attributes UI panel, calculating total stats including base values 
    /// and invested bonuses, and updates text layouts.
    /// </summary>
    public void RefreshUI()
    {
        if (playerData == null) return;
        var data = playerData;

        if (levelInfoText != null) levelInfoText.text = $"Level {data.currentLevel} / {data.maxLevelCap}";
        if (availablePointsText != null) availablePointsText.text = $"Stat Points: {data.availableStatPoints}";

        int totalHP = data.baseMaxHealth + (data.investedHPPoints * data.healthPerPoint);
        int totalAtk = data.baseAttack + (data.investedAtkPoints * data.attackPerPoint);
        int totalDef = data.baseDefense + (data.investedDefPoints * data.defensePerPoint);
        int totalStm = (int)(data.baseMaxStamina + (data.investedStaminaPoints * data.staminaPerPoint));
        
        if (hpText != null) hpText.text = $"Max HP: {totalHP} <color=#00FF00>(+{data.investedHPPoints})</color>";
        if (attackText != null) attackText.text = $"Attack: {totalAtk} <color=#00FF00>(+{data.investedAtkPoints})</color>";
        if (defenseText != null) defenseText.text = $"Defense: {totalDef} <color=#00FF00>(+{data.investedDefPoints})</color>";
        if (staminaText != null) staminaText.text = $"Stamina: {totalStm} <color=#00FF00>(+{data.investedStaminaPoints})</color>";

        if (goldText != null && goldItemData != null && InventoryManager.Instance != null)
            goldText.text = InventoryManager.Instance.GetItemAmount(goldItemData).ToString("N0");

        UpdateXPBar();
        UpdateStarsUI();
        UpdateAscensionUI();
    }

    #endregion

    #region Sub-Component Updates

    /// <summary>
    /// Updates the visual fill amount and text representation of the player's current experience points.
    /// Accounts for maximum level capping to prevent overflow visuals.
    /// </summary>
    private void UpdateXPBar()
    {
        if (playerData == null) return;
        var data = playerData;

        if (xpBarFill) xpBarFill.fillAmount = data.xpToNextLevel > 0 ? (float)data.currentXP / data.xpToNextLevel : 0f;
        if (xpText) xpText.text = (data.currentLevel >= data.maxLevelCap) ? "MAX" : $"{data.currentXP} / {data.xpToNextLevel}";
    }

    /// <summary>
    /// Updates the ascension rank indicators (stars) below the character name/level, 
    /// highlighting achieved ranks.
    /// </summary>
    private void UpdateStarsUI()
    {
        if (ascensionStars == null || playerData == null) return;

        for (int i = 0; i < ascensionStars.Length; i++)
            ascensionStars[i].color = (i < playerData.currentAscensionIndex) ? litStarColor : unlitStarColor;
    }

    /// <summary>
    /// Evaluates current ascension requirements against the player's actual inventory data.
    /// Dynamically populates the material slots and controls the interactability of the Ascend button.
    /// </summary>
    private void UpdateAscensionUI()
    {
        if (playerData == null) return;
        var data = playerData;

        if (data.ascensionPhases != null && data.currentAscensionIndex < data.ascensionPhases.Length)
        {
            if (ascendGroup) ascendGroup.SetActive(true);
            var phase = data.ascensionPhases[data.currentAscensionIndex];

            for (int i = 0; requiredItemSlots != null && i < requiredItemSlots.Length; i++)
            {
                if (requiredItemSlots[i]?.slotObject == null) continue;

                if (phase.requiredItems != null && i < phase.requiredItems.Length)
                {
                    var req = phase.requiredItems[i];
                    
                    if (req.item == null) continue; 

                    requiredItemSlots[i].slotObject.SetActive(true);

                    if (requiredItemSlots[i].itemIcon != null)
                        requiredItemSlots[i].itemIcon.sprite = req.item.itemIcon;

                    int currentAmount = 0;
                    if (InventoryManager.Instance != null)
                    {
                        currentAmount = InventoryManager.Instance.GetItemAmount(req.item);
                    }

                    string colorTag = currentAmount >= req.amount ? "<color=green>" : "<color=red>";
                    
                    if (requiredItemSlots[i].amountText != null)
                        requiredItemSlots[i].amountText.text = $"{colorTag}{currentAmount}</color>/{req.amount}";
                }
                else 
                {
                    requiredItemSlots[i].slotObject.SetActive(false);
                }
            }
            if (ascendBtn) ascendBtn.interactable = data.CanAscend();
        }
        else if (ascendGroup) 
        {
            ascendGroup.SetActive(false);
        }
    }

    #endregion
}