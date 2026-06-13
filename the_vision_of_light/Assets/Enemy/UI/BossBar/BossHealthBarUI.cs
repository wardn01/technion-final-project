using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Player HUD boss health bar — shown during boss fights instead of world-space enemy UI.
/// Attach to a manager object; assign <see cref="barRoot"/> to the whole BossHealthBarUI object under HUDScreen.
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    public static BossHealthBarUI Instance { get; private set; }

    [Tooltip("The whole BossHealthBarUI object under HUDScreen — NOT just Background.")]
    [SerializeField] private GameObject barRoot;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private TextMeshProUGUI levelText;

    private BossEnemy trackedBoss;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (barRoot == null)
        {
            GameObject found = GameObject.Find("BossHealthBarUI");
            if (found != null)
                barRoot = found;
        }

        HideImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ShowBoss(BossEnemy boss, string bossName, int level, float maxHealth, float currentHealth)
    {
        if (boss == null) return;

        trackedBoss = boss;
        SetVisible(true);

        if (bossNameText != null)
            bossNameText.text = bossName;

        if (levelText != null)
            levelText.text = $"Lv.{level}";

        UpdateHealth(currentHealth, maxHealth);
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (healthFillImage != null && maxHealth > 0f)
            healthFillImage.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
    }

    public void HideBoss(BossEnemy boss)
    {
        if (boss != null && trackedBoss != null && trackedBoss != boss)
            return;

        HideImmediate();
    }

    private void HideImmediate()
    {
        trackedBoss = null;
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (barRoot != null)
            barRoot.SetActive(visible);
    }
}
