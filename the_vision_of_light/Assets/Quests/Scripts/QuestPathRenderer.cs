using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Draws a navigable "guiding line" on the ground from the player to the tracked quest's objective,
/// using the NavMesh to follow walkable terrain. The path appears only within a distance band and
/// its texture scrolls/tiles to suggest direction. Recalculated on a fixed interval for performance.
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
    /// <summary>Maximum distance to the objective at which the path is drawn.</summary>
    public float activationDistance = 100f;

    /// <summary>Distance below which the path hides (player is essentially at the objective).</summary>
    public float hideDistance = 2f;

    /// <summary>Vertical offset so the line floats slightly above the ground.</summary>
    public float pathHeightOffset = 0.2f;

    /// <summary>Seconds between path recalculations.</summary>
    public float updateInterval = 0.5f;
    #endregion

    #region Visuals
    [Header("Visuals")]
    /// <summary>Texture scroll speed used to animate flow toward the objective.</summary>
    public float scrollSpeed = 1.5f;

    /// <summary>World length represented by one texture tile.</summary>
    public float textureTilingSize = 2f;
    #endregion

    #region Internal State
    private LineRenderer lineRenderer;
    private NavMeshPath path;
    private float timer = 0f;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        path = new NavMeshPath();
    }

    /// <summary>
    /// Clears the line when there is no valid tracked objective or the player is out of the distance
    /// band; otherwise recalculates the path on an interval and animates the texture scroll.
    /// </summary>
    private void Update()
    {
        if (player == null || QuestManager.Instance == null || !QuestManager.Instance.CurrentObjectiveHasTarget())
        {
            lineRenderer.positionCount = 0;
            return;
        }

        if (QuestPathSuppression.IsSuppressed)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        Vector3 targetPos = QuestManager.Instance.GetCurrentObjectiveTarget();
        float distance = Vector3.Distance(player.position, targetPos);

        if (distance > activationDistance || distance < hideDistance)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            DrawPath(targetPos);
        }

        if (lineRenderer.positionCount > 0 && lineRenderer.material != null)
        {
            float offset = Time.time * scrollSpeed;
            lineRenderer.material.mainTextureOffset = new Vector2(-offset, 0);
        }
    }
    #endregion

    #region Path Building
    /// <summary>
    /// Computes a NavMesh path to the target and feeds its corners into the line renderer, scaling
    /// the texture by the total path length so tiling stays consistent.
    /// </summary>
    private void DrawPath(Vector3 targetPos)
    {
        if (NavMesh.CalculatePath(player.position, targetPos, NavMesh.AllAreas, path))
        {
            lineRenderer.positionCount = path.corners.Length;
            float pathLength = 0f;

            for (int i = 0; i < path.corners.Length; i++)
            {
                lineRenderer.SetPosition(i, path.corners[i] + (Vector3.up * pathHeightOffset));

                if (i > 0)
                {
                    pathLength += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                }
            }

            if (lineRenderer.material != null && textureTilingSize > 0)
            {
                lineRenderer.material.mainTextureScale = new Vector2(pathLength / textureTilingSize, 1f);
            }
        }
        else
        {
            lineRenderer.positionCount = 0;
        }
    }
    #endregion
}
