using UnityEngine;

/// <summary>
/// Completes a quest step when the player buys a health potion (or a specific item) from a shop.
/// </summary>
public class QuestShopObjective : MonoBehaviour
{
    [Header("Quest Requirements")]
    public int requiredState = 1;
    public int requiredStep = 3;

    [Header("Purchase")]
    [Tooltip("When enabled, any consumable with instant heal counts.")]
    public bool acceptAnyHealthPotion = true;

    [Tooltip("Optional exact item when Accept Any Health Potion is disabled.")]
    public ItemData requiredItem;

    private bool isCompleted;

    public static void NotifyPurchase(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
            return;

        QuestShopObjective[] objectives = FindObjectsByType<QuestShopObjective>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (QuestShopObjective objective in objectives)
            objective.TryComplete(item);
    }

    private void TryComplete(ItemData item)
    {
        if (isCompleted || QuestManager.Instance == null)
            return;

        if (!QuestManager.Instance.IsAtQuestStep(requiredState, requiredStep))
            return;

        if (!IsValidPurchase(item))
            return;

        isCompleted = true;
        QuestManager.Instance.AdvanceStep(requiredState, requiredStep);
    }

    private bool IsValidPurchase(ItemData item)
    {
        if (requiredItem != null)
            return item == requiredItem;

        if (!acceptAnyHealthPotion)
            return false;

        if (item is ConsumableItemData consumable)
            return consumable.instantHeal > 0f || consumable.tickHealAmount > 0f;

        return item.itemName.Contains("Health Potion");
    }
}
