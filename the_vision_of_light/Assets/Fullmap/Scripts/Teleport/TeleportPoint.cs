using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages a specific teleport node in the game world.
/// Handles player proximity detection, state persistence (locked/unlocked),
/// and updating visual indicators on the world map.
/// Uses the shared InteractPrompt UI — must re-enable parent + F badge like doors/chests.
/// </summary>
public class TeleportPoint : MonoBehaviour
{
    #region Save Data & Settings
    [Header("Save Data")]
    /// <summary>A unique numeric identifier used to save the unlocked state of this teleport point.</summary>
    public int teleportID;

    [Header("Teleport Point Settings")]
    public bool isUnlocked = false;
    #endregion

    #region References
    [Header("Player Landing Point")]
    public Transform spawnLocation;

    [Header("Portal Visual Settings")]
    public Portal_Controller portalController;

    [Header("Icon Sprites")]
    public Sprite lockedIcon;
    public Sprite unlockedIcon;

    [Header("Map Icons")]
    public Image mapIcon;
    public SpriteRenderer minimapIcon;

    [Header("Interaction UI")]
    [Tooltip("Usually Interact_F under InteractPrompt.")]
    public GameObject promptContainer;

    [Tooltip("Optional. Parent InteractPrompt. Auto-resolved from promptContainer.")]
    public GameObject promptRoot;

    [Tooltip("Optional. F key badge (Interact_F/btn). Auto-resolved when empty.")]
    public GameObject interactKeyPrompt;

    public TextMeshProUGUI promptTextUI;
    public string promptText = "Open Teleport";
    #endregion

    private bool isPlayerNear;

    #region Unity Lifecycle
    private void Start()
    {
        ResolveSharedInteractUi();

        if (PlayerPrefs.GetInt("UnlockedTP_" + teleportID, 0) == 1)
            isUnlocked = true;

        if (isUnlocked && portalController != null)
            portalController.TogglePortal(true);

        UpdateMapIcons();
    }

    private void Update()
    {
        bool isMenuOpen =
            (ShopManager.Instance != null && ShopManager.Instance.shopPanel != null &&
             ShopManager.Instance.shopPanel.activeSelf) ||
            (UIManager.Instance != null && UIManager.Instance.isDialogueOpen);

        if (!isPlayerNear || isUnlocked)
            return;

        bool shouldShow = !isMenuOpen && Time.timeScale != 0f;

        if (shouldShow)
        {
            ShowInteractPrompt();

            if (Input.GetKeyDown(ShopManager.GetInteractKey()))
                UnlockPoint();
        }
        else
        {
            HideInteractPrompt();
        }
    }
    #endregion

    #region Prompt UI
    private void ShowInteractPrompt()
    {
        ResolveSharedInteractUi();

        if (promptRoot != null && !promptRoot.activeSelf)
            promptRoot.SetActive(true);

        if (promptContainer != null && !promptContainer.activeSelf)
            promptContainer.SetActive(true);

        if (interactKeyPrompt != null && !interactKeyPrompt.activeSelf)
            interactKeyPrompt.SetActive(true);

        if (promptTextUI == null)
            return;

        if (!promptTextUI.gameObject.activeSelf)
            promptTextUI.gameObject.SetActive(true);

        promptTextUI.color = Color.white;
        promptTextUI.text = string.IsNullOrEmpty(promptText) ? "Open Teleport" : promptText;
    }

    private void HideInteractPrompt()
    {
        if (promptContainer != null && promptContainer.activeSelf)
            promptContainer.SetActive(false);
    }

    private void ResolveSharedInteractUi()
    {
        if (promptRoot == null && promptContainer != null && promptContainer.transform.parent != null)
            promptRoot = promptContainer.transform.parent.gameObject;

        if (interactKeyPrompt == null && promptContainer != null)
        {
            Transform btn = promptContainer.transform.Find("btn");
            if (btn != null)
                interactKeyPrompt = btn.gameObject;
        }

        if (promptTextUI == null && promptContainer != null)
            promptTextUI = promptContainer.GetComponentInChildren<TextMeshProUGUI>(true);
    }
    #endregion

    #region Logic
    /// <summary>
    /// Unlocks the teleport point, activates the portal controller,
    /// updates UI, and persists the state to disk.
    /// </summary>
    private void UnlockPoint()
    {
        isUnlocked = true;

        PlayerPrefs.SetInt("UnlockedTP_" + teleportID, 1);
        PlayerPrefs.Save();

        if (portalController != null)
            portalController.TogglePortal(true);

        UpdateMapIcons();
        HideInteractPrompt();

        Debug.Log($"Teleport Point [{teleportID}] successfully unlocked.");
    }

    /// <summary>
    /// Updates the map and minimap icons based on the current unlocked state.
    /// </summary>
    private void UpdateMapIcons()
    {
        Sprite targetSprite = isUnlocked ? unlockedIcon : lockedIcon;

        if (mapIcon != null && targetSprite != null)
        {
            mapIcon.sprite = targetSprite;
            mapIcon.color = Color.white;
        }

        if (minimapIcon != null && targetSprite != null)
        {
            minimapIcon.sprite = targetSprite;
            minimapIcon.color = Color.white;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        isPlayerNear = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        isPlayerNear = false;
        HideInteractPrompt();
    }
    #endregion
}
