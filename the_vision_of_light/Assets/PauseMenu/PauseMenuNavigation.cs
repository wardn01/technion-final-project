using UnityEngine;
using UnityEngine.UI;

public class PauseMenuNavigation : MonoBehaviour
{
    [Header("Menu Screens")]
    public GameObject pauseMenuScreen;
    public GameObject mapScreen;
    public GameObject inventoryScreen;
    public GameObject setupScreen;
    public GameObject questScreen;

    [Header("Menu Buttons")]
    public Button mapBtn;
    public Button inventoryBtn;
    public Button setupBtn;
    public Button questsBtn;

    private void Start()
    {
        if (mapBtn != null) mapBtn.onClick.AddListener(OpenMap);
        if (inventoryBtn != null) inventoryBtn.onClick.AddListener(OpenInventory);
        if (setupBtn != null) setupBtn.onClick.AddListener(OpenSetup);
        if (questsBtn != null) questsBtn.onClick.AddListener(OpenQuests);
    }

    public void OpenMap()
    {
        if (pauseMenuScreen != null) pauseMenuScreen.SetActive(false);
        if (mapScreen != null) mapScreen.SetActive(true);
    }

    public void OpenInventory()
    {
        if (pauseMenuScreen != null) pauseMenuScreen.SetActive(false);
        if (inventoryScreen != null) inventoryScreen.SetActive(true);
    }

    public void OpenSetup()
    {
        if (pauseMenuScreen != null) pauseMenuScreen.SetActive(false);
        if (setupScreen != null) setupScreen.SetActive(true);
    }

    public void OpenQuests()
    {
        if (pauseMenuScreen != null) pauseMenuScreen.SetActive(false);
        if (mapScreen != null) mapScreen.SetActive(false);
        if (inventoryScreen != null) inventoryScreen.SetActive(false);
        if (setupScreen != null) setupScreen.SetActive(false);
        
        if (questScreen != null) questScreen.SetActive(true);
    }
}