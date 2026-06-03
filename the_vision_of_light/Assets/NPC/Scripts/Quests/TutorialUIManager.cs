using UnityEngine;
using TMPro;
using System.Collections;

public class TutorialUIManager : MonoBehaviour
{
    [System.Serializable]
    public class TutorialStep
    {
        public int questStateId; 
        [TextArea(3, 5)]
        public string tutorialText; 
    }

    [Header("Tutorials Setup")]
    public TutorialStep[] tutorials;

    [Header("UI References")]
    public RectTransform tutorialPanelRect; 
    public TextMeshProUGUI tutorialTextUI; 

    [Header("Animation Settings")]
    public float slideSpeed = 1500f; 
    public float slideOffset = 800f; 

    private int currentActiveQuest = -1;
    private bool isTransitioning = false;
    
    private Vector2 visiblePosition; 
    private Vector2 hiddenLeft;  

    void Start()
    {
        if (tutorialPanelRect != null)
        {
            visiblePosition = tutorialPanelRect.anchoredPosition;
            hiddenLeft = new Vector2(visiblePosition.x - slideOffset, visiblePosition.y);
            
            tutorialPanelRect.anchoredPosition = hiddenLeft;
            tutorialPanelRect.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (QuestManager.Instance == null || isTransitioning) return;

        int currentQuest = -1;
        if (QuestManager.Instance.trackedQuest != null)
        {
            currentQuest = QuestManager.Instance.trackedQuest.stateId;
        }

        if (currentQuest != currentActiveQuest)
        {
            StartCoroutine(TransitionTutorial(currentQuest));
        }
    }

    IEnumerator TransitionTutorial(int newQuestState)
    {
        isTransitioning = true;

        if (tutorialPanelRect.gameObject.activeSelf)
        {
            yield return StartCoroutine(SlideUI(tutorialPanelRect, hiddenLeft, true));
        }

        yield return new WaitForSeconds(0.4f);

        currentActiveQuest = newQuestState;

        string newText = "";
        foreach (var step in tutorials)
        {
            if (step.questStateId == newQuestState)
            {
                newText = FormatTutorialText(step.tutorialText);
                break;
            }
        }

        if (!string.IsNullOrEmpty(newText))
        {
            if (tutorialTextUI != null) tutorialTextUI.text = newText;
            
            tutorialPanelRect.anchoredPosition = hiddenLeft;
            
            yield return StartCoroutine(SlideUI(tutorialPanelRect, visiblePosition, false));
        }

        isTransitioning = false;
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

    string FormatTutorialText(string rawText)
    {
        string formattedText = rawText;
        if (KeybindManager.Instance != null)
        {
            foreach (var kvp in KeybindManager.Instance.keys)
            {
                string placeholder = "{" + kvp.Key + "}"; 
                if (formattedText.Contains(placeholder))
                {
                    string niceKeyName = GetFriendlyKeyName(kvp.Value);
                    formattedText = formattedText.Replace(placeholder, "<color=yellow>[" + niceKeyName + "]</color>");
                }
            }
        }
        return formattedText;
    }

    string GetFriendlyKeyName(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Mouse0: return "Left Click";
            case KeyCode.Mouse1: return "Right Click";
            case KeyCode.Mouse2: return "Middle Click";
            case KeyCode.LeftShift: return "L-Shift";
            case KeyCode.Escape: return "Esc";
            case KeyCode.Alpha1: return "1";
            case KeyCode.Alpha2: return "2";
            case KeyCode.Alpha3: return "3";
            case KeyCode.Alpha4: return "4";
            default: return key.ToString();
        }
    }
}