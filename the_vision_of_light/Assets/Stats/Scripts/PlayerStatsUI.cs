using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Player Stats screen: switches SideMenu tabs and binds values from <see cref="PlayerStatsTracker"/>.
/// Place on <c>StatsScreen</c> and wire buttons, info panels, and value texts in the Inspector.
/// Active tab gets a ">" marker in front of its label.
/// </summary>
public class PlayerStatsUI : MonoBehaviour
{
    public static PlayerStatsUI Instance { get; private set; }

    public enum StatsTab
    {
        CombatRecord,
        ElementalMastery,
        ExplorationFeats
    }

    [Header("Side Menu Buttons")]
    public Button combatRecordButton;
    public Button elementalMasteryButton;
    public Button explorationFeatsButton;

    [Header("Info Panels")]
    public GameObject combatRecordPanel;
    public GameObject elementalMasteryPanel;
    public GameObject explorationFeatsPanel;

    [Header("Combat Record Values")]
    public TextMeshProUGUI enemiesKilledValue;
    public TextMeshProUGUI totalDamageValue;
    public TextMeshProUGUI highestDamageValue;
    public TextMeshProUGUI timesDiedValue;

    [Header("Elemental Mastery Values")]
    public TextMeshProUGUI windDamageValue;
    public TextMeshProUGUI fireDamageValue;
    public TextMeshProUGUI iceDamageValue;

    [Header("Exploration & Feats Values")]
    public TextMeshProUGUI chestsOpenedValue;
    public TextMeshProUGUI wavesClearedValue;
    public TextMeshProUGUI potionsConsumedValue;

    [Header("Tab Look")]
    public Color activeTabColor = Color.white;
    public Color inactiveTabColor = new Color(1f, 1f, 1f, 0.45f);

    private StatsTab currentTab = StatsTab.CombatRecord;
    private string combatLabelBase = "Combat Record";
    private string elementalLabelBase = "Elemental Mastery";
    private string explorationLabelBase = "Exploration & Feats";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        CacheBaseLabels();
        WireButtons();
    }

    private void OnEnable()
    {
        ShowTab(currentTab);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void CacheBaseLabels()
    {
        combatLabelBase = CleanLabel(GetButtonLabelText(combatRecordButton), "Combat Record");
        elementalLabelBase = CleanLabel(GetButtonLabelText(elementalMasteryButton), "Elemental Mastery");
        explorationLabelBase = CleanLabel(GetButtonLabelText(explorationFeatsButton), "Exploration & Feats");
    }

    private void WireButtons()
    {
        if (combatRecordButton != null)
        {
            combatRecordButton.onClick.RemoveListener(ShowCombatRecord);
            combatRecordButton.onClick.AddListener(ShowCombatRecord);
        }

        if (elementalMasteryButton != null)
        {
            elementalMasteryButton.onClick.RemoveListener(ShowElementalMastery);
            elementalMasteryButton.onClick.AddListener(ShowElementalMastery);
        }

        if (explorationFeatsButton != null)
        {
            explorationFeatsButton.onClick.RemoveListener(ShowExplorationFeats);
            explorationFeatsButton.onClick.AddListener(ShowExplorationFeats);
        }
    }

    public void ShowCombatRecord() => ShowTab(StatsTab.CombatRecord);
    public void ShowElementalMastery() => ShowTab(StatsTab.ElementalMastery);
    public void ShowExplorationFeats() => ShowTab(StatsTab.ExplorationFeats);

    public void ShowTab(StatsTab tab)
    {
        currentTab = tab;

        if (combatRecordPanel != null)
            combatRecordPanel.SetActive(tab == StatsTab.CombatRecord);
        if (elementalMasteryPanel != null)
            elementalMasteryPanel.SetActive(tab == StatsTab.ElementalMastery);
        if (explorationFeatsPanel != null)
            explorationFeatsPanel.SetActive(tab == StatsTab.ExplorationFeats);

        ApplyTabHighlight(tab);
        RefreshValues();
    }

    public void RefreshValues()
    {
        PlayerStatistics stats = PlayerStatsTracker.Stats;

        SetText(enemiesKilledValue, FormatInt(stats.totalEnemiesKilled));
        SetText(totalDamageValue, FormatDamage(stats.totalDamageDealt));
        SetText(highestDamageValue, FormatDamage(stats.highestSingleDamage));
        SetText(timesDiedValue, FormatInt(stats.timesDied));

        SetText(windDamageValue, FormatDamage(stats.windDamageDealt));
        SetText(fireDamageValue, FormatDamage(stats.fireDamageDealt));
        SetText(iceDamageValue, FormatDamage(stats.iceDamageDealt));

        SetText(chestsOpenedValue, FormatInt(stats.chestsOpened));
        SetText(wavesClearedValue, FormatInt(stats.wavesCleared));
        SetText(potionsConsumedValue, FormatInt(stats.potionsConsumed));
    }

    private void ApplyTabHighlight(StatsTab tab)
    {
        SetTabLabel(combatRecordButton, combatLabelBase, tab == StatsTab.CombatRecord);
        SetTabLabel(elementalMasteryButton, elementalLabelBase, tab == StatsTab.ElementalMastery);
        SetTabLabel(explorationFeatsButton, explorationLabelBase, tab == StatsTab.ExplorationFeats);
    }

    private void SetTabLabel(Button button, string baseLabel, bool active)
    {
        if (button == null)
            return;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null)
            return;

        label.text = active ? ("> " + baseLabel) : baseLabel;
        label.color = active ? activeTabColor : inactiveTabColor;
    }

    private static string GetButtonLabelText(Button button)
    {
        if (button == null)
            return null;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        return label != null ? label.text : null;
    }

    private static string CleanLabel(string text, string fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
            return fallback;

        text = text.Trim();
        while (text.StartsWith(">"))
            text = text.Substring(1).TrimStart();

        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }

    private static void SetText(TextMeshProUGUI field, string value)
    {
        if (field != null)
            field.text = value;
    }

    private static string FormatInt(int value) => value.ToString("N0");

    private static string FormatDamage(float value)
    {
        return Mathf.RoundToInt(value).ToString("N0");
    }
}
