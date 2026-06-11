using UnityEngine;
using TMPro;

/// <summary>
/// Short-lived TMP label on the player HP bar (e.g. "-25" red, "+30" green).
/// Each hit gets its own number, spread in a fan so they do not overlap.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(TextMeshProUGUI))]
public class UIFloatingText : MonoBehaviour
{
    [Header("Motion (UI units / second)")]
    [Tooltip("Drift to the right while floating.")]
    public float moveSpeedX = 28f;

    [Tooltip("Main upward motion.")]
    public float moveSpeedY = 65f;

    [Header("Lifetime")]
    [Tooltip("Seconds fully visible before fading.")]
    public float lifeTime = 1.1f;

    [Tooltip("Alpha removed per second while fading.")]
    public float fadeSpeed = 2f;

    [Header("Spawn punch")]
    [Tooltip("Starting scale for a quick pop-in.")]
    public float punchScale = 1.2f;

    [Tooltip("How long the pop-in lasts.")]
    public float punchDuration = 0.12f;

    private static readonly Vector2[] SpreadPattern =
    {
        new Vector2(0f, 0f),
        new Vector2(-42f, 14f),
        new Vector2(42f, 14f),
        new Vector2(-28f, 30f),
        new Vector2(28f, 30f),
        new Vector2(0f, 46f),
    };

    private const float SpreadResetDelay = 0.35f;

    private static int spreadIndex;
    private static float lastSpreadTime;

    private RectTransform rectTransform;
    private TextMeshProUGUI textMesh;
    private Color textColor;
    private float lifeTimer;
    private float punchTimer;
    private bool isFading;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSpreadState()
    {
        spreadIndex = 0;
        lastSpreadTime = 0f;
    }

    /// <summary>Next fan offset when several numbers spawn in quick succession.</summary>
    public static Vector2 GetNextSpawnOffset()
    {
        if (Time.unscaledTime - lastSpreadTime > SpreadResetDelay)
            spreadIndex = 0;

        lastSpreadTime = Time.unscaledTime;
        Vector2 offset = SpreadPattern[spreadIndex % SpreadPattern.Length];
        spreadIndex++;
        return offset;
    }

    /// <summary>Aligns a new label to a spawn anchor plus fan offset.</summary>
    public static void PlaceAtSpawn(RectTransform label, RectTransform spawnAnchor, Vector2 spreadOffset)
    {
        if (label == null || spawnAnchor == null)
            return;

        label.anchorMin = spawnAnchor.anchorMin;
        label.anchorMax = spawnAnchor.anchorMax;
        label.pivot = spawnAnchor.pivot;
        label.anchoredPosition = spawnAnchor.anchoredPosition + spreadOffset;
    }

    /// <summary>Sets text, color, and starts lifetime. Call after <see cref="PlaceAtSpawn"/>.</summary>
    public void Setup(string text, Color color)
    {
        if (string.IsNullOrEmpty(text))
        {
            Destroy(gameObject);
            return;
        }

        rectTransform = GetComponent<RectTransform>();
        textMesh = GetComponent<TextMeshProUGUI>();

        if (textMesh == null)
        {
            Destroy(gameObject);
            return;
        }

        textMesh.text = text;
        textColor = color;
        textMesh.color = color;
        lifeTimer = lifeTime;
        punchTimer = punchDuration;
        isFading = false;

        if (rectTransform != null && punchDuration > 0f)
            rectTransform.localScale = Vector3.one * punchScale;
    }

    private void Update()
    {
        if (textMesh == null)
        {
            Destroy(gameObject);
            return;
        }

        float dt = Time.unscaledDeltaTime;

        if (punchTimer > 0f && rectTransform != null && punchDuration > 0f)
        {
            punchTimer -= dt;
            float t = 1f - Mathf.Clamp01(punchTimer / punchDuration);
            rectTransform.localScale = Vector3.one * Mathf.Lerp(punchScale, 1f, t);
        }

        lifeTimer -= dt;
        if (lifeTimer <= 0f)
            isFading = true;

        if (rectTransform != null)
            rectTransform.anchoredPosition += new Vector2(moveSpeedX, moveSpeedY) * dt;

        if (!isFading)
            return;

        textColor.a -= fadeSpeed * dt;
        textMesh.color = textColor;

        if (textColor.a <= 0f)
            Destroy(gameObject);
    }
}
