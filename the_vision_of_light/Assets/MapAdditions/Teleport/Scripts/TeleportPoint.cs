using UnityEngine;
using UnityEngine.UI;

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
    public GameObject interactPrompt;

    [TextArea]
    public string interactMessage = "Open Teleport [F]";

    private bool isPlayerNear = false;

    void Start()
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        if (isUnlocked && portalController != null)
            portalController.TogglePortal(true);

        UpdateMapIcons();
    }

    void Update()
    {
        if (isPlayerNear && !isUnlocked && Input.GetKeyDown(KeyCode.F))
        {
            UnlockPoint();
        }
    }

    void UnlockPoint()
    {
        isUnlocked = true;

        if (portalController != null)
            portalController.TogglePortal(true);

        UpdateMapIcons();

        if (interactPrompt != null)
            interactPrompt.SetActive(false);

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
        if (!other.CompareTag("Player"))
            return;

        isPlayerNear = true;

        if (isUnlocked)
            return;

        if (interactPrompt != null)
        {
            TMPro.TextMeshProUGUI text =
                interactPrompt.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            if (text != null)
                text.text = interactMessage;

            interactPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        isPlayerNear = false;

        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }
}