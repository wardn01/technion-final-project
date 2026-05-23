using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[Serializable]
public class TaskData
{
    public string id;
    public string title;
    public string description;
    public bool completed;

    public TaskData() { id = Guid.NewGuid().ToString(); }
}

[Serializable]
public class TaskContainer
{
    public List<TaskData> tasks = new List<TaskData>();
}

public class TasksPageManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject tasksPanel;
    public Transform contentParent;
    public TaskItem taskItemPrefab;

    [Header("Create Task")]
    public TMP_InputField newTaskTitleInput;
    public TMP_InputField newTaskDescInput;
    public Button createTaskButton;

    private TaskContainer container = new TaskContainer();

    private void Awake()
    {
        if (createTaskButton != null)
        {
            createTaskButton.onClick.RemoveAllListeners();
            createTaskButton.onClick.AddListener(() => CreateTaskFromInput());
        }
    }

    private void Start()
    {
        LoadTasks();
        RefreshUI();
    }

    public void CreateTaskFromInput()
    {
        if (newTaskTitleInput == null) return;
        string title = newTaskTitleInput.text?.Trim();
        string desc = newTaskDescInput != null ? newTaskDescInput.text : "";

        if (string.IsNullOrEmpty(title)) return;

        TaskData t = new TaskData { title = title, description = desc, completed = false };
        container.tasks.Add(t);
        SaveTasks();
        RefreshUI();

        newTaskTitleInput.text = "";
        if (newTaskDescInput != null) newTaskDescInput.text = "";
    }

    public void AddTask(TaskData task)
    {
        if (task == null) return;
        container.tasks.Add(task);
        SaveTasks();
        RefreshUI();
    }

    public void RemoveTask(string id)
    {
        container.tasks.RemoveAll(x => x.id == id);
        SaveTasks();
        RefreshUI();
    }

    public void ToggleTask(string id, bool completed)
    {
        TaskData t = container.tasks.Find(x => x.id == id);
        if (t != null)
        {
            t.completed = completed;
            SaveTasks();
        }
    }

    public void RefreshUI()
    {
        if (contentParent == null || taskItemPrefab == null) return;

        // clear
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }

        foreach (var task in container.tasks)
        {
            TaskItem it = Instantiate(taskItemPrefab, contentParent);
            if (it != null)
            {
                it.Setup(task, (id, completed) => ToggleTask(id, completed), id => RemoveTask(id));
            }
        }
    }

    public void SaveTasks()
    {
        try
        {
            SaveManager.SaveTasks(container);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to save tasks: " + ex.Message);
        }
    }

    public void LoadTasks()
    {
        try
        {
            container = SaveManager.LoadTasks();
            if (container == null)
            {
                container = new TaskContainer();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Failed to load tasks: " + ex.Message);
            container = new TaskContainer();
        }
    }
}
