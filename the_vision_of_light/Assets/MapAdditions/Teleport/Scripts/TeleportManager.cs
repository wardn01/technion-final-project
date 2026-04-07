using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class TeleportManager : MonoBehaviour
{   
    [Header("Teleportation Settings")]
    [Header("Settings")]
    public float loadingDuration = 1.5f;

    [Header("Player")]
    public Transform player;  

    [Header("UI Elements")]       
    public GameObject fullMapScreen; 
    public GameObject loadingScreen;

    [Header("Confirmation UI")]
    public GameObject teleportConfirmPanel;
    public GameObject mapSelectionGlow;
    private TeleportPoint selectedDestination;

    [Header("Audio")]
    public AudioClip teleportSound;
    public float teleportVolume = 0.2f;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (teleportConfirmPanel != null) teleportConfirmPanel.SetActive(false);
        if (mapSelectionGlow != null) mapSelectionGlow.SetActive(false);
    }

    public void SelectTeleportPoint(TeleportPoint destination)
    {
        if (destination.isUnlocked)
        {
            selectedDestination = destination;
            
            if (teleportConfirmPanel != null) teleportConfirmPanel.SetActive(true);

            if (mapSelectionGlow != null)
            {
                mapSelectionGlow.SetActive(true);
                
                GameObject clickedButton = EventSystem.current.currentSelectedGameObject;
                if (clickedButton != null)
                {
                    mapSelectionGlow.transform.SetParent(clickedButton.transform.parent, false);

                    mapSelectionGlow.transform.position = clickedButton.transform.position;

                    mapSelectionGlow.transform.SetAsFirstSibling(); 
                }
            }
        }
    }

    public void ConfirmTeleport()
    {
        if (selectedDestination != null)
        {
            if (teleportConfirmPanel != null) teleportConfirmPanel.SetActive(false);
            if (mapSelectionGlow != null) mapSelectionGlow.SetActive(false); 
            
            StartCoroutine(TeleportSequence(selectedDestination));
        }
    }

    public void CancelSelection()
    {
        selectedDestination = null;
        if (teleportConfirmPanel != null) teleportConfirmPanel.SetActive(false);
        if (mapSelectionGlow != null) mapSelectionGlow.SetActive(false);
    }

    private IEnumerator TeleportSequence(TeleportPoint destination)
    {
        if (teleportSound != null) {
            audioSource.volume = teleportVolume;
            audioSource.PlayOneShot(teleportSound);
        }

        if (fullMapScreen != null) fullMapScreen.SetActive(false);
        if (loadingScreen != null) loadingScreen.SetActive(true);

        yield return new WaitForSecondsRealtime(loadingDuration);

        Vector3 targetPosition;
        if (destination.spawnLocation != null) targetPosition = destination.spawnLocation.position;
        else targetPosition = destination.transform.position + new Vector3(2f, 1f, 0f);

        CharacterController cc = player.GetComponent<CharacterController>();
        Rigidbody rb = player.GetComponent<Rigidbody>();

        if (cc != null) 
        {
            cc.enabled = false;          
            player.position = targetPosition; 
            cc.enabled = true;           
        }
        else 
        {
            player.position = targetPosition;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        player.SendMessage("ResetVelocity", SendMessageOptions.DontRequireReceiver);
        player.SendMessage("ResetFallDamage", SendMessageOptions.DontRequireReceiver);
        player.SendMessage("CancelAttack", SendMessageOptions.DontRequireReceiver);

        if (loadingScreen != null) loadingScreen.SetActive(false);
        
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        selectedDestination = null;
    }
}