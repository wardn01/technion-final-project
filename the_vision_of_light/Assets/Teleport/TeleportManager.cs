using UnityEngine;
using System.Collections;

public class TeleportManager : MonoBehaviour
{
    public Transform player;         
    public GameObject fullMapScreen; 

    public GameObject loadingScreen;

    public AudioClip teleportSound;
    private AudioSource audioSource;

    void Start()
    {

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void TeleportToPoint(TeleportPoint destination)
    {
        if (destination.isUnlocked)
        {
            StartCoroutine(TeleportSequence(destination));
        }
        else
        {
            Debug.Log("Teleport locked.");
        }
    }

    private IEnumerator TeleportSequence(TeleportPoint destination)
    {
        if (teleportSound != null) 
        {   
            audioSource.volume = 0.2f;
            audioSource.PlayOneShot(teleportSound);
        }

        if (fullMapScreen != null) fullMapScreen.SetActive(false);
        if (loadingScreen != null) loadingScreen.SetActive(true);

        yield return new WaitForSecondsRealtime(1f);

        Vector3 targetPosition;
        if (destination.spawnLocation != null) targetPosition = destination.spawnLocation.position;
        else targetPosition = destination.transform.position + new Vector3(2f, 1f, 0f);

        CharacterController cc = player.GetComponent<CharacterController>();
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

        if (loadingScreen != null) loadingScreen.SetActive(false);
        
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("Teleport successful.");
    }
}