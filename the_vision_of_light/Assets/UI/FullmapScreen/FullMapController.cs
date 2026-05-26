using UnityEngine;

public class FullMapController : MonoBehaviour
{
    public static FullMapController Instance { get; private set; }

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

    [Header("Quest Marker Settings")]
    public GameObject questMarkerPrefab;
    private GameObject currentMarker;

    private bool wasMapOpen = false; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

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
            UpdateMapMarkers();
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
        CenterMapOnPosition(player.position);
    }

    public void CenterMapOnPosition(Vector3 worldPosition)
    {
        mapContent.localScale = Vector3.one * startZoom;
        float mapSize = mapContent.rect.width; 
        float ratio = mapSize / worldSize;

        float targetX = -(worldPosition.x - fullMapCamera.position.x) * ratio * startZoom;
        float targetY = -(worldPosition.z - fullMapCamera.position.z) * ratio * startZoom;

        Canvas.ForceUpdateCanvases(); 
        mapContent.anchoredPosition = new Vector2(targetX, targetY);
    }

    public void OpenMapToPosition(Vector3 worldPosition)
    {
        if (!fullMapScreen.activeSelf)
        {
            ToggleMap();
        }
        CenterMapOnPosition(worldPosition);
    }

    public void UpdateMapMarkers()
    {
        if (currentMarker != null) Destroy(currentMarker);

        if (QuestManager.Instance != null && QuestManager.Instance.trackedQuest != null)
        {
            QuestData tracked = QuestManager.Instance.trackedQuest;
            
            if (tracked.hasTargetLocation)
            {
                currentMarker = Instantiate(questMarkerPrefab, mapContent);
                
                float mapSize = mapContent.rect.width;
                float ratio = mapSize / worldSize;
                
                RectTransform rt = currentMarker.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(
                    (tracked.targetLocation.x - fullMapCamera.position.x) * ratio,
                    (tracked.targetLocation.z - fullMapCamera.position.z) * ratio
                );
            }
        }
    }

    public void PlayTeleportSound()
    {
        if (teleportClickSound != null && uiAudioSource != null)
        {
            uiAudioSource.PlayOneShot(teleportClickSound, teleportClickVolume);
        }
    }
}