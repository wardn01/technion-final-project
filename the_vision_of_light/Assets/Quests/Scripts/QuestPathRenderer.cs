using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Genshin-style quest trail: stable ground path along the Road NavMesh, with smoothing and
/// short persistence so the line does not flicker when sampling briefly fails.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class QuestPathRenderer : MonoBehaviour
{
    #region References
    [Header("References")]
    public Transform player;
    #endregion

    #region Settings
    [Header("Settings")]
    [Tooltip("Max distance to draw the path. 0 = no limit (Genshin-style always-on while tracked).")]
    public float activationDistance = 0f;

    [Tooltip("Hide the path when the player is this close to the objective.")]
    public float hideDistance = 3f;

    public float pathHeightOffset = 0.2f;

    [Tooltip("How often to recalculate the NavMesh route.")]
    public float updateInterval = 0.35f;

    [Tooltip("Recalculate immediately when the player moves this far.")]
    public float recalculateMoveThreshold = 4f;
    #endregion

    #region Road Network
    [Header("Road Network")]
    [Tooltip("Guide line follows NavMesh Road areas (Road_Volume segments).")]
    public bool requireRoadPath = true;

    public string roadAreaName = "Road";

    public float playerRoadSampleRadius = 15f;

    public float targetRoadSampleRadius = 25f;

    [Tooltip("Objective can be this far from the nearest road and still get a guide line.")]
    public float maxObjectiveRoadDistance = 25f;

    public float maxObjectiveRoadVerticalDistance = 4f;

    [Tooltip("Start drawing when the player is within this distance of a road.")]
    public float maxPlayerRoadHorizontalDistance = 12f;

    public float maxPlayerRoadVerticalDistance = 6f;

    [Tooltip("Keep the path visible until the player is farther than this from the road (prevents flicker).")]
    public float keepPlayerRoadHorizontalDistance = 18f;

    public float keepPlayerRoadVerticalDistance = 8f;

    [Tooltip("Draw a short segment from the player to the road when they are slightly off-road.")]
    public float extendFromPlayerMinDistance = 0.75f;
    #endregion

    #region Genshin-Style Stability
    [Header("Genshin-Style Stability")]
    [Tooltip("Keep showing the last good path for this long after a recalculation fails.")]
    public float pathPersistDuration = 3f;

    [Tooltip("How quickly path corners slide toward the new route.")]
    public float pathSmoothSpeed = 14f;

    [Tooltip("Allow partial NavMesh paths instead of hiding the line entirely.")]
    public bool allowPartialPaths = true;
    #endregion

    #region Visuals
    [Header("Visuals")]
    public float scrollSpeed = 1.5f;
    public float textureTilingSize = 2f;
    #endregion

    #region Internal State
    private LineRenderer lineRenderer;
    private NavMeshPath navMeshPath;
    private float recalculateTimer;
    private Vector3 lastRecalculatePosition;
    private Vector3[] targetCorners = System.Array.Empty<Vector3>();
    private Vector3[] displayCorners = System.Array.Empty<Vector3>();
    private float lastSuccessfulPathTime = -999f;
    private bool hasCachedPath;
    private int roadAreaMask = -1;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        navMeshPath = new NavMeshPath();
        roadAreaMask = 1 << NavMesh.GetAreaFromName(roadAreaName);

        if (player != null)
            lastRecalculatePosition = player.position;
    }

    private void Update()
    {
        if (!CanShowPath(out Vector3 targetPos))
        {
            ClearPath();
            return;
        }

        if (ShouldRecalculatePath())
            TryRefreshPath(targetPos);

        UpdateDisplayedPath();
        AnimateTexture();
    }
    #endregion

    #region Visibility
    private bool CanShowPath(out Vector3 targetPos)
    {
        targetPos = default;

        if (player == null || QuestManager.Instance == null || !QuestManager.Instance.CurrentObjectiveHasTarget())
            return false;

        if (QuestPathSuppression.IsSuppressed)
            return false;

        targetPos = QuestManager.Instance.GetCurrentObjectiveTarget();
        float distance = Vector3.Distance(player.position, targetPos);

        if (activationDistance > 0f && distance > activationDistance)
            return false;

        if (distance < hideDistance)
            return false;

        return true;
    }

    private bool ShouldRecalculatePath()
    {
        if (!hasCachedPath)
            return true;

        recalculateTimer += Time.deltaTime;

        if (recalculateTimer >= updateInterval)
            return true;

        return Vector3.Distance(player.position, lastRecalculatePosition) >= recalculateMoveThreshold;
    }
    #endregion

    #region Path Building
    private void TryRefreshPath(Vector3 targetPos)
    {
        recalculateTimer = 0f;
        lastRecalculatePosition = player.position;

        if (TryBuildPath(targetPos, out Vector3[] corners, out float pathLength))
        {
            targetCorners = corners;
            lastSuccessfulPathTime = Time.time;
            hasCachedPath = true;
            ApplyTextureScale(pathLength);
            return;
        }

        if (!hasCachedPath || Time.time - lastSuccessfulPathTime > pathPersistDuration)
            ClearPath();
    }

    private bool TryBuildPath(Vector3 targetPos, out Vector3[] corners, out float pathLength)
    {
        corners = System.Array.Empty<Vector3>();
        pathLength = 0f;

        int areaMask = requireRoadPath ? roadAreaMask : NavMesh.AllAreas;
        bool useRelaxedPlayerRoadCheck =
            hasCachedPath && Time.time - lastSuccessfulPathTime < pathPersistDuration;

        Vector3 pathStart = player.position;
        Vector3 roadStart = pathStart;
        bool extendFromPlayer = false;

        if (requireRoadPath)
        {
            if (!TryGetPlayerRoadPosition(useRelaxedPlayerRoadCheck, out roadStart))
                return false;

            pathStart = roadStart;
            extendFromPlayer = HorizontalDistance(player.position, roadStart) > extendFromPlayerMinDistance;
        }

        Vector3 pathEnd = targetPos;
        bool extendPathToObjective = false;

        if (requireRoadPath)
        {
            if (!TryGetObjectiveRoadPosition(targetPos, out pathEnd, out extendPathToObjective))
                return false;
        }

        if (!NavMesh.CalculatePath(pathStart, pathEnd, areaMask, navMeshPath))
            return false;

        if (navMeshPath.status == NavMeshPathStatus.PathInvalid)
            return false;

        if (!allowPartialPaths && navMeshPath.status != NavMeshPathStatus.PathComplete)
            return false;

        int extraStart = extendFromPlayer ? 1 : 0;
        int cornerCount = navMeshPath.corners.Length + extraStart + (extendPathToObjective ? 1 : 0);
        if (cornerCount < 2)
            return false;

        corners = new Vector3[cornerCount];
        Vector3 previousPoint = default;
        int writeIndex = 0;

        if (extendFromPlayer)
        {
            Vector3 playerPoint = player.position + (Vector3.up * pathHeightOffset);
            corners[writeIndex++] = playerPoint;
            previousPoint = playerPoint;
        }

        for (int i = 0; i < navMeshPath.corners.Length; i++)
        {
            Vector3 point = navMeshPath.corners[i] + (Vector3.up * pathHeightOffset);
            corners[writeIndex++] = point;

            if (writeIndex > 1)
                pathLength += Vector3.Distance(previousPoint, point);

            previousPoint = point;
        }

        if (extendPathToObjective)
        {
            Vector3 objectivePoint = targetPos + (Vector3.up * pathHeightOffset);
            corners[writeIndex] = objectivePoint;
            pathLength += Vector3.Distance(previousPoint, objectivePoint);
        }

        return true;
    }

    private void UpdateDisplayedPath()
    {
        if (!hasCachedPath || targetCorners.Length < 2)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        if (displayCorners.Length != targetCorners.Length)
            displayCorners = new Vector3[targetCorners.Length];

        bool needsSnap = lineRenderer.positionCount != targetCorners.Length;
        float smoothStep = needsSnap ? 1f : pathSmoothSpeed * Time.deltaTime;

        for (int i = 0; i < targetCorners.Length; i++)
        {
            if (needsSnap || displayCorners.Length <= i)
                displayCorners[i] = targetCorners[i];
            else
                displayCorners[i] = Vector3.Lerp(displayCorners[i], targetCorners[i], smoothStep);
        }

        lineRenderer.positionCount = displayCorners.Length;
        for (int i = 0; i < displayCorners.Length; i++)
            lineRenderer.SetPosition(i, displayCorners[i]);
    }

    private void ClearPath()
    {
        hasCachedPath = false;
        targetCorners = System.Array.Empty<Vector3>();
        displayCorners = System.Array.Empty<Vector3>();
        lineRenderer.positionCount = 0;
    }

    private void ApplyTextureScale(float pathLength)
    {
        if (lineRenderer.material != null && textureTilingSize > 0f)
            lineRenderer.material.mainTextureScale = new Vector2(pathLength / textureTilingSize, 1f);
    }

    private void AnimateTexture()
    {
        if (lineRenderer.positionCount > 0 && lineRenderer.material != null)
        {
            float offset = Time.time * scrollSpeed;
            lineRenderer.material.mainTextureOffset = new Vector2(-offset, 0);
        }
    }
    #endregion

    #region Road Sampling
    private bool TryGetObjectiveRoadPosition(Vector3 targetPos, out Vector3 roadPosition, out bool extendToObjective)
    {
        extendToObjective = false;

        if (!TrySampleRoadPosition(targetPos, targetRoadSampleRadius, out roadPosition))
            return false;

        float horizontalDistance = HorizontalDistance(targetPos, roadPosition);
        float verticalDistance = Mathf.Abs(targetPos.y - roadPosition.y);

        if (horizontalDistance > maxObjectiveRoadDistance ||
            verticalDistance > maxObjectiveRoadVerticalDistance)
            return false;

        extendToObjective = horizontalDistance > 0.75f;
        return true;
    }

    private bool TryGetPlayerRoadPosition(bool relaxed, out Vector3 roadPosition)
    {
        if (!TrySampleRoadPosition(player.position, playerRoadSampleRadius, out roadPosition))
            return false;

        float horizontalDistance = HorizontalDistance(player.position, roadPosition);
        float verticalDistance = Mathf.Abs(player.position.y - roadPosition.y);

        float maxHorizontal = relaxed ? keepPlayerRoadHorizontalDistance : maxPlayerRoadHorizontalDistance;
        float maxVertical = relaxed ? keepPlayerRoadVerticalDistance : maxPlayerRoadVerticalDistance;

        return horizontalDistance <= maxHorizontal && verticalDistance <= maxVertical;
    }

    private bool TrySampleRoadPosition(Vector3 worldPosition, float sampleRadius, out Vector3 roadPosition)
    {
        if (NavMesh.SamplePosition(worldPosition, out NavMeshHit hit, sampleRadius, roadAreaMask))
        {
            roadPosition = hit.position;
            return true;
        }

        roadPosition = default;
        return false;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
    #endregion
}
