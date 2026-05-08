using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Global UI Panels")]
    public GameObject inventoryPanel;
    public GameObject mapPanel;
    public GameObject shopPanel;
    public GameObject attributesPanel;
    
    [HideInInspector] public bool isDialogueOpen = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public bool IsAnyOtherPanelOpen(GameObject callerPanel)
    {
        if (inventoryPanel != null && inventoryPanel.activeSelf && inventoryPanel != callerPanel) return true;
        if (mapPanel != null && mapPanel.activeSelf && mapPanel != callerPanel) return true;
        if (shopPanel != null && shopPanel.activeSelf && shopPanel != callerPanel) return true;
        if (attributesPanel != null && attributesPanel.activeSelf && attributesPanel != callerPanel) return true;
        if (isDialogueOpen) return true;

        return false; 
    }
}