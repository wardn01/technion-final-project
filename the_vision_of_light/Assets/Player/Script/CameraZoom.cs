using UnityEngine;
using Unity.Cinemachine;

public class CameraZoom : MonoBehaviour
{
    [Header("Camera Setup")]
    public CinemachineCamera playerCam; 
    private CinemachineOrbitalFollow orbitalFollow;

    [Header("Scroll Zoom Settings")]
    public float zoomSpeed = 0.5f;
    public float minZoom = 0.1f;
    public float maxZoom = 1.2f;

    [Header("Obstacle Avoidance (Genshin Style)")]
    public LayerMask obstacleLayers; 
    public Vector3 targetOffset = new Vector3(0, 0f, 0); 
    public float autoZoomSpeed = 15f; 

    private float targetDistance; 

    void Start()
    {
        if (playerCam == null) playerCam = GetComponent<CinemachineCamera>();
        
        if (playerCam != null)
        {
            orbitalFollow = playerCam.GetComponent<CinemachineOrbitalFollow>();
            if (orbitalFollow != null)
            {
                targetDistance = orbitalFollow.RadialAxis.Value;
            }
        }
    }

    void LateUpdate() 
    {
        if (orbitalFollow == null || playerCam.Follow == null) return;

        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0)
        {
            targetDistance -= scroll * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minZoom, maxZoom);
        }

        float currentV = orbitalFollow.RadialAxis.Value;
        Vector3 rayOrigin = playerCam.Follow.position + targetOffset;
        Vector3 camPos = playerCam.transform.position;
        float currentDistanceInMeters = Vector3.Distance(rayOrigin, camPos);
        
        float metersPerUnit = (currentV > 0.05f) ? (currentDistanceInMeters / currentV) : currentDistanceInMeters;
        if (metersPerUnit < 0.1f) metersPerUnit = 1f;

        Vector3 camDirection = (camPos - rayOrigin).normalized;
        float expectedMeters = targetDistance * metersPerUnit;
        Vector3 expectedCamPos = rayOrigin + (camDirection * expectedMeters);

        float finalDistanceValue = targetDistance;

        Debug.DrawLine(rayOrigin, expectedCamPos, Color.yellow);

        if (Physics.Linecast(rayOrigin, expectedCamPos, out RaycastHit hit, obstacleLayers))
        {
            float safeMeters = Mathf.Max(0.3f, hit.distance - 0.2f);
            finalDistanceValue = safeMeters / metersPerUnit;

            Debug.DrawLine(rayOrigin, hit.point, Color.red);
        }

        orbitalFollow.RadialAxis.Value = Mathf.Lerp(orbitalFollow.RadialAxis.Value, finalDistanceValue, Time.deltaTime * autoZoomSpeed);
    }
}