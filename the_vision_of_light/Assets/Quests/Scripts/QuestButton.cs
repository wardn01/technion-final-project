using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A single row in the quest log list. Displays a quest's title (with an active/completed checkbox),
/// a truncated description, and a live distance readout, and forwards clicks to the details view.
/// </summary>
public class QuestButton : MonoBehaviour
{
    #region UI References
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI distanceText;
    public Button button;
    #endregion

    #region State
    private QuestData myQuest;
    private Transform player;
    private bool isQuestActive;
    private System.Action<QuestData> onClickCallback;
    #endregion

    #region Setup
    /// <summary>
    /// Initializes this row for a quest. Styles it as active (yellow, open box) or completed
    /// (gray, green check), truncates the description, and wires the click callback.
    /// </summary>
    /// <param name="quest">The quest this row represents.</param>
    /// <param name="isActive">Whether the quest is the currently active one.</param>
    /// <param name="callback">Invoked with the quest when the row is clicked.</param>
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
            string preview = isActive && QuestManager.Instance != null
                ? quest.GetDescriptionForStep(QuestManager.Instance.questStepIndex)
                : quest.questDescription;

            string[] words = preview.Split(' ');
            descText.text = words.Length > 4
                ? string.Join(" ", words, 0, 4) + " ..."
                : preview;
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
    #endregion

    #region Distance Updating
    /// <summary>
    /// Continuously refreshes the distance readout for active quests that have a target location.
    /// </summary>
    private void Update()
    {
        if (isQuestActive && myQuest != null && player != null && distanceText != null
            && QuestManager.Instance != null && QuestManager.Instance.CurrentObjectiveHasTarget())
        {
            distanceText.gameObject.SetActive(true);
            float dist = Vector3.Distance(player.position, QuestManager.Instance.GetCurrentObjectiveTarget());
            distanceText.text = Mathf.RoundToInt(dist).ToString() + "m";
        }
        else if (distanceText != null)
        {
            distanceText.gameObject.SetActive(false);
        }
    }
    #endregion
}
