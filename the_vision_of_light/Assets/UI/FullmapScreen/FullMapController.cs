using UnityEngine;

public class FullMapController : MonoBehaviour
{
    [Header("Full Map UI")]
    public GameObject fullMapScreen; 
    public RectTransform mapContent;

    [Header("UI Elements to Hide")] 
    public GameObject[] uiElementsToHide;

    [Header("Player Settings")]
    public Transform player;
    public Transform fullMapCamera; 
    public float worldSize = 1000f; 
    public float startZoom = 1.5f;

    [Header("Map Sounds")]
    public AudioClip teleportClickSound;
    public float teleportClickVolume = 0.3f;
    private AudioSource uiAudioSource;

    private bool wasMapOpen = false; 

    void Start()
    {
        if (fullMapCamera != null)
        {
            fullMapCamera.gameObject.SetActive(false);
        }
        uiAudioSource = gameObject.AddComponent<AudioSource>();
        uiAudioSource.spatialBlend = 0f;
        uiAudioSource.playOnAwake = false;
    }

    void Update()
    {
        bool isMapOpen = fullMapScreen.activeSelf;

        if (wasMapOpen && !isMapOpen)
        {
            ToggleUIElements(true);
        }
        wasMapOpen = isMapOpen;
    }

    public void ToggleMap()
    {

        if (Time.timeScale == 0f && !fullMapScreen.activeSelf) return;

        bool isOpen = fullMapScreen.activeSelf;
        fullMapScreen.SetActive(!isOpen);

        if (!isOpen) 
        {
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            fullMapCamera.gameObject.SetActive(true); 
            ToggleUIElements(false);
            CenterMapOnPlayer(); 
        }
        else 
        {
            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            fullMapCamera.gameObject.SetActive(false); 
            ToggleUIElements(true);
        }
    }

    private void ToggleUIElements(bool show)
    {
        foreach (GameObject element in uiElementsToHide)
        {
            if (element != null)
            {
                element.SetActive(show);
            }
        }
    }

    void CenterMapOnPlayer()
    {
        mapContent.localScale = Vector3.one * startZoom;
        float mapSize = mapContent.rect.width; 
        float ratio = mapSize / worldSize;

        float targetX = -(player.position.x - fullMapCamera.position.x) * ratio * startZoom;
        float targetY = -(player.position.z - fullMapCamera.position.z) * ratio * startZoom;

        Canvas.ForceUpdateCanvases(); 
        mapContent.anchoredPosition = new Vector2(targetX, targetY);
    }

    public void PlayTeleportSound()
    {
        if (teleportClickSound != null && uiAudioSource != null)
        {
            uiAudioSource.PlayOneShot(teleportClickSound, teleportClickVolume);
        }
    }
}