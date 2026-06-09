using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Displays short-lived warning messages inside context-specific UI anchors
/// (inventory, attributes, shop, or default HUD).
/// </summary>
public class NotificationManager : MonoBehaviour
{
    #region Singleton

    public static NotificationManager Instance { get; private set; }

    #endregion

    #region References

    [Header("UI References")]
    public TextMeshProUGUI warningText;
    public float displayTime = 1.5f;

    [Header("Warning Places")]
    [Tooltip("Empty RectTransform inside InventoryScreen.")]
    public RectTransform inventoryWarningPlace;

    [Tooltip("Empty RectTransform inside Attributes/Setup screen.")]
    public RectTransform attributesWarningPlace;

    [Tooltip("Empty RectTransform inside ShopScreen.")]
    public RectTransform shopWarningPlace;

    [Tooltip("Fallback place when no menu screen is open.")]
    public RectTransform defaultWarningPlace;

    #endregion

    #region State

    private bool referencesResolved;

    private static readonly string[] InventoryRootNames = { "InventoryScreen", "Inventory" };
    private static readonly string[] AttributesRootNames = { "SetupScreen", "AttributesScreen", "attributesScreen" };
    private static readonly string[] ShopRootNames = { "ShopScreen", "Shop" };
    private static readonly string[] HudRootNames = { "HUDScreen", "HUD" };

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (warningText == null)
            warningText = GetComponent<TextMeshProUGUI>();

        if (Instance != null && Instance != this)
        {
            NotificationManager keeper = PickKeeper(Instance, this);
            NotificationManager duplicate = keeper == Instance ? this : Instance;

            if (duplicate != null)
                Destroy(duplicate.gameObject);

            Instance = keeper;
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        EnsureReferences();

        if (warningText != null)
            warningText.gameObject.SetActive(false);
    }

    #endregion

    #region Warnings

    /// <summary>
    /// Shows a warning in the warning place that matches the currently open UI screen.
    /// </summary>
    public void ShowWarning(string message)
    {
        EnsureReferences();

        if (warningText == null)
        {
            Debug.LogWarning($"[NotificationManager] Missing Warning Text. Message was: {message}");
            return;
        }

        RectTransform targetPlace = ResolveWarningPlace();
        MoveWarningToPlace(targetPlace);

        warningText.text = message;
        warningText.gameObject.SetActive(true);
        warningText.transform.SetAsLastSibling();

        StopAllCoroutines();
        StartCoroutine(HideWarningAfterDelay());
    }

    /// <summary>
    /// Picks the warning anchor based on which major UI screen is currently active.
    /// </summary>
    private RectTransform ResolveWarningPlace()
    {
        if (ShopManager.Instance != null
            && ShopManager.Instance.shopPanel != null
            && ShopManager.Instance.shopPanel.activeSelf
            && shopWarningPlace != null)
        {
            return shopWarningPlace;
        }

        if (IsInventoryOpen() && inventoryWarningPlace != null)
            return inventoryWarningPlace;

        if (IsAttributesOpen() && attributesWarningPlace != null)
            return attributesWarningPlace;

        if (defaultWarningPlace != null)
            return defaultWarningPlace;

        Canvas canvas = warningText.canvas;
        return canvas != null ? canvas.transform as RectTransform : null;
    }

    /// <summary>
    /// Parents the warning text under the selected place and centers it there.
    /// </summary>
    private void MoveWarningToPlace(RectTransform place)
    {
        if (place == null)
            return;

        RectTransform textRect = warningText.rectTransform;

        if (textRect.parent != place)
            textRect.SetParent(place, false);

        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.localScale = Vector3.one;
    }

    private IEnumerator HideWarningAfterDelay()
    {
        yield return new WaitForSecondsRealtime(displayTime);

        if (warningText != null)
            warningText.gameObject.SetActive(false);
    }

    #endregion

    #region Reference Resolution

    /// <summary>
    /// Auto-finds the warning text and WarningPlace objects when inspector fields are empty.
    /// </summary>
    private void EnsureReferences()
    {
        if (referencesResolved)
            return;

        if (warningText == null)
        {
            foreach (TextMeshProUGUI text in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (text.gameObject.name == "Warning")
                {
                    warningText = text;
                    break;
                }
            }
        }

        AutoBindWarningPlaces();
        referencesResolved = true;
    }

    /// <summary>
    /// Scans the scene for objects named WarningPlace and maps them to the matching screen.
    /// </summary>
    private void AutoBindWarningPlaces()
    {
        RectTransform[] allRects = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (RectTransform rect in allRects)
        {
            if (rect.name != "WarningPlace")
                continue;

            string path = BuildHierarchyPath(rect);

            if (inventoryWarningPlace == null && ContainsRootName(path, InventoryRootNames))
                inventoryWarningPlace = rect;
            else if (attributesWarningPlace == null && ContainsRootName(path, AttributesRootNames))
                attributesWarningPlace = rect;
            else if (shopWarningPlace == null && ContainsRootName(path, ShopRootNames))
                shopWarningPlace = rect;
            else if (defaultWarningPlace == null && ContainsRootName(path, HudRootNames))
                defaultWarningPlace = rect;
        }
    }

    private static NotificationManager PickKeeper(NotificationManager first, NotificationManager second)
    {
        bool firstValid = first != null && first.HasWarningText();
        bool secondValid = second != null && second.HasWarningText();

        if (firstValid && !secondValid)
            return first;

        if (secondValid && !firstValid)
            return second;

        bool firstHasPlaces = first != null && first.HasAnyWarningPlace();
        bool secondHasPlaces = second != null && second.HasAnyWarningPlace();

        if (firstHasPlaces && !secondHasPlaces)
            return first;

        if (secondHasPlaces && !firstHasPlaces)
            return second;

        return first != null ? first : second;
    }

    private bool HasWarningText()
    {
        return warningText != null || GetComponent<TextMeshProUGUI>() != null;
    }

    private bool HasAnyWarningPlace()
    {
        return inventoryWarningPlace != null
            || attributesWarningPlace != null
            || shopWarningPlace != null
            || defaultWarningPlace != null;
    }

    private static bool ContainsRootName(string path, string[] rootNames)
    {
        foreach (string rootName in rootNames)
        {
            if (path.Contains(rootName))
                return true;
        }

        return false;
    }

    private static string BuildHierarchyPath(Transform transform)
    {
        string path = transform.name;
        Transform current = transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private static bool IsInventoryOpen()
    {
        if (InventoryUIManager.Instance != null
            && InventoryUIManager.Instance.inventoryWindow != null
            && InventoryUIManager.Instance.inventoryWindow.activeSelf)
        {
            return true;
        }

        return PauseMenuManager.Instance != null
            && PauseMenuManager.Instance.inventoryScreen != null
            && PauseMenuManager.Instance.inventoryScreen.activeSelf;
    }

    private static bool IsAttributesOpen()
    {
        if (CharacterMenuController.Instance != null
            && CharacterMenuController.Instance.attributesScreen != null
            && CharacterMenuController.Instance.attributesScreen.activeSelf)
        {
            return true;
        }

        return PauseMenuManager.Instance != null
            && PauseMenuManager.Instance.setupScreen != null
            && PauseMenuManager.Instance.setupScreen.activeSelf;
    }

    #endregion
}
