using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VisionOfLight.Enemy;

/// <summary>
/// Bestiary screen: left species list (locked until first kill) + right detail panel
/// (name, description, kill count, loot). Place on <c>MonsterScreen</c>.
/// List slots are spawned automatically from <see cref="MonsterBestiaryCatalog"/>.
/// </summary>
public class MonsterBestiaryUI : MonoBehaviour
{
    public static MonsterBestiaryUI Instance { get; private set; }

    [Header("Catalog")]
    public MonsterBestiaryCatalog catalog;

    [Header("Side List")]
    [Tooltip("Parent with Vertical Layout Group (usually Scroll Content). Slots spawn here.")]
    public Transform sideListParent;

    [Tooltip("Prefab of Slot_btn (Button + MonsterName + Icon). Drag from Project, not from scene.")]
    public GameObject listButtonPrefab;

    [Header("Detail — Identity")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI killsText;
    public Image iconImage;

    [Header("Detail — Loot")]
    public Transform lootListParent;

    [Tooltip("Prefab of LootSlot (ItemIcon + optional ItemName). Drag from Project.")]
    public GameObject lootRowPrefab;

    [Header("Locked State")]
    [Tooltip("Only this object is shown in Info while the entry is locked (your Info/none Image).")]
    public GameObject lockedNoneObject;

    [Tooltip("Shown in the list name when the monster has never been killed.")]
    public string lockedListLabel = "???";

    [Tooltip("Shown in the detail name / kills while locked (unused if lockedNoneObject is used).")]
    public string lockedInfoLabel = "???";

    [TextArea(2, 3)]
    public string lockedDetailMessage = "???";

    [Tooltip("Optional fallback sprite if lockedNoneObject is not assigned.")]
    public Sprite lockedIcon;

    [Header("Tab Look")]
    public Color activeTabColor = Color.white;
    public Color inactiveTabColor = new Color(1f, 1f, 1f, 0.45f);
    public Color lockedTabColor = new Color(1f, 1f, 1f, 0.55f);

    [Header("Debug / Test")]
    [Tooltip("When on, every catalog monster is shown unlocked (names, icons, loot).")]
    public bool revealAllForTest;

    private readonly List<Button> listButtons = new List<Button>();
    private readonly List<TextMeshProUGUI> listLabels = new List<TextMeshProUGUI>();
    private readonly List<Image> listIcons = new List<Image>();
    private int selectedIndex = -1;
    private Sprite cachedLockedIcon;
    private GameObject lootRowTemplate;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        EnsureCatalog();
        EnsureLockedNoneObject();
    }

