using UnityEngine;

/// <summary>
/// Keeps a world-space UI element facing the active camera each frame.
/// </summary>
public class FaceCamera : MonoBehaviour
{
    public Camera myTargetCamera;

    #region Unity Lifecycle
    private void Start()
    {
        if (myTargetCamera == null)
        {
            myTargetCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (myTargetCamera != null)
        {
            transform.forward = myTargetCamera.transform.forward;
        }
    }
    #endregion
}
