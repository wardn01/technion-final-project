using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Single pickup toast row. Fades out after showTime; supports stacking the same item.
/// </summary>
public class PickupNotification : MonoBehaviour
{
    [Header("UI References")]
    public Image itemIcon;
    public TextMeshProUGUI itemText;
    public CanvasGroup canvasGroup;

    [Header("Timing")]
    [Tooltip("Seconds the row stays fully visible before fading.")]
    public float showTime = 2f;

    [Tooltip("Seconds to fade from full alpha to zero.")]
    public float fadeTime = 1f;

    private ItemData item;
    private int displayedAmount;
    private float timer;

    /// <summary>Initializes icon, name x amount, and visibility timer.</summary>
    public void Setup(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
        {
            Destroy(gameObject);
            return;
        }

        this.item = item;
        displayedAmount = amount;

        if (itemIcon != null)
        {
            itemIcon.sprite = item.itemIcon;
            itemIcon.enabled = item.itemIcon != null;
        }

        RefreshText();
        ResetTimer();
    }

    /// <summary>Adds to this row when the item matches; refreshes text and timer.</summary>
    public bool TryStack(ItemData other, int amount)
    {
        if (item == null || other == null || item != other || amount <= 0)
            return false;

        displayedAmount += amount;
        RefreshText();
        ResetTimer();
        return true;
    }

    private void RefreshText()
    {
        if (itemText != null && item != null)
            itemText.text = $"{item.itemName} x{displayedAmount}";
    }

    private void ResetTimer()
    {
        timer = showTime;
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        timer -= dt;

        if (timer > 0f)
            return;

        if (canvasGroup != null && fadeTime > 0f)
            canvasGroup.alpha -= (1f / fadeTime) * dt;

        if (canvasGroup == null || canvasGroup.alpha <= 0f)
            Destroy(gameObject);
    }
}
