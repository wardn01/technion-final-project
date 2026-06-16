using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using VisionOfLight.Player;

/// <summary>
/// Weapon-specific E/Q skill HUD: cooldown fills, charge dots, ready LEDs, and ready sounds.
/// One <see cref="WeaponUIProfile"/> per weapon; only the active weapon's roots are shown.
/// </summary>
public class PlayerSkillsUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Player combat script that owns weapon state and timers.")]
    public PlayerCombat combatScript;

    [System.Serializable]
    public class QChargeDot
    {
        [Tooltip("Optional parent (e.g. 1_Req). Hidden when this slot is not needed.")]
        public GameObject slotRoot;

        [Tooltip("Shown when this charge slot is empty.")]
        public GameObject iconOff;

        [Tooltip("Shown when this charge slot is filled.")]
        public GameObject iconOn;
    }

    [System.Serializable]
    public class WeaponUIProfile
    {
        [Tooltip("Must match WeaponItemData.itemName (case-insensitive).")]
        public string weaponName;

        [Header("Root Objects to Toggle")]
        public GameObject eSkillRoot;
        public GameObject qSkillRoot;

        [Header("E Skill UI")]
        [Tooltip("Radial/linear fill while E is on cooldown.")]
        public Image eCooldownFill;
        public GameObject eFrameLed;
        public TextMeshProUGUI eTimerText;

        [Header("Q Skill UI")]
        [Tooltip("Drains as E hits accumulate toward Q.")]
        public Image qCooldownFill;
        public GameObject qFrameLed;

        [Header("Q Charge Dots (optional)")]
        public QChargeDot[] qChargeDots;
    }

    [Header("UI Layouts")]
    public WeaponUIProfile[] weaponUIs;

    [Header("Audio Settings")]
    public float eReadyVolume = 0.2f;
    public float qReadyVolume = 0.4f;
    public AudioSource uiAudioSource;
    public AudioClip eReadySound;
    public AudioClip qReadySound;

    private int lastETimerValue = -1;
    private bool wasEReady = true;
    private bool wasQReady = true;

    private string lastWeaponName = "";
    private Coroutine currentPulseCoroutine;

    private void Start()
    {
        foreach (var ui in weaponUIs)
        {
            if (ui.eTimerText != null) ui.eTimerText.gameObject.SetActive(false);
            if (ui.eCooldownFill != null) ui.eCooldownFill.fillAmount = 0;
            if (ui.qCooldownFill != null) ui.qCooldownFill.fillAmount = 1;
            ResetQChargeDots(ui, 0, ui.qChargeDots != null ? ui.qChargeDots.Length : 0);

            if (ui.eSkillRoot != null) ui.eSkillRoot.SetActive(false);
            if (ui.qSkillRoot != null) ui.qSkillRoot.SetActive(false);
        }
    }

    private void Update()
    {
        if (combatScript == null || combatScript.activeWeaponData == null || weaponUIs.Length == 0)
            return;

        UpdateWeaponUI();
        HandleSkillE();
        HandleSkillQ();
    }

    /// <summary>Index into weaponUIs for the equipped weapon, or -1 if none match.</summary>
    private int GetCurrentUIIndex()
    {
        if (combatScript == null || combatScript.activeWeaponData == null)
            return -1;

        for (int i = 0; i < weaponUIs.Length; i++)
        {
            if (string.Equals(weaponUIs[i].weaponName.Trim(), combatScript.activeWeaponData.itemName.Trim(), System.StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    /// <summary>Shows E/Q roots for the current weapon only when the weapon changes.</summary>
    private void UpdateWeaponUI()
    {
        string currentName = combatScript.activeWeaponData.itemName;

        if (currentName == lastWeaponName)
            return;

        foreach (var ui in weaponUIs)
        {
            if (ui.eSkillRoot != null) ui.eSkillRoot.SetActive(false);
            if (ui.qSkillRoot != null) ui.qSkillRoot.SetActive(false);
        }

        int currentIndex = GetCurrentUIIndex();
        if (currentIndex != -1)
        {
            if (weaponUIs[currentIndex].eSkillRoot != null)
                weaponUIs[currentIndex].eSkillRoot.SetActive(true);
            if (weaponUIs[currentIndex].qSkillRoot != null)
                weaponUIs[currentIndex].qSkillRoot.SetActive(true);
        }

        lastWeaponName = currentName;
    }

    /// <summary>Cooldown fill, countdown text, ready LED, and ready sound for skill E.</summary>
    private void HandleSkillE()
    {
        int index = GetCurrentUIIndex();
        if (index == -1)
            return;

        var ui = weaponUIs[index];
        var activeWeapon = combatScript.activeWeaponData;

        var state = combatScript.GetCurrentWeaponState();
        if (state == null)
            return;

        bool isEReady = state.skillETimer <= 0;

        if (isEReady && !wasEReady)
        {
            if (uiAudioSource != null && eReadySound != null)
                uiAudioSource.PlayOneShot(eReadySound, eReadyVolume);
        }
        wasEReady = isEReady;

        if (state.skillETimer > 0)
        {
            if (ui.eCooldownFill != null)
                ui.eCooldownFill.fillAmount = state.skillETimer / activeWeapon.skillECooldown;

            if (ui.eTimerText != null)
            {
                ui.eTimerText.gameObject.SetActive(true);
                int currentTimer = Mathf.CeilToInt(state.skillETimer);

                if (currentTimer != lastETimerValue)
                {
                    ui.eTimerText.text = currentTimer.ToString();
                    lastETimerValue = currentTimer;

                    if (currentPulseCoroutine != null)
                    {
                        StopCoroutine(currentPulseCoroutine);
                        ui.eTimerText.transform.localScale = Vector3.one;
                    }
                    currentPulseCoroutine = StartCoroutine(PulseText(ui.eTimerText.transform));
                }
            }
        }
        else
        {
            lastETimerValue = -1;
            if (ui.eCooldownFill != null) ui.eCooldownFill.fillAmount = 0;
            if (ui.eTimerText != null) ui.eTimerText.gameObject.SetActive(false);
        }

        if (ui.eFrameLed != null)
            ui.eFrameLed.SetActive(isEReady);
    }

    /// <summary>Q charge dots/fill, ready LED, and ready sound when E hits reach required count.</summary>
    private void HandleSkillQ()
    {
        int index = GetCurrentUIIndex();
        if (index == -1)
            return;

        var ui = weaponUIs[index];
        var activeWeapon = combatScript.activeWeaponData;

        var state = combatScript.GetCurrentWeaponState();
        if (state == null)
            return;

        bool isQReady = state.currentE_Count >= activeWeapon.requiredE_For_Q;

        if (isQReady && !wasQReady)
        {
            if (uiAudioSource != null && qReadySound != null)
                uiAudioSource.PlayOneShot(qReadySound, qReadyVolume);
        }
        wasQReady = isQReady;

        if (ui.qChargeDots != null && ui.qChargeDots.Length > 0)
            UpdateQChargeDots(ui, state.currentE_Count, activeWeapon.requiredE_For_Q);

        if (ui.qCooldownFill != null && activeWeapon.requiredE_For_Q > 0)
        {
            float fillRatio = (float)state.currentE_Count / activeWeapon.requiredE_For_Q;
            ui.qCooldownFill.fillAmount = 1f - fillRatio;
        }

        if (ui.qFrameLed != null)
            ui.qFrameLed.SetActive(isQReady);
    }

    private void UpdateQChargeDots(WeaponUIProfile ui, int currentCount, int requiredCount)
    {
        for (int i = 0; i < ui.qChargeDots.Length; i++)
        {
            QChargeDot dot = ui.qChargeDots[i];
            bool slotActive = i < requiredCount;
            bool isLit = i < currentCount;

            if (dot.slotRoot != null)
                dot.slotRoot.SetActive(slotActive);

            if (dot.iconOff != null)
                dot.iconOff.SetActive(slotActive && !isLit);

            if (dot.iconOn != null)
                dot.iconOn.SetActive(slotActive && isLit);
        }
    }

    private void ResetQChargeDots(WeaponUIProfile ui, int currentCount, int visibleSlots)
    {
        if (ui.qChargeDots == null || ui.qChargeDots.Length == 0)
            return;

        UpdateQChargeDots(ui, currentCount, visibleSlots);
    }

    /// <summary>Quick scale bounce when the E cooldown number ticks down.</summary>
    private IEnumerator PulseText(Transform textTransform)
    {
        float duration = 0.2f;
        float elapsed = 0f;
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = new Vector3(1.4f, 1.4f, 1.4f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            textTransform.localScale = Vector3.Lerp(originalScale, targetScale, Mathf.PingPong(t * 2, 1));
            yield return null;
        }

        textTransform.localScale = originalScale;
    }
}
