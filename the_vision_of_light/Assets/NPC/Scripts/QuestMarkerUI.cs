using UnityEngine;

public class QuestMarkerUI : MonoBehaviour
{
    [Header("Quest Settings")]
    public QuestData[] relatedQuests;
    
    [Header("Visuals (2D Image)")]
    public SpriteRenderer markerSprite;
    public float bobSpeed = 4f; 
    public float bobHeight = 0.2f; 

    private Vector3 startPos;
    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main; 
        
        if (markerSprite == null)
        {
            markerSprite = GetComponent<SpriteRenderer>();
        }

        startPos = transform.localPosition; 
    }

    private void Update()
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
            
            if (mainCam != null)
            {
                transform.LookAt(transform.position + mainCam.transform.rotation * Vector3.forward,
                                 mainCam.transform.rotation * Vector3.up);
            }
        }
    }
}