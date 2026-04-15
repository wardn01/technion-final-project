using UnityEngine;
using UnityEngine.UI;

public class TeleportPoint : MonoBehaviour
{
    [Header("Teleport Point Settings")]
    [Header("Unlock Settings")]
    [Tooltip("Is this teleport point unlocked at the start?")]
    public bool isUnlocked = false; 

    [Header("Player landing point")]
    public Transform spawnLocation; 

    [Header("--- Main Rock Settings ---")]
    public MeshRenderer mainRockRenderer;
    public Material mainLockedMat;
    public Material mainUnlockedMat;

    [Header("--- Base Rock Settings ---")]
    public MeshRenderer baseRockRenderer;
    public Material baseLockedMat;
    public Material baseUnlockedMat;

    [Header("Visual Effects (Particles)")]
    public GameObject unlockedGlow;

    [Header("Icon Sprites")]
    public Sprite lockedIcon;    
    public Sprite unlockedIcon;   

    [Header("Audio")]
    public AudioClip unlockSound; 
    public float unlockVolume = 0.3f;
    private AudioSource audioSource;      

    [Header("Map Icons")]
    public Image mapIcon;        
    
    [Header("Minimap Icon btn")]
    public SpriteRenderer minimapIcon;

    [Header("Interaction (F Key UI)")]
    public GameObject interactPrompt;
    
    private bool isPlayerNear = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (interactPrompt != null) interactPrompt.SetActive(false);

        UpdateVisualState();
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
        
        UpdateVisualState();
        
        if (unlockSound != null) 
        {   
            audioSource.volume = unlockVolume;
            audioSource.PlayOneShot(unlockSound);
        }

        if (interactPrompt != null) interactPrompt.SetActive(false);

        Debug.Log("Point unlocked and minimap updated.");
    }

    void UpdateVisualState()
    {
        if (isUnlocked)
        {
            if (mainRockRenderer != null) mainRockRenderer.material = mainUnlockedMat;
            
            if (baseRockRenderer != null) baseRockRenderer.material = baseUnlockedMat;

            if (unlockedGlow != null) unlockedGlow.SetActive(true);

            if (mapIcon != null && unlockedIcon != null) { mapIcon.sprite = unlockedIcon; mapIcon.color = Color.white; }
            if (minimapIcon != null && unlockedIcon != null) { minimapIcon.sprite = unlockedIcon; minimapIcon.color = Color.white; }
        }
        else
        {
            if (mainRockRenderer != null) mainRockRenderer.material = mainLockedMat;
            
            if (baseRockRenderer != null) baseRockRenderer.material = baseLockedMat;

            if (unlockedGlow != null) unlockedGlow.SetActive(false);

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