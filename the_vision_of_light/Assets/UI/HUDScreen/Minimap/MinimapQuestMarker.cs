using UnityEngine;

public class MinimapQuestMarker : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform markerIcon; 
    public RectTransform minimapRect; 

    [Header("World References")]
    public Camera minimapCamera; 

    private void Update()
    {
        if (QuestManager.Instance == null || !QuestManager.Instance.CurrentObjectiveHasTarget())
        {
            markerIcon.gameObject.SetActive(false);
            return;
        }

        markerIcon.gameObject.SetActive(true);

        Vector3 targetPos = QuestManager.Instance.GetCurrentObjectiveTarget();

        if (minimapCamera != null)
        {
            Vector3 viewportPos = minimapCamera.WorldToViewportPoint(targetPos);

            Vector2 uiPos = new Vector2(
                (viewportPos.x - 0.5f) * minimapRect.rect.width,
                (viewportPos.y - 0.5f) * minimapRect.rect.height
            );

            float minimapRadius = (minimapRect.rect.width / 2f) - 15f; 

            if (viewportPos.z < 0)
            {
                uiPos *= -1f;
            }

            if (uiPos.magnitude > minimapRadius)
            {
                uiPos = uiPos.normalized * minimapRadius;
            }

            markerIcon.anchoredPosition = uiPos;
        }
    }
}