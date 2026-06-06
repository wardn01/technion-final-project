using UnityEngine;

/// <summary>
/// Core controller for the full-screen map interface. Handles world-to-UI coordinate mapping, 
/// player tracking, and dynamic instantiation of quest markers.
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

    #region Unity Lifecycle
    /// <summary>
    /// Initializes the singleton instance for global access.
    /// </summary>
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Disables the dedicated map camera on startup to conserve rendering performance.
    /// </summary>
    private void Start()
    {
        if (fullMapCamera != null)
        {
            fullMapCamera.gameObject.SetActive(false);
        }
    }
    #endregion

    #region Map Logic (Public API)
    /// <summary>
    /// Refreshes the map UI state. Must be called immediately after enabling the map panel.
    /// </summary>
    public void RefreshMapUI()
    {
        CenterMapOnPlayer();
        UpdateMapMarkers();
    }

    /// <summary>
    /// Snaps the map's focal point directly to the player's current world coordinates.
    /// </summary>
    public void CenterMapOnPlayer()
    {
        if (player != null) CenterMapOnPosition(player.position);
    }

    /// <summary>
    /// Translates a 3D world coordinate into a 2D UI anchored position and centers the map content on it.
    /// </summary>
    /// <param name="worldPosition">The target world coordinate to center the map on.</param>
    public void CenterMapOnPosition(Vector3 worldPosition)
    {
        if (mapContent == null || fullMapCamera == null) return;

        // Apply initial scale/zoom factor
        mapContent.localScale = Vector3.one * startZoom;
        
        float mapSize = mapContent.rect.width; 
        float ratio = mapSize / worldSize;

        // Calculate anchored offset based on camera position and scale ratio
        float targetX = -(worldPosition.x - fullMapCamera.position.x) * ratio * startZoom;
        float targetY = -(worldPosition.z - fullMapCamera.position.z) * ratio * startZoom;

        // Force canvas layout rebuild to apply position changes without visual delay
        Canvas.ForceUpdateCanvases(); 
        mapContent.anchoredPosition = new Vector2(targetX, targetY);
    }

    /// <summary>
    /// Interacts with the central PauseMenuManager to open the map screen, then focuses on a specific location.
    /// </summary>
    /// <param name="worldPosition">The specific world location to focus the map on.</param>
    public void OpenMapToPosition(Vector3 worldPosition)
    {
        if (PauseMenuManager.Instance != null && !fullMapScreen.activeSelf)
        {
            PauseMenuManager.Instance.OpenMap();
        }
        
        CenterMapOnPosition(worldPosition);
    }

    /// <summary>
    /// Instantiates and updates visual markers on the map based on the actively tracked quest data.
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
                
                // Convert world space target to map UI space
                rt.anchoredPosition = new Vector2(
                    (tracked.targetLocation.x - fullMapCamera.position.x) * ratio,
                    (tracked.targetLocation.z - fullMapCamera.position.z) * ratio
                );
            }
        }
    }
    #endregion
}