    private void EnsureLockedNoneObject()
    {
        if (lockedNoneObject != null)
            return;

        // Prefer the Info/none child the user placed in the Hierarchy.
        Transform found = transform.Find("Info/none");
        if (found == null)
            found = FindDeepChild(transform, "none");

        if (found != null)
            lockedNoneObject = found.gameObject;
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
                return child;

            Transform nested = FindDeepChild(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private void EnsureCatalog()
    {
        if (catalog != null && catalog.entries != null && catalog.entries.Length > 0)
            return;

        catalog = Resources.Load<MonsterBestiaryCatalog>("MonsterBestiaryCatalog");
    }

    private void OnEnable()
    {
        EnsureCatalog();
        Refresh();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Rebuilds the list and refreshes the selected detail panel.</summary>
    public void Refresh()
    {
        RebuildList();

        if (catalog == null || catalog.entries == null || catalog.entries.Length == 0)
        {
            selectedIndex = -1;
            ShowEmptyDetail();
            return;
        }

        if (selectedIndex < 0 || selectedIndex >= catalog.entries.Length)
            selectedIndex = 0;

        SelectIndex(selectedIndex);
    }

    private void RebuildList()
    {
        ClearChildren(sideListParent);
        listButtons.Clear();
        listLabels.Clear();
        listIcons.Clear();

        if (catalog == null || catalog.entries == null || sideListParent == null)
            return;

        EnsureScrollContentCanGrow();

        for (int i = 0; i < catalog.entries.Length; i++)
        {
            MonsterBestiaryEntry entry = catalog.entries[i];
            if (entry == null || entry.stats == null)
                continue;

            int index = i;
            string monsterId = entry.stats.name;
            bool discovered = IsEntryDiscovered(monsterId);
            string label = discovered ? entry.stats.EnemyName : lockedListLabel;

            Button button = CreateListButton(label, ResolveListIcon(entry, discovered));
            listButtons.Add(button);
            listLabels.Add(FindNamedTmp(button.transform, "MonsterName")
                ?? button.GetComponentInChildren<TextMeshProUGUI>(true));
            listIcons.Add(FindNamedImage(button.transform, "Icon"));

            button.onClick.AddListener(() => SelectIndex(index));
        }

        ForceScrollLayoutRefresh();
    }

    /// <summary>
    /// Content must grow with spawned rows; otherwise ScrollRect thinks nothing overflows.
    /// </summary>
    private void EnsureScrollContentCanGrow()
    {
        VerticalLayoutGroup layout = sideListParent.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = sideListParent.gameObject.AddComponent<VerticalLayoutGroup>();

        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        ContentSizeFitter fitter = sideListParent.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = sideListParent.gameObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (sideListParent is RectTransform contentRt)
        {
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
        }

        ScrollRect scroll = sideListParent.GetComponentInParent<ScrollRect>();
        if (scroll == null)
            return;

        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = Mathf.Max(scroll.scrollSensitivity, 40f);
        scroll.content = sideListParent as RectTransform;
    }

    private void ForceScrollLayoutRefresh()
    {
        if (sideListParent is not RectTransform contentRt)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);

        ScrollRect scroll = sideListParent.GetComponentInParent<ScrollRect>();
        if (scroll != null)
        {
            scroll.verticalNormalizedPosition = 1f;
            scroll.velocity = Vector2.zero;
        }
    }

    private bool IsEntryDiscovered(string monsterId)
    {
        if (revealAllForTest)
            return true;

        return PlayerStatsTracker.IsDiscovered(monsterId);
    }

    /// <summary>Inspector / UI test: unlock every catalog monster visually.</summary>
    [ContextMenu("Test/Reveal All Monsters")]
    public void RevealAllForTest()
    {
        revealAllForTest = true;
        Refresh();
        Debug.Log("[Bestiary] Test reveal ON — all monsters shown unlocked.");
    }

    /// <summary>Inspector / UI test: back to real kill unlocks.</summary>
    [ContextMenu("Test/Lock Unknown Monsters")]
    public void LockUnknownForTest()
    {
        revealAllForTest = false;
        Refresh();
        Debug.Log("[Bestiary] Test reveal OFF — using real kill data.");
    }

    /// <summary>Left scroll thumbnail (MiniIcon preferred).</summary>
    private Sprite ResolveListIcon(MonsterBestiaryEntry entry, bool discovered)
    {
        if (!discovered || entry == null)
            return GetLockedIcon();

        if (entry.listIcon != null)
            return entry.listIcon;

        return ResolveDetailIcon(entry, discovered: true);
    }

    /// <summary>Right Info panel portrait (full image).</summary>
    private Sprite ResolveDetailIcon(MonsterBestiaryEntry entry, bool discovered)
    {
        if (!discovered || entry == null)
            return GetLockedIcon();

        if (entry.icon != null)
            return entry.icon;

        Sprite fromStats = entry.stats != null ? entry.stats.Icon : null;
        return fromStats != null ? fromStats : GetLockedIcon();
    }

    private Sprite GetLockedIcon()
    {
        if (lockedIcon != null)
            return lockedIcon;

        if (cachedLockedIcon == null)
            cachedLockedIcon = Resources.Load<Sprite>("none");

        return cachedLockedIcon;
    }

    private Button CreateListButton(string label, Sprite icon)
    {
        GameObject go;
        if (listButtonPrefab != null)
        {
            go = Instantiate(listButtonPrefab, sideListParent);
            go.name = "MonsterRow";
        }
        else
        {
            go = new GameObject("MonsterRow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(sideListParent, false);

            Image bg = go.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.01f);

            GameObject textGo = new GameObject("MonsterName", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8f, 2f);
            textRt.offsetMax = new Vector2(-8f, -2f);

            TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 28f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
        }

        RectTransform rowRt = go.GetComponent<RectTransform>();
        if (rowRt != null && listButtonPrefab == null)
            rowRt.sizeDelta = new Vector2(rowRt.sizeDelta.x, 40f);

        TextMeshProUGUI labelTmp = FindNamedTmp(go.transform, "MonsterName")
            ?? go.GetComponentInChildren<TextMeshProUGUI>(true);
        if (labelTmp != null)
            labelTmp.text = label;

        ApplyIcon(FindNamedImage(go.transform, "Icon"), icon);

        Button button = go.GetComponent<Button>();
        if (button == null)
            button = go.AddComponent<Button>();

        LayoutElement layoutElement = go.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = go.AddComponent<LayoutElement>();

        RectTransform rt = go.GetComponent<RectTransform>();
        float rowHeight = rt != null && rt.sizeDelta.y > 1f ? rt.sizeDelta.y : 180f;
        layoutElement.minHeight = rowHeight;
        layoutElement.preferredHeight = rowHeight;
        layoutElement.flexibleHeight = 0f;

        return button;
    }

    public void SelectIndex(int index)
    {
        if (catalog == null || catalog.entries == null)
            return;

        if (index < 0 || index >= catalog.entries.Length)
            return;

        selectedIndex = index;
        MonsterBestiaryEntry entry = catalog.entries[index];
        if (entry == null || entry.stats == null)
        {
            ShowEmptyDetail();
            return;
        }

        string monsterId = entry.stats.name;
        bool discovered = IsEntryDiscovered(monsterId);

        // Restyle list buttons (created only for valid catalog entries, in order).
        int buttonIndex = 0;
        for (int i = 0; i < catalog.entries.Length; i++)
        {
            MonsterBestiaryEntry e = catalog.entries[i];
            if (e == null || e.stats == null)
                continue;

            if (buttonIndex >= listLabels.Count)
                break;

            bool disc = IsEntryDiscovered(e.stats.name);
            bool selected = i == selectedIndex;
            TextMeshProUGUI label = listLabels[buttonIndex];
            if (label != null)
            {
                if (!disc)
                {
                    label.text = lockedListLabel;
                    label.color = lockedTabColor;
                }
                else
                {
                    label.text = selected ? ("> " + e.stats.EnemyName) : e.stats.EnemyName;
                    label.color = selected ? activeTabColor : inactiveTabColor;
                }
            }

            if (buttonIndex < listIcons.Count)
                ApplyIcon(listIcons[buttonIndex], ResolveListIcon(e, disc));

            buttonIndex++;
        }

        if (!discovered)
        {
            ShowLockedDetail();
            return;
        }

        ShowUnlockedDetail(entry);
    }

    private void ShowLockedDetail()
    {
        EnsureLockedNoneObject();
        SetDetailUnlockedVisible(false);

        if (lockedNoneObject != null)
        {
            lockedNoneObject.SetActive(true);
            ClearLootChildren();
            return;
        }

        // Fallback if Info/none was not wired: old text + locked sprite behavior.
        SetText(nameText, lockedInfoLabel);
        SetText(descriptionText, lockedDetailMessage);
        SetText(killsText, lockedInfoLabel);
        ApplyIcon(iconImage, GetLockedIcon());
        ClearLootChildren();
    }

    private void ShowUnlockedDetail(MonsterBestiaryEntry entry)
    {
        EnsureLockedNoneObject();
        if (lockedNoneObject != null)
            lockedNoneObject.SetActive(false);

        SetDetailUnlockedVisible(true);

        EnemyBaseStats stats = entry.stats;
        string monsterId = stats.name;
        int kills = PlayerStatsTracker.GetKillCount(monsterId);

        SetText(nameText, stats.EnemyName);
        SetText(descriptionText, string.IsNullOrWhiteSpace(entry.description) ? string.Empty : entry.description);
        SetText(killsText, kills.ToString("N0"));

        if (iconImage == null)
        {
            Transform info = transform.Find("Info");
            if (info != null)
                iconImage = FindNamedImage(info, "Icon");
        }

        ApplyIcon(iconImage, ResolveDetailIcon(entry, discovered: true));
        RebuildLootList(stats.LootTable);
    }

    private void ShowEmptyDetail()
    {
        EnsureLockedNoneObject();
        if (lockedNoneObject != null)
            lockedNoneObject.SetActive(false);

        SetDetailUnlockedVisible(true);
        SetText(nameText, string.Empty);
        SetText(descriptionText, "No monsters in catalog.");
        SetText(killsText, string.Empty);
        ApplyIcon(iconImage, null);
        ClearLootChildren();
    }

    private void SetDetailUnlockedVisible(bool visible)
    {
        SetActive(nameText, visible);
        SetActive(descriptionText, visible);
        SetActive(killsText, visible);

        // Hide the "Times Defeated" label wrapper with the number.
        if (killsText != null)
        {
            Transform killsParent = killsText.transform.parent;
            if (killsParent != null && killsParent != transform && killsParent.name != "Info")
                killsParent.gameObject.SetActive(visible);
        }

        Image icon = iconImage;
        if (icon == null)
        {
            Transform info = transform.Find("Info");
            if (info != null)
                icon = FindNamedImage(info, "Icon");
            if (icon == null)
                icon = FindNamedImage(transform, "Icon");
            if (visible && icon != null)
                iconImage = icon;
        }

        SetActive(icon, visible);

        // Also toggle IconClip so an empty frame is not left visible while locked.
        if (icon != null && icon.transform.parent != null && icon.transform.parent.name == "IconClip")
            icon.transform.parent.gameObject.SetActive(visible);
    }

    private static void SetActive(Component component, bool visible)
    {
        if (component != null)
            component.gameObject.SetActive(visible);
    }

    private static void ApplyIcon(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = true;
        image.type = Image.Type.Simple;
        // Stretch to fill the slot/clip (same look as placing the sprite in the Editor).
        image.preserveAspect = false;
        image.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        image.gameObject.SetActive(true);

        // Remove runtime fitter if a previous test pass added one.
        AspectRatioFitter fitter = image.GetComponent<AspectRatioFitter>();
        if (fitter != null)
            Object.Destroy(fitter);

        RectTransform rt = image.rectTransform;
        if (rt == null)
            return;

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private void EnsureLootRowTemplate()
    {
        if (lootRowTemplate != null)
            return;

        if (lootRowPrefab != null)
        {
            lootRowTemplate = lootRowPrefab;

            // Scene object under LootList: keep it as a hidden template (do not Destroy).
            if (lootListParent != null && lootRowTemplate.transform.IsChildOf(lootListParent))
                lootRowTemplate.SetActive(false);

            return;
        }

        if (lootListParent == null)
            return;

        // Fallback: first child that has an ItemIcon.
        for (int i = 0; i < lootListParent.childCount; i++)
        {
            Transform child = lootListParent.GetChild(i);
            if (lockedNoneObject != null && child.gameObject == lockedNoneObject)
                continue;

            if (FindNamedImage(child, "ItemIcon") != null)
            {
                lootRowTemplate = child.gameObject;
                lootRowTemplate.SetActive(false);
                lootRowPrefab = lootRowTemplate;
                return;
            }
        }
    }

    private void RebuildLootList(LootDrop[] lootTable)
    {
        EnsureLootRowTemplate();
        ClearLootChildren();

        if (lootListParent == null || lootTable == null)
            return;

        lootListParent.gameObject.SetActive(true);

        foreach (LootDrop drop in lootTable)
        {
            if (drop == null || drop.item == null)
                continue;

            string amount = drop.minAmount == drop.maxAmount
                ? drop.minAmount.ToString()
                : $"{drop.minAmount}-{drop.maxAmount}";

            string line = $"{drop.item.itemName}  x{amount}  ({drop.dropChance:0.#}%)";
            CreateLootRow(drop.item.itemIcon, line);
        }
    }

    private void CreateLootRow(Sprite icon, string label)
    {
        EnsureLootRowTemplate();

        if (lootRowTemplate != null)
        {
            GameObject row = Instantiate(lootRowTemplate, lootListParent);
            row.name = "LootSlot";
            row.SetActive(true);

            Image img = FindNamedImage(row.transform, "ItemIcon");
            if (img == null)
            {
                foreach (Image candidate in row.GetComponentsInChildren<Image>(true))
                {
                    if (candidate == null)
                        continue;
                    if (candidate.gameObject == row)
                        continue;
                    img = candidate;
                    break;
                }
            }

            ApplyIcon(img, icon);

            TextMeshProUGUI tmp = FindNamedTmp(row.transform, "ItemName")
                ?? FindNamedTmp(row.transform, "ItemAmountText")
                ?? FindNamedTmp(row.transform, "Label")
                ?? row.GetComponentInChildren<TextMeshProUGUI>(true);

            if (tmp != null)
                tmp.text = label;

            return;
        }

        // Last resort: icon-only cell if no prefab/template was wired.
        GameObject go = new GameObject("LootSlot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(lootListParent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100f, 100f);
        ApplyIcon(go.GetComponent<Image>(), icon);
    }

    /// <summary>Clears spawned loot rows but keeps the template + locked none object.</summary>
    private void ClearLootChildren()
    {
        if (lootListParent == null)
            return;

        for (int i = lootListParent.childCount - 1; i >= 0; i--)
        {
            GameObject child = lootListParent.GetChild(i).gameObject;
            if (lootRowTemplate != null && child == lootRowTemplate)
            {
                child.SetActive(false);
                continue;
            }

            if (lockedNoneObject != null && child == lockedNoneObject)
            {
                child.SetActive(false);
                continue;
            }

            Destroy(child);
        }
    }

    private static TextMeshProUGUI FindNamedTmp(Transform root, string childName)
    {
        if (root == null)
            return null;

        Transform child = root.Find(childName);
        if (child != null)
            return child.GetComponent<TextMeshProUGUI>();

        foreach (TextMeshProUGUI tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp != null && tmp.gameObject.name == childName)
                return tmp;
        }

        return null;
    }

    private static Image FindNamedImage(Transform root, string childName)
    {
        if (root == null)
            return null;

        Transform child = root.Find(childName);
        if (child != null)
            return child.GetComponent<Image>();

        foreach (Image img in root.GetComponentsInChildren<Image>(true))
        {
            if (img != null && img.gameObject.name == childName)
                return img;
        }

        return null;
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private static void SetText(TextMeshProUGUI field, string value)
    {
        if (field != null)
            field.text = value ?? string.Empty;
    }
}
