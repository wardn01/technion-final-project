using UnityEngine;
using UnityEngine.UI;

public class TeleportPoint : MonoBehaviour
{
    [Header("Teleport Point Settings")]
    public bool isUnlocked = false; 

    [Header("Player landing point")]
    public Transform spawnLocation; 

    [Header("--- Portal Visual Settings ---")]
    public Portal_Controller portalController; 

    [Header("Icon Sprites")]
    public Sprite lockedIcon;    
    public Sprite unlockedIcon;   

    [Header("Map Icons")]
    public Image mapIcon;        
    public SpriteRenderer minimapIcon;

    [Header("Interaction (F Key UI)")]
    public GameObject interactPrompt;
    
    private bool isPlayerNear = false;

    void Start()
    {
        if (interactPrompt != null) interactPrompt.SetActive(false);

        if (isUnlocked && portalController != null)
        {
            portalController.TogglePortal(true);
        }
        
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
        {
            portalController.TogglePortal(true);
        }
        
        UpdateMapIcons();

        if (interactPrompt != null) interactPrompt.SetActive(false);

        Debug.Log("Portal Unlocked with Cinematic Audio & Effects!");
    }

    void UpdateMapIcons()
    {
        if (isUnlocked)
        {
            if (mapIcon != null && unlockedIcon != null) { mapIcon.sprite = unlockedIcon; mapIcon.color = Color.white; }
            if (minimapIcon != null && unlockedIcon != null) { minimapIcon.sprite = unlockedIcon; minimapIcon.color = Color.white; }
        }
        else
        {
            if (mapIcon != null && lockedIcon != null) { mapIcon.sprite = lockedIcon; mapIcon.color = Color.white; }
            if (minimapIcon != null && lockedIcon != null) { minimapIcon.sprite = lockedIcon; minimapIcon.color = Color.white; }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            isPlayerNear = true;
            if (!isUnlocked && interactPrompt != null) 
            {
                interactPrompt.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            isPlayerNear = false;
            if (interactPrompt != null) 
            {
                interactPrompt.SetActive(false);
            }
        }
    }
}