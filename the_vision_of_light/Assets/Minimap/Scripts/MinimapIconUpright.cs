using UnityEngine;

/// <summary>
/// Keeps world-space minimap icons upright relative to the minimap camera yaw.
/// </summary>
public class MinimapIconUpright : MonoBehaviour
{
    private Transform minimapCamera;

    private void Awake()
    {
        minimapCamera = MinimapFollow.CameraTransform;
    }

    private void LateUpdate()
    {
        if (minimapCamera == null)
            minimapCamera = MinimapFollow.CameraTransform;

        if (minimapCamera == null) return;

        transform.rotation = Quaternion.Euler(90f, minimapCamera.eulerAngles.y, 0f);
    }
}
