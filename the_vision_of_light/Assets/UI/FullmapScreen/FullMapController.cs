using UnityEngine;

/// <summary>
/// Handles the logic for the full screen map, including player centering, quest markers, and scaling.
/// </summary>
public class FullMapController : MonoBehaviour
{
    #region Singleton
    public static FullMapController Instance { get; private set; }
    #endregion

    #region References & Settings
    [Header("Full Map UI")]
    public GameObject fullMapScreen; 
    public RectTransform mapContent;

    [Header("Player Settings")]
    public Transform player;
    public Transform fullMapCamera; 
    public float worldSize = 1000f; 
    public float startZoom = 1.5f;

    [Header("Quest Marker Settings")]
    public GameObject questMarkerPrefab;
    private GameObject currentMarker;
    #endregion

    private bool wasMapOpen = false; 

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (fullMapCamera != null)
        {
            fullMapCamera.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (fullMapScreen == null) return;

        bool isMapOpen = fullMapScreen.activeSelf;
        
        // Detect if map was just opened
        if (!wasMapOpen && isMapOpen)
        {
            CenterMapOnPlayer(); 
            UpdateMapMarkers();
        }

        wasMapOpen = isMapOpen;
    }
    #endregion

    #region Map Logic (Centering & Markers)
    public void CenterMapOnPlayer()
    {
        if (player != null) CenterMapOnPosition(player.position);
    }

    /// <summary>
    /// Calculates the correct anchored position to center the map UI on a specific world coordinate.
    /// </summary>
    public void CenterMapOnPosition(Vector3 worldPosition)
    {
        if (mapContent == null || fullMapCamera == null) return;

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
        if (PauseMenuManager.Instance != null && !fullMapScreen.activeSelf)
        {
            PauseMenuManager.Instance.OpenMap();
        }
        
        CenterMapOnPosition(worldPosition);
    }

    /// <summary>
    /// Instantiates and positions the quest marker on the map based on the active tracked quest.
    /// </summary>
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
    #endregion
}