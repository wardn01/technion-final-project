using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestButton : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI distanceText; 
    public Button button;

    private QuestData myQuest;
    private Transform player;
    private bool isQuestActive;

    public void Setup(QuestData quest, bool isActive, System.Action<QuestData> callback)
    {
        myQuest = quest;
        isQuestActive = isActive;
        onClickCallback = callback;
        
        if (isActive)
        {
            titleText.text = "☐ " + quest.questTitle;
            titleText.color = Color.yellow;
            
            if (descText != null) descText.color = Color.white;
        }
        else
        {
            titleText.text = "<color=green>☑</color> " + quest.questTitle; 
            titleText.color = Color.gray;
            
            if (descText != null) descText.color = Color.gray;
        }

        if (descText != null)
        {
            string[] words = quest.questDescription.Split(' ');
            if (words.Length > 4)
            {
                descText.text = string.Join(" ", words, 0, 4) + " ...";
            }
            else
            {
                descText.text = quest.questDescription;
            }
        }

        if (button == null) button = GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClickCallback?.Invoke(myQuest));

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    private System.Action<QuestData> onClickCallback;

    private void Update()
    {
        if (isQuestActive && myQuest != null && myQuest.hasTargetLocation && player != null && distanceText != null)
        {
            distanceText.gameObject.SetActive(true);
            float dist = Vector3.Distance(player.position, myQuest.targetLocation);
            distanceText.text = Mathf.RoundToInt(dist).ToString() + "m";
        }
        else if (distanceText != null)
        {
            distanceText.gameObject.SetActive(false);
        }
    }
}