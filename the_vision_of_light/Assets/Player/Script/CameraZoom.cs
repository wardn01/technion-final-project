using UnityEngine;
using Unity.Cinemachine;

public class CameraZoom : MonoBehaviour
{
    [Header("Camera Setup")]
    public CinemachineCamera playerCam; 
    private CinemachineOrbitalFollow orbitalFollow;

    [Header("Zoom Settings")]
    public float zoomSpeed = 0.1f;
    public float minZoom = 0.3f;
    public float maxZoom = 2f;

    void Start()
    {
        if (playerCam == null) playerCam = GetComponent<CinemachineCamera>();
        
        if (playerCam != null)
        {
            orbitalFollow = playerCam.GetComponent<CinemachineOrbitalFollow>();
        }
    }

    void Update()
    {
        if (orbitalFollow != null)
        {
            float scroll = Input.mouseScrollDelta.y;

            if (scroll != 0)
            {
                orbitalFollow.RadialAxis.Value -= scroll * zoomSpeed;

                orbitalFollow.RadialAxis.Value = Mathf.Clamp(orbitalFollow.RadialAxis.Value, minZoom, maxZoom);
            }
        }
    }
}