using UnityEngine;
using UnityEngine.UI;

public class PlayerSkillsUI : MonoBehaviour
{
    [Header("References")]
    public PlayerCombat combatScript; // اسحب اللاعب (Xiao) هون

    [Header("Skill E Settings")]
    public Image eCooldownFill;      // اسحب صورة التعبئة تبعت الـ E
    public GameObject eFrameLed;     // اسحب كائن الضوء (Frame_Led) تبع الـ E

    [Header("Skill Q Settings")]
    public Image qCooldownFill;      // اسحب صورة التعبئة (القفل الأسود) تبعت الـ Q
    public GameObject qFrameLed;     // اسحب كائن الضوء (Frame_Led) تبع الـ Q

    void Update()
    {
        if (combatScript == null) return;

        HandleSkillE();
        HandleSkillQ();
    }

    void HandleSkillE()
    {
        if (eCooldownFill != null)
        {
            // إذا الـ Timer أكبر من 0 يعني المهارة بتشحن
            if (combatScript.skillETimer > 0)
            {
                eCooldownFill.fillAmount = combatScript.skillETimer / combatScript.skillECooldown;
            }
            else
            {
                eCooldownFill.fillAmount = 0;
            }
        }

        // تشغيل ضوء الإطار لما الـ E تجهز
        if (eFrameLed != null)
        {
            eFrameLed.SetActive(combatScript.skillETimer <= 0);
        }
    }

    void HandleSkillQ()
    {
        bool isQReady = combatScript.currentE_Count >= combatScript.requiredE_For_Q;

        // تعبئة القفل الأسود للـ Q تدريجياً
        if (qCooldownFill != null)
        {
            float fillRatio = (float)combatScript.currentE_Count / combatScript.requiredE_For_Q;
            qCooldownFill.fillAmount = 1f - fillRatio; 
        }

        // تشغيل ضوء الإطار لما الـ Q تجهز
        if (qFrameLed != null)
        {
            qFrameLed.SetActive(isQReady);
        }
    }
}