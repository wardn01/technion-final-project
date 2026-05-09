using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; 

public class PlayerSkillsUI : MonoBehaviour
{
    [Header("References")]
    public PlayerCombat combatScript; 

    [System.Serializable]
    public class WeaponUIProfile
    {
        public string weaponName; 
        
        [Header("Root Objects to Toggle")]
        public GameObject eSkillRoot;
        public GameObject qSkillRoot;
        
        [Header("E Skill UI")]
        public Image eCooldownFill;
        public GameObject eFrameLed;
        public TextMeshProUGUI eTimerText;
        
        [Header("Q Skill UI")]
        public Image qCooldownFill;
        public GameObject qFrameLed;
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

    void Start()
    {
        foreach (var ui in weaponUIs)
        {
            if (ui.eTimerText != null) ui.eTimerText.gameObject.SetActive(false);
            if (ui.eCooldownFill != null) ui.eCooldownFill.fillAmount = 0;
            if (ui.qCooldownFill != null) ui.qCooldownFill.fillAmount = 1;
            
            if (ui.eSkillRoot != null) ui.eSkillRoot.SetActive(false);
            if (ui.qSkillRoot != null) ui.qSkillRoot.SetActive(false);
        }
    }

    void Update()
    {
        if (combatScript == null || combatScript.activeWeaponData == null || weaponUIs.Length == 0) return;

        UpdateWeaponUI(); 
        HandleSkillE();
        HandleSkillQ();
    }

    private int GetCurrentUIIndex()
    {
        if (combatScript.activeWeaponData == null) return -1;
        
        for (int i = 0; i < weaponUIs.Length; i++)
        {
            if (weaponUIs[i].weaponName == combatScript.activeWeaponData.itemName)
            {
                return i;
            }
        }
        return -1;
    }

    void UpdateWeaponUI()
    {
        string currentName = combatScript.activeWeaponData.itemName;
        
        if (currentName != lastWeaponName)
        {
            foreach (var ui in weaponUIs)
            {
                if (ui.eSkillRoot != null) ui.eSkillRoot.SetActive(false);
                if (ui.qSkillRoot != null) ui.qSkillRoot.SetActive(false);
            }

            int currentIndex = GetCurrentUIIndex();
            if (currentIndex != -1)
            {
                if (weaponUIs[currentIndex].eSkillRoot != null) weaponUIs[currentIndex].eSkillRoot.SetActive(true);
                if (weaponUIs[currentIndex].qSkillRoot != null) weaponUIs[currentIndex].qSkillRoot.SetActive(true);
            }

            lastWeaponName = currentName;
        }
    }

    void HandleSkillE()
    {
        int index = GetCurrentUIIndex();
        if (index == -1) return;

        var ui = weaponUIs[index]; 
        var activeWeapon = combatScript.activeWeaponData; 
        
        var state = combatScript.GetCurrentWeaponState();
        if (state == null) return;

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

        if (ui.eFrameLed != null) ui.eFrameLed.SetActive(isEReady);
    }

    void HandleSkillQ()
    {
        int index = GetCurrentUIIndex();
        if (index == -1) return;

        var ui = weaponUIs[index];
        var activeWeapon = combatScript.activeWeaponData;
        
        var state = combatScript.GetCurrentWeaponState();
        if (state == null) return;

        bool isQReady = state.currentE_Count >= activeWeapon.requiredE_For_Q;

        if (isQReady && !wasQReady)
        {
            if (uiAudioSource != null && qReadySound != null)
                uiAudioSource.PlayOneShot(qReadySound, qReadyVolume);
        }
        wasQReady = isQReady;

        if (ui.qCooldownFill != null)
        {
            float fillRatio = (float)state.currentE_Count / activeWeapon.requiredE_For_Q;
            ui.qCooldownFill.fillAmount = 1f - fillRatio; 
        }

        if (ui.qFrameLed != null) ui.qFrameLed.SetActive(isQReady);
    }

    IEnumerator PulseText(Transform textTransform)
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