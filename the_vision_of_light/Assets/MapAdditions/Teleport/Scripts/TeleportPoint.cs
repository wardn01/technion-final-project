using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TeleportPoint : MonoBehaviour
{
    [Header("Teleport Point Settings")]
    public bool isUnlocked = false;

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

    private bool isPlayerNear = false;

    void Start()
    {
        if (isUnlocked && portalController != null)
            portalController.TogglePortal(true);

        UpdateMapIcons();
    }

    void Update()
    {
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

    void UnlockPoint()
    {
        isUnlocked = true;

        if (portalController != null)
            portalController.TogglePortal(true);

        UpdateMapIcons();

        if (promptContainer != null)
            promptContainer.SetActive(false);

        Debug.Log("Teleport Point Unlocked");
    }

    void UpdateMapIcons()
    {
        if (isUnlocked)
        {
            if (mapIcon != null && unlockedIcon != null)
            {
                mapIcon.sprite = unlockedIcon;
                mapIcon.color = Color.white;
            }

            if (minimapIcon != null && unlockedIcon != null)
            {
                minimapIcon.sprite = unlockedIcon;
                minimapIcon.color = Color.white;
            }
        }
        else
        {
            if (mapIcon != null && lockedIcon != null)
            {
                mapIcon.sprite = lockedIcon;
                mapIcon.color = Color.white;
            }

            if (minimapIcon != null && lockedIcon != null)
            {
                minimapIcon.sprite = lockedIcon;
                minimapIcon.color = Color.white;
            }
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
}