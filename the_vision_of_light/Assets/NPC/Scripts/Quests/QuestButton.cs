using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestButton : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    private QuestData myQuest;
    private System.Action<QuestData> onClickCallback;

    public void Setup(QuestData data, bool isActive, System.Action<QuestData> callback)
    {
        myQuest = data;
        onClickCallback = callback;
        
        if (isActive)
        {
            titleText.text = "📌 " + data.questTitle;
            titleText.color = Color.yellow;
        }
        else
        {
            titleText.text = "✔️ " + data.questTitle;
            titleText.color = Color.gray;
        }

        Button btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onClickCallback?.Invoke(myQuest));
    }
}