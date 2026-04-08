using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; 

public class PlayerSkillsUI : MonoBehaviour
{
    [Header("References")]
    public PlayerCombat combatScript; 

    [Header("Skill E Settings")]
    public Image eCooldownFill;      
    public GameObject eFrameLed;     
    public TextMeshProUGUI eTimerText; 

    [Header("Skill Q Settings")]
    public Image qCooldownFill;      
    public GameObject qFrameLed;     

    [Header("Audio Settings")]
    public float eReadyVolume = 0.2f;
    public float qReadyVolume = 0.4f;
    public AudioSource uiAudioSource;
    public AudioClip eReadySound;
    public AudioClip qReadySound;

    private int lastETimerValue = -1;
    private bool wasEReady = true;
    private bool wasQReady = true;

    private Coroutine currentPulseCoroutine;

    void Start()
    {
        if (eTimerText != null) eTimerText.gameObject.SetActive(false);
        if (eCooldownFill != null) eCooldownFill.fillAmount = 0;
        if (qCooldownFill != null) qCooldownFill.fillAmount = 1;
    }

    void Update()
    {
        if (combatScript == null) return;

        HandleSkillE();
        HandleSkillQ();
    }

    void HandleSkillE()
    {
        bool isEReady = combatScript.skillETimer <= 0;

        if (isEReady && !wasEReady)
        {
            if (uiAudioSource != null && eReadySound != null)
            {
                uiAudioSource.PlayOneShot(eReadySound, eReadyVolume);
            }
        }
        wasEReady = isEReady;

        if (combatScript.skillETimer > 0)
        {
            if (eCooldownFill != null)
                eCooldownFill.fillAmount = combatScript.skillETimer / combatScript.skillECooldown;

            if (eTimerText != null)
            {
                eTimerText.gameObject.SetActive(true);
                int currentTimer = Mathf.CeilToInt(combatScript.skillETimer);

                if (currentTimer != lastETimerValue)
                {
                    eTimerText.text = currentTimer.ToString();
                    lastETimerValue = currentTimer;
                    
                    if (currentPulseCoroutine != null)
                    {
                        StopCoroutine(currentPulseCoroutine);
                        eTimerText.transform.localScale = Vector3.one;
                    }
                    currentPulseCoroutine = StartCoroutine(PulseText(eTimerText.transform));
                }
            }
        }
        else
        {
            lastETimerValue = -1; 

            if (eCooldownFill != null)
                eCooldownFill.fillAmount = 0;

            if (eTimerText != null)
                eTimerText.gameObject.SetActive(false);
        }

        if (eFrameLed != null)
        {
            eFrameLed.SetActive(isEReady);
        }
    }

    void HandleSkillQ()
    {
        bool isQReady = combatScript.currentE_Count >= combatScript.requiredE_For_Q;

        if (isQReady && !wasQReady)
        {
            if (uiAudioSource != null && qReadySound != null)
            {
                uiAudioSource.PlayOneShot(qReadySound, qReadyVolume);
            }
        }
        wasQReady = isQReady;

        if (qCooldownFill != null)
        {
            float fillRatio = (float)combatScript.currentE_Count / combatScript.requiredE_For_Q;
            qCooldownFill.fillAmount = 1f - fillRatio; 
        }

        if (qFrameLed != null)
        {
            qFrameLed.SetActive(isQReady);
        }
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