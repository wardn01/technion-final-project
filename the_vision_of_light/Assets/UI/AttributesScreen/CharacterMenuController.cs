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

    private void Awake() => Instance = this;

    void Start()
    {
        if (attributesScreen) attributesScreen.SetActive(false);
        if (quickSlotsBar) normalPosition = quickSlotsBar.anchoredPosition;

        if(openAttributesBtn) openAttributesBtn.onClick.AddListener(() => SwitchTab(true));
        if(openWeaponsBtn) openWeaponsBtn.onClick.AddListener(() => SwitchTab(false));
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
        }
        else
        {
            if (quickSlotsBar)
            {
                quickSlotsBar.gameObject.SetActive(true);
                quickSlotsBar.anchoredPosition = normalPosition;
            }
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
    }
}