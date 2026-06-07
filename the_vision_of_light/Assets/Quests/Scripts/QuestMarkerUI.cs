using UnityEngine;

/// <summary>
/// A world-space 2D marker (e.g. above an NPC or objective) that becomes visible only while one of
/// its related quests is active. While visible it bobs up and down and faces the camera (billboard).
/// </summary>
public class QuestMarkerUI : MonoBehaviour
{
    #region Quest Settings
    [Header("Quest Settings")]
    /// <summary>Quests that should make this marker visible while active.</summary>
    public QuestData[] relatedQuests;

    /// <summary>Which step index shows this marker. -1 = visible for the whole chapter (legacy).</summary>
    public int requiredStep = -1;
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
    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (markerSprite == null)
        {
            markerSprite = GetComponent<SpriteRenderer>();
        }

        startPos = transform.localPosition;
    }

    /// <summary>
    /// Shows/hides the marker based on quest state, and animates the bob + billboard while visible.
    /// </summary>
    private void LateUpdate()
    {
        if (QuestManager.Instance == null || markerSprite == null || relatedQuests == null || relatedQuests.Length == 0) return;

        int currentStep = QuestManager.Instance.questStepIndex;
        bool isAnyQuestActive = false;

        foreach (QuestData quest in relatedQuests)
        {
            if (quest == null) continue;

            bool stateMatches = QuestManager.Instance.mainQuestState == quest.stateId;
            bool stepMatches = requiredStep < 0 || currentStep == requiredStep;

            if (stateMatches && stepMatches)
            {
                isAnyQuestActive = true;
                break;
            }
        }

        if (markerSprite.enabled != isAnyQuestActive)
        {
            markerSprite.enabled = isAnyQuestActive;
        }

        if (isAnyQuestActive)
        {
            float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = new Vector3(startPos.x, newY, startPos.z);

            if (targetCamera != null)
            {
                transform.forward = targetCamera.transform.forward;
            }
        }
    }
    #endregion
}
