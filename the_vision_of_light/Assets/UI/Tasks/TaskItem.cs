using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TaskItem : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;
    public Toggle completedToggle;
    public Button removeButton;

    private string myId;
    private System.Action<string, bool> onToggle;
    private System.Action<string> onRemove;

    public void Setup(TaskData data, System.Action<string, bool> onToggleCallback, System.Action<string> onRemoveCallback)
    {
        if (data == null) return;
        myId = data.id;
        titleText.text = data.title ?? "";
        if (descText != null) descText.text = data.description ?? "";

        onToggle = onToggleCallback;
        onRemove = onRemoveCallback;

        if (completedToggle != null)
        {
            completedToggle.isOn = data.completed;
            completedToggle.onValueChanged.RemoveAllListeners();
            completedToggle.onValueChanged.AddListener(val => onToggle?.Invoke(myId, val));
        }

        if (removeButton != null)
        {
            removeButton.onClick.RemoveAllListeners();
            removeButton.onClick.AddListener(() => onRemove?.Invoke(myId));
        }
    }
}
