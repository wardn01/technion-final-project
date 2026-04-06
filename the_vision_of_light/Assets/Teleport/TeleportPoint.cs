using UnityEngine;
using UnityEngine.UI;

public class TeleportPoint : MonoBehaviour
{
    public bool isUnlocked = false; 

    public Transform spawnLocation; 

    public Material lockedMat;         
    public Material unlockedMat;       
    public MeshRenderer centerCrystal; 
    public MeshRenderer baseCrystal;   

    public GameObject darkGlow;        
    public GameObject blueGlow;        

    public Image mapIcon;        
    
    public SpriteRenderer minimapIcon;

    public Sprite lockedIcon;    
    public Sprite unlockedIcon;  

    public AudioClip unlockSound; 
    private AudioSource audioSource; 

    private bool isPlayerNear = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (centerCrystal != null) centerCrystal.material = lockedMat;
        if (baseCrystal != null) baseCrystal.material = lockedMat; 
        if (darkGlow != null) darkGlow.SetActive(true);
        if (blueGlow != null) blueGlow.SetActive(false);

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

    void Update()
    {
        if (isPlayerNear == true && isUnlocked == false && Input.GetKeyDown(KeyCode.F))
        {
            UnlockPoint();
        }
    }

    void UnlockPoint()
    {
        isUnlocked = true;
        
        if (centerCrystal != null) centerCrystal.material = unlockedMat;
        if (baseCrystal != null) baseCrystal.material = unlockedMat; 
        if (darkGlow != null) darkGlow.SetActive(false);
        if (blueGlow != null) blueGlow.SetActive(true);

        if (mapIcon != null && unlockedIcon != null) mapIcon.sprite = unlockedIcon;
        if (minimapIcon != null && unlockedIcon != null) minimapIcon.sprite = unlockedIcon;
        
        if (unlockSound != null) 
        {   
            audioSource.volume = 0.3f;
            audioSource.PlayOneShot(unlockSound);
        }
        Debug.Log("Point unlocked and minimap updated.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerNear = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerNear = false;
    }
}