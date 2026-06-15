using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Scroll zoom, obstacle pull-in, and combat camera follow for the player Cinemachine rig.
/// Runs after CinemachineBrain (execution order 250) and corrects any lag during attack root motion.
/// </summary>
[DefaultExecutionOrder(250)]
public class CameraZoom : MonoBehaviour
{
    [Header("Camera Setup")]
    public CinemachineCamera playerCam;
    private CinemachineOrbitalFollow orbitalFollow;
    private CinemachineRotationComposer rotationComposer;
    private Transform followTarget;
    private PlayerCombat playerCombat;

    [Header("Scroll Zoom Settings")]
    public float zoomSpeed = 0.5f;
    public float minZoom = 0.1f;
    public float maxZoom = 1.2f;

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayers;
    public Vector3 targetOffset = new Vector3(0, 0f, 0);
    public float autoZoomSpeed = 15f;

    [Header("Combat Follow")]
    [Tooltip("Zero = camera snaps with the player during combos and skill root motion.")]
    public Vector3 combatPositionDamping = Vector3.zero;

    private float targetDistance;
    private Vector3 defaultPositionDamping;
    private Vector2 defaultComposerDamping;
    private bool defaultsCached;
    private Vector3 lastFollowTargetPos;
    private Vector3 lastCameraPos;
    private bool followTargetInitialized;
    private bool wasTightFollow;

    private void Start()
    {
        if (playerCam == null) playerCam = GetComponent<CinemachineCamera>();

        if (playerCam != null)
        {
            orbitalFollow = playerCam.GetComponent<CinemachineOrbitalFollow>();
            rotationComposer = playerCam.GetComponent<CinemachineRotationComposer>();
            followTarget = ResolveFollowTarget();

            if (orbitalFollow != null)
            {
                targetDistance = orbitalFollow.RadialAxis.Value;
                defaultPositionDamping = orbitalFollow.TrackerSettings.PositionDamping;
                defaultsCached = true;
            }

            if (rotationComposer != null)
                defaultComposerDamping = rotationComposer.Damping;
        }

        if (followTarget != null)
        {
            Transform playerRoot = followTarget.parent != null ? followTarget.parent : followTarget;
            playerCombat = playerRoot.GetComponentInChildren<PlayerCombat>();
        }
    }

    private void Update()
    {
        ApplyCombatDamping();
    }

    private void LateUpdate()
    {
        if (orbitalFollow == null || followTarget == null) return;

        if (playerCombat != null && !playerCombat.inCombatStance)
        {
            followTargetInitialized = false;
            wasTightFollow = false;
        }

        bool tightFollow = playerCombat != null && playerCombat.RequiresCombatCameraFollow();
        if (wasTightFollow != tightFollow)
            SyncFollowReferences();

        wasTightFollow = tightFollow;

        ApplyCombatDamping();
        CorrectCombatCameraLag();

        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0)
        {
            targetDistance -= scroll * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minZoom, maxZoom);
        }

        float currentV = orbitalFollow.RadialAxis.Value;
        Vector3 rayOrigin = followTarget.position + targetOffset;
        Vector3 camPos = playerCam.transform.position;
        float currentDistanceInMeters = Vector3.Distance(rayOrigin, camPos);

        float metersPerUnit = (currentV > 0.05f) ? (currentDistanceInMeters / currentV) : currentDistanceInMeters;
        if (metersPerUnit < 0.1f) metersPerUnit = 1f;

        Vector3 camDirection = (camPos - rayOrigin).normalized;
        float expectedMeters = targetDistance * metersPerUnit;
        Vector3 expectedCamPos = rayOrigin + (camDirection * expectedMeters);

        float finalDistanceValue = targetDistance;

        if (Physics.Linecast(rayOrigin, expectedCamPos, out RaycastHit hit, obstacleLayers))
        {
            float safeMeters = Mathf.Max(0.3f, hit.distance - 0.2f);
            finalDistanceValue = safeMeters / metersPerUnit;
        }

        orbitalFollow.RadialAxis.Value = Mathf.Lerp(
            orbitalFollow.RadialAxis.Value,
            finalDistanceValue,
            Time.deltaTime * autoZoomSpeed);
    }

    private Transform ResolveFollowTarget()
    {
        if (playerCam == null) return null;
        if (playerCam.Follow != null) return playerCam.Follow;
        return playerCam.Target.TrackingTarget;
    }

    private void ApplyCombatDamping()
    {
        if (!defaultsCached || playerCombat == null) return;

        bool tightFollow = playerCombat.RequiresCombatCameraFollow();

        var settings = orbitalFollow.TrackerSettings;
        settings.PositionDamping = tightFollow ? combatPositionDamping : defaultPositionDamping;
        orbitalFollow.TrackerSettings = settings;

        if (rotationComposer != null)
            rotationComposer.Damping = tightFollow ? Vector2.zero : defaultComposerDamping;
    }

    /// <summary>
    /// After Cinemachine updates, apply only the follow lag (target movement minus camera movement).
    /// Fixes cumulative drift on Attack_2 / Attack_3 combo root motion.
    /// </summary>
    private void CorrectCombatCameraLag()
    {
        if (playerCombat == null || !playerCombat.RequiresCombatCameraFollow()) return;

        Vector3 targetPos = followTarget.position;
        Vector3 cameraPos = playerCam.transform.position;

        if (!followTargetInitialized)
        {
            lastFollowTargetPos = targetPos;
            lastCameraPos = cameraPos;
            followTargetInitialized = true;
            return;
        }

        Vector3 targetDelta = targetPos - lastFollowTargetPos;
        Vector3 cameraDelta = cameraPos - lastCameraPos;
        Vector3 correction = targetDelta - cameraDelta;

        if (correction.sqrMagnitude > 0.000001f)
            playerCam.transform.position += correction;

        lastFollowTargetPos = targetPos;
        lastCameraPos = playerCam.transform.position;
    }

    private void SyncFollowReferences()
    {
        if (followTarget == null || playerCam == null) return;

        lastFollowTargetPos = followTarget.position;
        lastCameraPos = playerCam.transform.position;
        followTargetInitialized = true;
    }
}
