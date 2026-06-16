using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// Player HUD boss health bar — shown during boss fights instead of world-space enemy UI.
    /// Also owns RageGolem (summon meter) and RageOrc (enrage meter) threshold fills.
    /// </summary>
    public class BossHealthBarUI : MonoBehaviour
    {
        public static BossHealthBarUI Instance { get; private set; }

        #region Boss Bar References
        [Tooltip("The whole BossHealthBarUI object under HUDScreen — NOT just Background.")]
        [SerializeField] private GameObject barRoot;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private TextMeshProUGUI bossNameText;
        [SerializeField] private TextMeshProUGUI levelText;
        #endregion

        #region Rage Meters
        [Tooltip("RageGolem under BossHealthBarUI — fills as Kola loses HP, then mini golems spawn.")]
        [SerializeField] private GameObject golemSummonMeterRoot;
        [SerializeField] private Image golemSummonFillImage;

        [Tooltip("RageOrc under BossHealthBarUI — fills as the Orc loses HP, then enrage triggers.")]
        [SerializeField] private GameObject orcRageMeterRoot;
        [SerializeField] private Image orcRageFillImage;

        private BossEnemy trackedBoss;
        private bool golemSummonMeterActive;
        private bool orcRageMeterActive;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResolveReferences();
            HideImmediate();
        }

        private void ResolveReferences()
        {
            if (barRoot == null)
            {
                GameObject found = GameObject.Find("BossHealthBarUI");
                if (found != null)
                    barRoot = found;
            }

            if (golemSummonMeterRoot == null && barRoot != null)
            {
                Transform rage = barRoot.transform.Find("RageGolem");
                if (rage != null)
                {
                    golemSummonMeterRoot = rage.gameObject;

                    if (golemSummonFillImage == null)
                    {
                        Transform fill = rage.Find("Background/Fill");
                        if (fill != null)
                            golemSummonFillImage = fill.GetComponent<Image>();
                    }
                }
            }

            if (orcRageMeterRoot == null && barRoot != null)
            {
                Transform rage = barRoot.transform.Find("RageOrc");
                if (rage != null)
                {
                    orcRageMeterRoot = rage.gameObject;

                    if (orcRageFillImage == null)
                    {
                        Transform fill = rage.Find("Background/Fill");
                        if (fill != null)
                            orcRageFillImage = fill.GetComponent<Image>();
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        #endregion

        #region Boss Bar API
        /// <summary>Shows the bar for a boss and activates RageGolem or RageOrc when applicable.</summary>
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

            bool showGolemMeter = boss is Golem;
            bool showOrcMeter = boss is Orc;
            SetGolemSummonMeterVisible(showGolemMeter);
            SetOrcRageMeterVisible(showOrcMeter);

            if (showGolemMeter)
                UpdateGolemSummonMeter(currentHealth, maxHealth, ((Golem)boss).MiniGolemSummonHealthPercent, false);
            else if (showOrcMeter)
                UpdateOrcRageMeter(currentHealth, maxHealth, ((Orc)boss).EnrageHealthPercent, ((Orc)boss).IsEnrageTriggered);
        }

        /// <summary>Updates the main HP fill while a boss is aggroed.</summary>
        public void UpdateHealth(float currentHealth, float maxHealth)
        {
            if (healthFillImage != null && maxHealth > 0f)
                healthFillImage.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
        }

        /// <summary>Hides the bar when the boss resets or dies.</summary>
        public void HideBoss(BossEnemy boss)
        {
            if (boss != null && trackedBoss != null && trackedBoss != boss)
                return;

            HideImmediate();
        }
        #endregion

        #region Rage Meter API
        /// <summary>Fills RageGolem as Kola loses HP toward the MiniGolem summon threshold.</summary>
        public void UpdateGolemSummonMeter(float currentHealth, float maxHealth, float summonAtHealthPercent, bool summonComplete)
        {
            UpdateThresholdMeter(golemSummonFillImage, golemSummonMeterActive, currentHealth, maxHealth, summonAtHealthPercent, summonComplete);
        }

        /// <summary>Fills RageOrc as the Orc loses HP toward the enrage threshold.</summary>
        public void UpdateOrcRageMeter(float currentHealth, float maxHealth, float enrageAtHealthPercent, bool enrageComplete)
        {
            UpdateThresholdMeter(orcRageFillImage, orcRageMeterActive, currentHealth, maxHealth, enrageAtHealthPercent, enrageComplete);
        }

        /// <summary>Hides RageGolem after MiniGolems spawn.</summary>
        public void HideGolemSummonMeter()
        {
            SetGolemSummonMeterVisible(false);
        }

        /// <summary>Hides RageOrc after the enrage cutscene starts.</summary>
        public void HideOrcRageMeter()
        {
            SetOrcRageMeterVisible(false);
        }

        private static void UpdateThresholdMeter(
            Image fillImage,
            bool isActive,
            float currentHealth,
            float maxHealth,
            float thresholdHealthPercent,
            bool thresholdReached)
        {
            if (!isActive || fillImage == null)
                return;

            if (thresholdReached)
            {
                fillImage.fillAmount = 1f;
                return;
            }

            if (maxHealth <= 0f)
            {
                fillImage.fillAmount = 0f;
                return;
            }

            float thresholdHealth = maxHealth * thresholdHealthPercent;
            float healthToLose = maxHealth - thresholdHealth;
            if (healthToLose <= 0f)
            {
                fillImage.fillAmount = 1f;
                return;
            }

            float healthLost = maxHealth - currentHealth;
            fillImage.fillAmount = Mathf.Clamp01(healthLost / healthToLose);
        }

        private void SetGolemSummonMeterVisible(bool visible)
        {
            golemSummonMeterActive = visible;

            if (golemSummonMeterRoot != null)
                golemSummonMeterRoot.SetActive(visible);

            if (visible && golemSummonFillImage != null)
                golemSummonFillImage.fillAmount = 0f;
        }

        private void SetOrcRageMeterVisible(bool visible)
        {
            orcRageMeterActive = visible;

            if (orcRageMeterRoot != null)
                orcRageMeterRoot.SetActive(visible);

            if (visible && orcRageFillImage != null)
                orcRageFillImage.fillAmount = 0f;
        }

        private void HideImmediate()
        {
            trackedBoss = null;
            SetGolemSummonMeterVisible(false);
            SetOrcRageMeterVisible(false);
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (barRoot != null)
                barRoot.SetActive(visible);
        }
        #endregion
    }
}
