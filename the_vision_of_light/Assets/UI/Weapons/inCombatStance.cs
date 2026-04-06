using UnityEngine;

public class WeaponUI : MonoBehaviour
{
    [Header("References")]
    public PlayerCombat combatScript; // اسحب اللاعب هون

    [Header("UI Objects")]
    public GameObject swordIconObj;   // كائن السيف الكامل
    public GameObject handIconObj;    // كائن الإيد الكامل

    void Update()
    {
        if (combatScript == null) return;

        // فحص إذا السيف مسحوب
        bool isDrawn = combatScript.inCombatStance;

        // --- تطبيق فكرتك: إظهار وإخفاء الكائنات ---
        if (swordIconObj != null) 
            swordIconObj.SetActive(isDrawn);   // بيشتغل لما تسحب السيف

        if (handIconObj != null) 
            handIconObj.SetActive(!isDrawn);   // بيشتغل لما تكون الإيد فاضية (! معناها العكس)
    }
}