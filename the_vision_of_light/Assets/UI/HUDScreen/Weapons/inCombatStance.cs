using UnityEngine;

public class WeaponUI : MonoBehaviour
{
    [Header("References")]
    public PlayerCombat combatScript;

    [Header("UI Objects")]
    public GameObject swordIconObj;
    public GameObject handIconObj;

    void Update()
    {
        if (combatScript == null) return;

        bool isDrawn = combatScript.inCombatStance;

        if (swordIconObj != null) 
            swordIconObj.SetActive(isDrawn);

        if (handIconObj != null) 
            handIconObj.SetActive(!isDrawn);
    }
}