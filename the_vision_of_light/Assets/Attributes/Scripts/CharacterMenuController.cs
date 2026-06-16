using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using VisionOfLight.Player;

/// <summary>
/// Controls the Character Setup menu, handling tab navigation between Attributes and Weapons.
/// Manages the Build (Loadout) system with smooth UI transitions and controls the dynamic 
/// visibility and positioning of the Quick Slots bar depending on the active context.
/// </summary>
public class CharacterMenuController : MonoBehaviour
{
    #region Singleton

    /// <summary>
    /// Global singleton instance ensuring consistent access to the character menu state.
    /// </summary>
    public static CharacterMenuController Instance { get; private set; }

    #endregion

    #region Data & Navigation References

    [Header("Player Data")]
    /// <summary>Reference to the persistent player data to load/save stats and builds.</summary>
    public PlayerData playerData;

    [Header("Main Navigation Roots")]
    /// <summary>The root GameObject of the entire character menu screen.</summary>
    public GameObject attributesScreen;
    /// <summary>The UI panel displaying the player's core stats and ascension UI.</summary>
    public GameObject attributesSubPanel;
    /// <summary>The UI panel displaying the weapon inventory and upgrade system.</summary>
    public GameObject weaponsSubPanel;

    #endregion

    #region Tab Controls

    [Header("Tab Buttons Control")]
    public Button openAttributesBtn;
    public Button openWeaponsBtn;
    public TextMeshProUGUI attributesBtnText;
    public TextMeshProUGUI weaponsBtnText;

