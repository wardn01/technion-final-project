using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CharacterMenuController : MonoBehaviour
{
    public static CharacterMenuController Instance { get; private set; }

    [Header("Main Navigation Roots")]
    public GameObject attributesScreen;
    public GameObject attributesSubPanel;
    public GameObject weaponsSubPanel;

    [Header("Tab Buttons Control")]
    public Button openAttributesBtn;
    public Button openWeaponsBtn;
    public TextMeshProUGUI attributesBtnText; 
    public TextMeshProUGUI weaponsBtnText;
    public Color activeTabColor = Color.white;
    public Color inactiveTabColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);

    [Header("Quick Slots Control")]
    public RectTransform quickSlotsBar;
    public Vector2 menuOpenPosition = new Vector2(0, -420);
    private Vector2 normalPosition;

    [Header("Build System UI (UpMenu)")]
    public Button[] buildTabBtns; 
    public TextMeshProUGUI[] buildTabTexts; 
    public Color selectedBuildColor = Color.white;
    public Color unselectedBuildColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    
    [Header("Screen Slide Animation")]
    public RectTransform mainContentPanel;
    public float slideDistance = 1500f;
    public float slideDuration = 0.25f;
    private Coroutine contentSlideCoroutine;

    private int currentSelectedBuildSlot = 0;
    private bool isInitializing = true;

    private void Awake() => Instance = this;

    void Start()
    {
        if (attributesScreen) attributesScreen.SetActive(false);
        if (quickSlotsBar) normalPosition = quickSlotsBar.anchoredPosition;

        if(openAttributesBtn) openAttributesBtn.onClick.AddListener(() => SwitchTab(true));
        if(openWeaponsBtn) openWeaponsBtn.onClick.AddListener(() => SwitchTab(false));

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

    public void ToggleMenu()
    {
        bool isOpening = !attributesScreen.activeSelf;
        attributesScreen.SetActive(isOpening);

        Cursor.lockState = isOpening ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isOpening;
        Time.timeScale = isOpening ? 0f : 1f;

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

    public void SwitchTab(bool showAttributes)
    {
        attributesSubPanel.SetActive(showAttributes);
        weaponsSubPanel.SetActive(!showAttributes);

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

        if (showAttributes) attributesSubPanel.GetComponent<PlayerAttributesUI>()?.RefreshUI();
        else weaponsSubPanel.GetComponent<WeaponUpgradeUI>()?.RefreshGrid();
    }

    private void SelectBuildTab(int index)
    {
        if (PlayerData.Instance == null) return;
        if (index == currentSelectedBuildSlot && !isInitializing) return;

        if (isInitializing)
        {
            UpdateBuildData(index);
        }
        else
        {
            if (contentSlideCoroutine != null) StopCoroutine(contentSlideCoroutine);
            contentSlideCoroutine = StartCoroutine(SlideContentAndLoad(index));
        }
    }

    private IEnumerator SlideContentAndLoad(int newIndex)
    {
        if (mainContentPanel == null) yield break;

        float direction = (newIndex > currentSelectedBuildSlot) ? -1f : 1f;
        Vector2 startPos = Vector2.zero;
        Vector2 targetOutPos = new Vector2(direction * slideDistance, 0);

        float elapsed = 0f;
        float halfDuration = slideDuration / 2f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / halfDuration;
            t = t * t * (3f - 2f * t); // Smoothstep
            mainContentPanel.anchoredPosition = Vector2.Lerp(startPos, targetOutPos, t);
            yield return null;
        }

        UpdateBuildData(newIndex);

        Vector2 targetInPos = new Vector2(-direction * slideDistance, 0);
        mainContentPanel.anchoredPosition = targetInPos;

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / halfDuration;
            t = 1f - Mathf.Pow(1f - t, 3f);
            mainContentPanel.anchoredPosition = Vector2.Lerp(targetInPos, startPos, t);
            yield return null;
        }

        mainContentPanel.anchoredPosition = startPos;
    }

    private void UpdateBuildData(int index)
    {
        if (!isInitializing)
        {
            PlayerData.Instance.SaveBuild(currentSelectedBuildSlot);
        }

        currentSelectedBuildSlot = index;
        PlayerData.Instance.LoadBuild(currentSelectedBuildSlot);

        if (buildTabBtns != null)
        {
            for (int i = 0; i < buildTabBtns.Length; i++)
            {
                Image btnImg = buildTabBtns[i].GetComponent<Image>();
                if (btnImg != null) btnImg.color = (i == index) ? selectedBuildColor : unselectedBuildColor;

                if (buildTabTexts != null && buildTabTexts.Length > i && buildTabTexts[i] != null)
                {
                    buildTabTexts[i].color = (i == index) ? Color.black : Color.white;
                }
            }
        }

        attributesSubPanel.GetComponent<PlayerAttributesUI>()?.RefreshUI();
        weaponsSubPanel.GetComponent<WeaponUpgradeUI>()?.RefreshGrid();
        
        PlayerHealth pHealth = FindFirstObjectByType<PlayerHealth>();
        if (pHealth != null) pHealth.UpdateStatsFromData(); 
    }
}