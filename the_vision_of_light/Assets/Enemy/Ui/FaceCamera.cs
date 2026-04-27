using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    public Camera myTargetCamera;

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
}