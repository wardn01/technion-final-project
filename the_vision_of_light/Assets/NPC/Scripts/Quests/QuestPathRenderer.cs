using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(LineRenderer))]
public class QuestPathRenderer : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Settings")]
    public float activationDistance = 100f; 
    public float hideDistance = 2f; 
    public float pathHeightOffset = 0.2f; 
    public float updateInterval = 0.5f; 

    [Header("Visuals")]
    public float scrollSpeed = 1.5f; 
    public float textureTilingSize = 2f; 

    private LineRenderer lineRenderer;
    private NavMeshPath path;
    private float timer = 0f;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        path = new NavMeshPath();
    }

    private void Update()
    {
        if (QuestManager.Instance == null || QuestManager.Instance.trackedQuest == null || !QuestManager.Instance.trackedQuest.hasTargetLocation)
        {
            lineRenderer.positionCount = 0; 
            return;
        }

        Vector3 targetPos = QuestManager.Instance.trackedQuest.targetLocation;
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
}