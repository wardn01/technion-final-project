using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A single item granted to the player when a quest is completed.
/// </summary>
[System.Serializable]
public class QuestReward
{
    public ItemData item;
    public int amount;
}

/// <summary>
/// One objective inside a quest. A quest can chain several steps before it completes
/// (e.g. talk → visit location → return and talk).
/// </summary>
[System.Serializable]
public class QuestStep
{
    [TextArea(2, 4)]
    public string description;

    public bool hasTargetLocation;
    public Vector3 targetLocation;
}

/// <summary>
/// ScriptableObject describing one chapter of the main story. While <see cref="QuestManager.mainQuestState"/>
/// equals <see cref="stateId"/>, the quest is active. Progress within it is tracked by
/// <see cref="QuestManager.questStepIndex"/> against <see cref="steps"/>.
/// </summary>
[CreateAssetMenu(fileName = "New Quest", menuName = "Game Data/Quest/Quest Data")]
public class QuestData : ScriptableObject
{
    public int stateId;
    public string questTitle;

    [TextArea]
    public string questDescription;

    /// <summary>Ordered objectives for this quest. If empty, the quest uses <see cref="questDescription"/> as a single step.</summary>
    public List<QuestStep> steps = new List<QuestStep>();

    public List<QuestReward> rewards;

    public bool hasTargetLocation;
    public Vector3 targetLocation;

    public int StepCount => (steps != null && steps.Count > 0) ? steps.Count : 1;

    public string GetDescriptionForStep(int stepIndex)
    {
        if (steps != null && stepIndex >= 0 && stepIndex < steps.Count && !string.IsNullOrEmpty(steps[stepIndex].description))
            return steps[stepIndex].description;

        return questDescription;
    }

    public bool HasTargetForStep(int stepIndex)
    {
        if (steps != null && stepIndex >= 0 && stepIndex < steps.Count)
            return steps[stepIndex].hasTargetLocation;

        return hasTargetLocation;
    }

    public Vector3 GetTargetForStep(int stepIndex)
    {
        if (steps != null && stepIndex >= 0 && stepIndex < steps.Count)
            return steps[stepIndex].targetLocation;

        return targetLocation;
    }
}
