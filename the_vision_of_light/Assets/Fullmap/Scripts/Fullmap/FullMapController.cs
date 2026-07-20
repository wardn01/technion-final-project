using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core controller for the full-screen map interface. Handles world-to-UI coordinate mapping,
/// player tracking, and dynamic instantiation of quest markers.
/// Player arrow is drawn as a UI overlay so it stays above teleport/NPC icons on FullMapTexture.
/// </summary>
public class FullMapController : MonoBehaviour
{
    #region Singleton
    public static FullMapController Instance { get; private set; }
    public static Camera FullMapRenderCamera { get; private set; }
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

    [Header("Player Marker Overlay")]
    [Tooltip("Optional. Auto-created under mapContent if empty — keeps player above teleport buttons.")]
    public RectTransform playerMapMarker;

    [Tooltip("UI size of the player arrow on the full map.")]
    public Vector2 playerMarkerSize = new Vector2(70f, 70f);

    [Header("Quest Marker Settings")]
    public GameObject questMarkerPrefab;
    private GameObject currentMarker;

    private SpriteRenderer worldPlayerMapIcon;
    private Image playerMarkerImage;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (fullMapCamera != null)
        {
            FullMapRenderCamera = fullMapCamera.GetComponent<Camera>();
            MapRenderSettings.ApplyToFullMap(FullMapRenderCamera);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            FullMapRenderCamera = null;
        }
    }

    private void Start()
    {
        MapRenderSettings.ApplyToFullMap(FullMapRenderCamera);
        MapRenderSettings.EnsurePlayerMapIconLayer(player);
        CacheWorldPlayerMapIcon();
        EnsurePlayerMapMarker();
        SetFullMapCameraEnabled(false);
    }

    private void LateUpdate()
    {
        if (fullMapScreen == null || !fullMapScreen.activeInHierarchy)
            return;

        UpdatePlayerMapMarker();
    }
    #endregion

    #region Camera Toggle
    /// <summary>Toggles full-map rendering without disabling this manager or MapNightVision on the minimap rig.</summary>
    public void SetFullMapCameraEnabled(bool enabled)
    {
        if (FullMapRenderCamera != null)
            FullMapRenderCamera.enabled = enabled;

        // World-space arrow stays for minimap; hide it on full map so only the UI overlay shows above icons.
        if (worldPlayerMapIcon != null)
            worldPlayerMapIcon.enabled = !enabled;
    }
    #endregion

    #region Map Logic (Public API)
    /// <summary>
    /// Refreshes the map UI state. Must be called immediately after enabling the map panel.
    /// </summary>
    public void RefreshMapUI()
    {
        EnsurePlayerMapMarker();
        CenterMapOnPlayer();
        UpdateMapMarkers();
        UpdatePlayerMapMarker();
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

    /// <summary>
    /// Interacts with the central PauseMenuManager to open the map screen, then focuses on a specific location.
    /// </summary>
    public void OpenMapToPosition(Vector3 worldPosition)
    {
        if (PauseMenuManager.Instance != null && !fullMapScreen.activeSelf)
            PauseMenuManager.Instance.OpenMap();

        CenterMapOnPosition(worldPosition);
    }

    /// <summary>
    /// Instantiates and updates visual markers on the map based on the actively tracked quest data.
    /// </summary>
    public void UpdateMapMarkers()
    {
        if (currentMarker != null) Destroy(currentMarker);

        if (QuestManager.Instance != null
            && QuestManager.Instance.trackedQuest != null
            && QuestManager.Instance.CurrentObjectiveHasTarget())
        {
            Vector3 targetPos = QuestManager.Instance.GetCurrentObjectiveTarget();

            currentMarker = Instantiate(questMarkerPrefab, mapContent);

            RectTransform rt = currentMarker.GetComponent<RectTransform>();
            rt.anchoredPosition = WorldToMapAnchoredPosition(targetPos);
        }

        UpdatePlayerMapMarker();
    }
    #endregion

    #region Player Marker Overlay
    private void CacheWorldPlayerMapIcon()
    {
        if (player == null) return;

        Transform icon = player.Find("PlayerMapIcon");
        if (icon != null)
            worldPlayerMapIcon = icon.GetComponent<SpriteRenderer>();
    }

    private void EnsurePlayerMapMarker()
    {
        if (mapContent == null) return;

        if (playerMapMarker == null)
        {
            Transform existing = mapContent.Find("PlayerMapMarker");
            if (existing != null)
                playerMapMarker = existing as RectTransform;
        }

        if (playerMapMarker == null)
        {
            GameObject go = new GameObject("PlayerMapMarker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            playerMapMarker = go.GetComponent<RectTransform>();
            playerMapMarker.SetParent(mapContent, false);
            playerMapMarker.anchorMin = new Vector2(0.5f, 0.5f);
            playerMapMarker.anchorMax = new Vector2(0.5f, 0.5f);
            playerMapMarker.pivot = new Vector2(0.5f, 0.5f);
            playerMapMarker.sizeDelta = playerMarkerSize;
            playerMapMarker.localScale = Vector3.one;
        }

        playerMarkerImage = playerMapMarker.GetComponent<Image>();
        if (playerMarkerImage == null)
            playerMarkerImage = playerMapMarker.gameObject.AddComponent<Image>();

        if (playerMarkerImage.sprite == null && worldPlayerMapIcon != null)
            playerMarkerImage.sprite = worldPlayerMapIcon.sprite;

        playerMarkerImage.raycastTarget = false;
        playerMarkerImage.preserveAspect = true;
        playerMapMarker.gameObject.SetActive(true);
    }

    private void UpdatePlayerMapMarker()
    {
        if (player == null || mapContent == null || fullMapCamera == null)
            return;

        EnsurePlayerMapMarker();
        if (playerMapMarker == null) return;

        playerMapMarker.anchoredPosition = WorldToMapAnchoredPosition(player.position);
        // Match top-down facing: world Y yaw → UI Z rotation.
        playerMapMarker.localRotation = Quaternion.Euler(0f, 0f, -player.eulerAngles.y);
        playerMapMarker.SetAsLastSibling();
    }

    private Vector2 WorldToMapAnchoredPosition(Vector3 worldPosition)
    {
        float mapSize = mapContent.rect.width;
        float ratio = mapSize / worldSize;

        return new Vector2(
            (worldPosition.x - fullMapCamera.position.x) * ratio,
            (worldPosition.z - fullMapCamera.position.z) * ratio
        );
    }
    #endregion
}
