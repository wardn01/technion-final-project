using UnityEngine;
using TMPro;
using System.Collections;

public class QuestTrackerUI : MonoBehaviour
{
    public static QuestTrackerUI Instance { get; private set; }

    [Header("UI References")]
    public RectTransform trackerPanelRect; 
    public TextMeshProUGUI questTitleText;
    public TextMeshProUGUI questDescText;
    public TextMeshProUGUI distanceText;

    [Header("Animation Settings")]
    public float slideSpeed = 1500f;
    public float slideOffset = 800f; 

    [Header("World References")]
    public Transform player;

    private QuestData currentDisplayedQuest = null;
    private bool isTransitioning = false;
    
    private Vector2 visiblePosition;
    private Vector2 hiddenLeft;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (trackerPanelRect != null)
        {
            visiblePosition = trackerPanelRect.anchoredPosition;
            hiddenLeft = new Vector2(visiblePosition.x - slideOffset, visiblePosition.y);
            
            trackerPanelRect.anchoredPosition = hiddenLeft;
            trackerPanelRect.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (isTransitioning) return; 

        QuestData actualQuest = QuestManager.Instance != null ? QuestManager.Instance.trackedQuest : null;

        if (actualQuest != null && actualQuest.stateId < QuestManager.Instance.mainQuestState)
        {
            actualQuest = null;
        }

        if (actualQuest != currentDisplayedQuest)
        {
            StartCoroutine(TransitionQuest(actualQuest));
        }
        else if (actualQuest != null)
        {
            UpdateQuestTexts(actualQuest);
        }
    }

    IEnumerator TransitionQuest(QuestData newQuest)
    {
        isTransitioning = true;

        if (currentDisplayedQuest != null && trackerPanelRect.gameObject.activeSelf)
        {
            yield return StartCoroutine(SlideUI(trackerPanelRect, hiddenLeft, true));
        }

        yield return new WaitForSeconds(0.4f);

        currentDisplayedQuest = newQuest;

        if (newQuest != null)
        {
            UpdateQuestTexts(newQuest); 
            trackerPanelRect.anchoredPosition = hiddenLeft;
            yield return StartCoroutine(SlideUI(trackerPanelRect, visiblePosition, false));
        }

        isTransitioning = false;
    }

    void UpdateQuestTexts(QuestData quest)
    {
        if (quest == null) return;

        questTitleText.text = quest.questTitle;
        string desc = quest.questDescription;
        string[] words = desc.Split(' ');
        
        if (words.Length > 7) questDescText.text = string.Join(" ", words, 0, 7) + " ...";
        else questDescText.text = desc;

        if (distanceText != null)
        {
            if (quest.hasTargetLocation && player != null)
            {
                if (!distanceText.gameObject.activeSelf) distanceText.gameObject.SetActive(true);
                
                float distance = Vector3.Distance(player.position, quest.targetLocation);
                distanceText.text = Mathf.RoundToInt(distance).ToString() + "m";
                
                if (distance < 15f) distanceText.color = Color.green;
                else distanceText.color = new Color(1f, 0.8f, 0f);
            }
            else
            {
                if (distanceText.gameObject.activeSelf) distanceText.gameObject.SetActive(false);
            }
        }
    }

    IEnumerator SlideUI(RectTransform rect, Vector2 targetPos, bool hideAtEnd)
    {
        if (rect == null) yield break;
        if (!hideAtEnd) rect.gameObject.SetActive(true);

        while (Vector2.Distance(rect.anchoredPosition, targetPos) > 0.5f)
        {
            rect.anchoredPosition = Vector2.MoveTowards(rect.anchoredPosition, targetPos, slideSpeed * Time.deltaTime);
            yield return null;
        }

        rect.anchoredPosition = targetPos;
        if (hideAtEnd) rect.gameObject.SetActive(false);
    }
}