    public Color activeTabColor = Color.white;
    public Color inactiveTabColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);

    #endregion

    #region Quick Slots Management

    [Header("Quick Slots Control")]
    /// <summary>Reference to the Quick Slots bar to manipulate its position contextually.</summary>
    public RectTransform quickSlotsBar;
    /// <summary>The target position for the Quick Slots when the Weapons tab is active.</summary>
    public Vector2 menuOpenPosition = new Vector2(-170, -400);
    
    /// <summary>Caches the default gameplay position of the Quick Slots bar.</summary>
    private Vector2 normalPosition;

    #endregion

    #region Build System (Loadouts)

    [Header("Build System UI")]
    public Button[] buildTabBtns;
    public TextMeshProUGUI[] buildTabTexts; // Left here so inspector references don't break
    public Color selectedBuildColor = Color.white;
    public Color unselectedBuildColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    #endregion

    #region Animation Settings

    [Header("Screen Slide Animation")]
    /// <summary>The main content container that will be animated during build swaps.</summary>
    public RectTransform mainContentPanel;
    /// <summary>The distance (in pixels) the panel travels during the slide animation.</summary>
    public float slideDistance = 1500f;
    /// <summary>The duration (in seconds) of the slide animation.</summary>
    public float slideDuration = 0.25f;

    private Coroutine contentSlideCoroutine;
    private int currentSelectedBuildSlot = 0;
    private bool isInitializing = true;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        if (attributesScreen) attributesScreen.SetActive(false);
        if (quickSlotsBar) normalPosition = quickSlotsBar.anchoredPosition;

        if (openAttributesBtn) openAttributesBtn.onClick.AddListener(() => SwitchTab(true));
        if (openWeaponsBtn) openWeaponsBtn.onClick.AddListener(() => SwitchTab(false));

        if (buildTabBtns != null)
        {
            for (int i = 0; i < buildTabBtns.Length; i++)
            {
                int index = i;
                buildTabBtns[i].onClick.AddListener(() => SelectBuildTab(index));
            }
        }

        SelectBuildTab(0);
        isInitializing = false;
    }

    #endregion

    #region Menu Visibility & Tab Logic

    /// <summary>
    /// Toggles the entire character menu screen.
    /// Manages the restoration of the Quick Slots bar to its original state when closing.
    /// </summary>
    public void ToggleMenu()
    {
        if (attributesScreen == null) return;

        bool isOpening = !attributesScreen.activeSelf;
        attributesScreen.SetActive(isOpening);

        if (isOpening)
        {
            SwitchTab(true);
            if (mainContentPanel != null) mainContentPanel.anchoredPosition = Vector2.zero;
        }
        else
        {
            if (quickSlotsBar)
            {
                quickSlotsBar.gameObject.SetActive(true);
                quickSlotsBar.anchoredPosition = normalPosition;
            }
            if (contentSlideCoroutine != null) StopCoroutine(contentSlideCoroutine);
        }
    }

    /// <summary>
    /// Switches the active sub-panel between Attributes and Weapons.
    /// Dynamically adjusts the visibility and position of the Quick Slots bar based on the context.
    /// </summary>
    /// <param name="showAttributes">True to show the Attributes panel; False to show Weapons.</param>
    public void SwitchTab(bool showAttributes)
    {
        if (attributesSubPanel != null) attributesSubPanel.SetActive(showAttributes);
        if (weaponsSubPanel != null) weaponsSubPanel.SetActive(!showAttributes);

        if (quickSlotsBar)
        {
            if (showAttributes)
            {
                quickSlotsBar.gameObject.SetActive(false);
            }
            else
            {
                quickSlotsBar.gameObject.SetActive(true);
                quickSlotsBar.anchoredPosition = menuOpenPosition;
            }
        }

        // Restore the side-tab indicator glyph, color and scale exactly as configured.
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

        if (showAttributes && attributesSubPanel != null)
            attributesSubPanel.GetComponent<PlayerAttributesUI>()?.RefreshUI();
        else if (!showAttributes && weaponsSubPanel != null)
            weaponsSubPanel.GetComponent<WeaponUpgradeUI>()?.RefreshGrid();
    }

    #endregion

    #region Build System (Loadouts)

    /// <summary>
    /// Initiates a build (loadout) switch.
    /// Triggers the slide animation unless the menu is initializing.
    /// </summary>
    /// <param name="index">The index of the selected build slot.</param>
    private void SelectBuildTab(int index)
    {
        if (playerData == null) return;
        if (index == currentSelectedBuildSlot && !isInitializing) return;

        if (isInitializing) UpdateBuildData(index);
        else
        {
            if (contentSlideCoroutine != null) StopCoroutine(contentSlideCoroutine);
            contentSlideCoroutine = StartCoroutine(SlideContentAndLoad(index));
        }
    }

    /// <summary>
    /// Handles the smooth UI slide transition when swapping between builds.
    /// Utilizes unscaled time to ensure the animation plays smoothly even while the game is paused.
    /// </summary>
    /// <param name="newIndex">The target build index to load mid-transition.</param>
    private IEnumerator SlideContentAndLoad(int newIndex)
    {
        if (mainContentPanel == null) yield break;

        float direction = (newIndex > currentSelectedBuildSlot) ? -1f : 1f;
        Vector2 startPos = Vector2.zero;
        Vector2 targetOutPos = new Vector2(direction * slideDistance, 0);

        float elapsed = 0f;
        float halfDuration = slideDuration / 2f;

        // Slide Out Animation
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / halfDuration;
            t = t * t * (3f - 2f * t); // Smoothstep easing
            mainContentPanel.anchoredPosition = Vector2.Lerp(startPos, targetOutPos, t);
            yield return null;
        }

        // Swap Data while the panel is out of view
        UpdateBuildData(newIndex);

        Vector2 targetInPos = new Vector2(-direction * slideDistance, 0);
        mainContentPanel.anchoredPosition = targetInPos;
        elapsed = 0f;

        // Slide In Animation
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / halfDuration;
            t = 1f - Mathf.Pow(1f - t, 3f); // Cubic ease-out
            mainContentPanel.anchoredPosition = Vector2.Lerp(targetInPos, startPos, t);
            yield return null;
        }

        mainContentPanel.anchoredPosition = startPos;
    }

    /// <summary>
    /// Saves the current build state, loads the target build data, and synchronizes the UI and PlayerHealth.
    /// </summary>
    /// <param name="index">The build slot index to activate.</param>
    private void UpdateBuildData(int index)
    {
        if (playerData == null) return;
        
        // Save current progress before switching
        if (!isInitializing) playerData.SaveBuild(currentSelectedBuildSlot);

        currentSelectedBuildSlot = index;
        playerData.LoadBuild(currentSelectedBuildSlot);

        // Update Build Tab UI Visuals (Background color only)
        if (buildTabBtns != null)
        {
            for (int i = 0; i < buildTabBtns.Length; i++)
            {
                Image btnImg = buildTabBtns[i].GetComponent<Image>();
                if (btnImg != null) btnImg.color = (i == index) ? selectedBuildColor : unselectedBuildColor;

                // Text color is intentionally left untouched so it stays as set in the Inspector.
            }
        }

        // Refresh Sub-Panels with new data
        if (attributesSubPanel != null) attributesSubPanel.GetComponent<PlayerAttributesUI>()?.RefreshUI();
        if (weaponsSubPanel != null) weaponsSubPanel.GetComponent<WeaponUpgradeUI>()?.RefreshGrid();

        // Sync health system to reflect new stat allocations
        PlayerHealth pHealth = PlayerRegistry.Instance?.Health;
        if (pHealth != null) pHealth.UpdateStatsFromData();
    }

    #endregion
}