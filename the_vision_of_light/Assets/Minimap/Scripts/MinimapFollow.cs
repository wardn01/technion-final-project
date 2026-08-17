using UnityEngine;

/// <summary>
/// Top-down minimap camera rig. Follows the player and exposes a static reference for icon scripts.
/// </summary>
public class MinimapFollow : MonoBehaviour
{
    #region Static Accessors
    public static Transform CameraTransform { get; private set; }
    public static Camera RenderCamera { get; private set; }
    #endregion

    #region Serialized Fields
    public Transform player;
    public Transform playerCamera;
    public float height = 500f;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        CameraTransform = transform;
        RenderCamera = GetComponent<Camera>();
        MapRenderSettings.ApplyToMinimap(RenderCamera);
    }

    private void Start()
    {
        MapRenderSettings.ApplyToMinimap(RenderCamera);
        MapRenderSettings.EnsurePlayerMapIconLayer(player);
    }

    private void OnDestroy()
    {
        if (CameraTransform == transform)
        {
            CameraTransform = null;
            RenderCamera = null;
        }
    }
    #endregion

    #region Camera Follow
    private void LateUpdate()
    {
        if (player == null || playerCamera == null) return;

        transform.position = new Vector3(player.position.x, player.position.y + height, player.position.z);
        transform.rotation = Quaternion.Euler(90f, playerCamera.eulerAngles.y, 0f);
    }
    #endregion
}
