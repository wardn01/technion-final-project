using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages a specific teleport node in the game world. 
/// Handles player proximity detection, state persistence (locked/unlocked), 
/// and updating visual indicators on the world map.
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
    public GameObject promptContainer; 
    public TextMeshProUGUI promptTextUI;
    public string promptText = "Open Teleport [F]";
    #endregion

    private bool isPlayerNear = false;

    #region Unity Lifecycle
    private void Start()
    {
        // Load saved state using the numeric ID from PlayerPrefs
        if (PlayerPrefs.GetInt("UnlockedTP_" + teleportID, 0) == 1)
        {
            isUnlocked = true;
        }

        // Activate visuals if already unlocked
        if (isUnlocked && portalController != null)
            portalController.TogglePortal(true);

        UpdateMapIcons();
    }

    private void Update()
    {
        // Prevent interaction if UI menus are open or game is paused
        bool isMenuOpen = (ShopManager.Instance != null && ShopManager.Instance.shopPanel.activeSelf) || 
                          (UIManager.Instance != null && UIManager.Instance.isDialogueOpen);

        if (isPlayerNear && !isUnlocked)
        {
            bool shouldShow = !isMenuOpen && Time.timeScale != 0f;

            if (shouldShow)
            {
                if (promptContainer != null && !promptContainer.activeSelf) promptContainer.SetActive(true);
                
                if (promptTextUI != null)
                {
                    if (!promptTextUI.gameObject.activeSelf) promptTextUI.gameObject.SetActive(true);
                    promptTextUI.text = promptText;
                }

                // Handle interaction input
                if (Input.GetKeyDown(KeyCode.F))
                {
                    UnlockPoint();
                }
            }
            else
            {
                if (promptContainer != null && promptContainer.activeSelf) promptContainer.SetActive(false);
            }
        }
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

        // Save the unlocked state
        PlayerPrefs.SetInt("UnlockedTP_" + teleportID, 1);
        PlayerPrefs.Save();

        // Trigger the portal visual/audio sequence
        if (portalController != null)
            portalController.TogglePortal(true);

        UpdateMapIcons();

        if (promptContainer != null)
            promptContainer.SetActive(false);

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
        if (!other.CompareTag("Player")) return;
        isPlayerNear = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        isPlayerNear = false;
        
        if (promptContainer != null) promptContainer.SetActive(false);
    }
    #endregion
}