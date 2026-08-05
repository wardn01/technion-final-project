using UnityEngine;

/// <summary>
/// A world-space 2D marker (e.g. "!" above an NPC) that becomes visible while a related
/// quest chapter is active on a matching step — or while the current objective target
/// is near this marker (same place the minimap "!" points to).
/// </summary>
public class QuestMarkerUI : MonoBehaviour
{
    #region Quest Settings
    [Header("Quest Settings")]
    /// <summary>Quests that should make this marker visible while active.</summary>
    public QuestData[] relatedQuests;

    /// <summary>
    /// Legacy single step. Used when <see cref="requiredSteps"/> is empty.
    /// -1 = visible for the whole chapter.
    /// </summary>
    public int requiredStep = -1;

    /// <summary>
    /// Preferred: steps where this NPC/objective should show "!".
    /// Example: talk at 0 and return at 2 → { 0, 2 }.
    /// Empty = fall back to <see cref="requiredStep"/>.
    /// </summary>
    public int[] requiredSteps;

    [Tooltip("Also show when QuestManager's current objective target is near this transform (matches minimap !).")]
    public bool showWhenObjectiveNearby = true;

    [Tooltip("How close the objective target must be to this marker (meters).")]
    public float nearbyObjectiveDistance = 8f;
    #endregion

    #region Camera Settings
    [Header("Camera Settings")]
    /// <summary>Camera the marker billboards toward; falls back to <see cref="Camera.main"/>.</summary>
    public Camera targetCamera;
    #endregion

    #region Visuals
    [Header("Visuals (2D Image)")]
    public SpriteRenderer markerSprite;

    /// <summary>Vertical bobbing speed.</summary>
    public float bobSpeed = 4f;

    /// <summary>Vertical bobbing amplitude.</summary>
    public float bobHeight = 0.2f;
    #endregion

    /// <summary>Original local position used as the bobbing pivot.</summary>
    private Vector3 startPos;

    #region Unity Lifecycle
    private void Awake()
    {
        ResolveSprite();
        if (markerSprite != null)
            markerSprite.enabled = false;
    }

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        ResolveSprite();
        startPos = transform.localPosition;
        RefreshVisibility();
    }

    private void OnEnable()
    {
        RefreshVisibility();
    }

    private void LateUpdate()
    {
        if (markerSprite == null)
            ResolveSprite();

        if (targetCamera == null)
            targetCamera = Camera.main;

        bool visible = ShouldShowMarker();

        if (markerSprite != null)
        {
            if (visible && !markerSprite.gameObject.activeSelf)
                markerSprite.gameObject.SetActive(true);

            if (markerSprite.enabled != visible)
                markerSprite.enabled = visible;
        }

        if (!visible || markerSprite == null)
            return;

        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);

        if (targetCamera != null)
            transform.forward = targetCamera.transform.forward;
    }
    #endregion

    #region Visibility
    private void RefreshVisibility()
    {
        if (markerSprite == null)
            ResolveSprite();

        if (markerSprite != null)
            markerSprite.enabled = ShouldShowMarker();
    }

    private bool ShouldShowMarker()
    {
        if (QuestManager.Instance == null)
            return false;

        if (MatchesConfiguredQuestStep())
            return true;

        // Minimap already points here — show the world "!" above the NPC/objective too.
        if (showWhenObjectiveNearby && IsCurrentObjectiveNearby())
            return true;

        return false;
    }

    private bool MatchesConfiguredQuestStep()
    {
        if (relatedQuests == null || relatedQuests.Length == 0)
            return false;

        int currentState = QuestManager.Instance.mainQuestState;
        int currentStep = QuestManager.Instance.questStepIndex;

        foreach (QuestData quest in relatedQuests)
        {
            if (quest == null)
                continue;

            if (currentState != quest.stateId)
                continue;

            if (StepMatches(currentStep))
                return true;
        }

        return false;
    }

    private bool IsCurrentObjectiveNearby()
    {
        if (!QuestManager.Instance.CurrentObjectiveHasTarget())
            return false;

        Vector3 target = QuestManager.Instance.GetCurrentObjectiveTarget();
        float maxDist = Mathf.Max(0.5f, nearbyObjectiveDistance);

        // Compare on XZ so a marker floating above the head still counts.
        Vector3 flatTarget = target;
        flatTarget.y = 0f;
        Vector3 flatHere = transform.position;
        flatHere.y = 0f;
        return (flatTarget - flatHere).sqrMagnitude <= maxDist * maxDist;
    }

    private bool StepMatches(int currentStep)
    {
        if (requiredSteps != null && requiredSteps.Length > 0)
        {
            for (int i = 0; i < requiredSteps.Length; i++)
            {
                if (requiredSteps[i] == currentStep)
                    return true;
            }

            return false;
        }

        return requiredStep < 0 || currentStep == requiredStep;
    }

    private void ResolveSprite()
    {
        if (markerSprite != null)
            return;

        markerSprite = GetComponent<SpriteRenderer>();
        if (markerSprite == null)
            markerSprite = GetComponentInChildren<SpriteRenderer>(true);
    }
    #endregion
}
