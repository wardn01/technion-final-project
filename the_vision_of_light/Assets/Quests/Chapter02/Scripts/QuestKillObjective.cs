using UnityEngine;

/// <summary>
/// Activates quest monsters during one step and advances when all
/// <see cref="QuestMonster"/> in the group are defeated.
/// Put this on the same parent that holds the skeletons (your setup).
/// </summary>
public class QuestKillObjective : MonoBehaviour
{
    [Header("Quest Requirements")]
    public int requiredState = 1;
    public int requiredStep = 1;

    [Header("Monsters")]
    [Tooltip("Parent holding quest enemies. Leave empty to use this GameObject's children.")]
    public GameObject monstersGroup;

    [Tooltip("Leave at 0 to auto-count QuestMonster components in the group.")]
    public int requiredKills;

    [Header("Feedback")]
    public bool showWellDoneMessage = true;

    [TextArea(1, 2)]
    public string wellDoneMessage = "Well done!";

    private int currentKills;
    private bool isCompleted;

    private void Awake()
    {
        if (monstersGroup == null)
            monstersGroup = gameObject;

        CacheRequiredKills();
        SetMonstersActive(false);
    }

    private void Start()
    {
        TryActivateForCurrentStep();
    }

    private void Update()
    {
        TryActivateForCurrentStep();
    }

    private void TryActivateForCurrentStep()
    {
        if (isCompleted || QuestManager.Instance == null)
            return;

        if (!QuestManager.Instance.IsAtQuestStep(requiredState, requiredStep))
            return;

        if (!AreMonstersVisible() && currentKills < requiredKills)
            SetMonstersActive(true);
    }

    public void RegisterKill()
    {
        if (isCompleted || QuestManager.Instance == null)
            return;

        if (!QuestManager.Instance.IsAtQuestStep(requiredState, requiredStep))
            return;

        currentKills++;

        if (currentKills < requiredKills)
            return;

        CompleteObjective();
    }

    private void CompleteObjective()
    {
        isCompleted = true;
        SetMonstersActive(false);

        if (showWellDoneMessage && NotificationManager.Instance != null)
            NotificationManager.Instance.ShowWarning(wellDoneMessage);

        QuestManager.Instance.AdvanceStep(requiredState, requiredStep);
    }

    private void CacheRequiredKills()
    {
        if (requiredKills > 0)
            return;

        requiredKills = monstersGroup.GetComponentsInChildren<QuestMonster>(true).Length;

        if (requiredKills <= 0)
            Debug.LogWarning($"[QuestKillObjective] No QuestMonster found under '{monstersGroup.name}'. Add QuestMonster to each enemy.");
    }

    private bool AreMonstersVisible()
    {
        foreach (QuestMonster monster in monstersGroup.GetComponentsInChildren<QuestMonster>(true))
        {
            if (monster != null && monster.gameObject.activeInHierarchy)
                return true;
        }

        return false;
    }

    private void SetMonstersActive(bool active)
    {
        if (active)
            EnsureGroupHierarchyActive();

        if (monstersGroup == gameObject)
        {
            foreach (QuestMonster monster in monstersGroup.GetComponentsInChildren<QuestMonster>(true))
            {
                if (monster != null)
                    monster.gameObject.SetActive(active);
            }

            return;
        }

        monstersGroup.SetActive(active);
    }

    private void EnsureGroupHierarchyActive()
    {
        Transform node = monstersGroup != null ? monstersGroup.transform : transform;
        while (node != null)
        {
            if (!node.gameObject.activeSelf)
                node.gameObject.SetActive(true);

            node = node.parent;
        }
    }
}
