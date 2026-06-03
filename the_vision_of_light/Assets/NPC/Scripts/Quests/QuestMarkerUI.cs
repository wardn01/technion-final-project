using UnityEngine;

public class QuestMarkerUI : MonoBehaviour
{
    [Header("Quest Settings")]
    public QuestData[] relatedQuests;
    
    [Header("Camera Settings")]
    public Camera targetCamera;

    [Header("Visuals (2D Image)")]
    public SpriteRenderer markerSprite;
    public float bobSpeed = 4f; 
    public float bobHeight = 0.2f; 

    private Vector3 startPos;

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

    private void LateUpdate()
    {
        if (QuestManager.Instance == null || markerSprite == null || relatedQuests == null || relatedQuests.Length == 0) return;

        bool isAnyQuestActive = false;
        foreach (QuestData quest in relatedQuests)
        {
            if (quest != null && QuestManager.Instance.mainQuestState == quest.stateId)
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
}