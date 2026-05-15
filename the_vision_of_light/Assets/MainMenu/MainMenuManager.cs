using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject playPanel;
    public GameObject settingsPanel;
    public GameObject blurOverlay;
    public GameObject confirmDeletePanel;
    
    [Header("New Game Section")]
    public TMP_InputField nameInputField;
    public Button newGameButton;
    public TMP_Text maxLimitText;

    [Header("Load Game Section")]
    public Transform loadGameContainer;
    public GameObject slotPrefab;

    [Header("Confirm Delete Section")]
    public TMP_Text confirmWorldNameText;
    private int slotToDelete = -1;

    [Header("Settings Tabs")]
    public GameObject graphicsTab;
    public GameObject audioTab;
    public GameObject keyTab;

    private void Start()
    {
        playPanel.SetActive(false);
        settingsPanel.SetActive(false);
        blurOverlay.SetActive(false);
        confirmDeletePanel.SetActive(false);
    }

    public void OpenPlayMenu()
    {
        blurOverlay.SetActive(true);
        playPanel.SetActive(true);
        RefreshSlots();
    }

    public void ClosePlayMenu()
    {
        blurOverlay.SetActive(false);
        playPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        blurOverlay.SetActive(true);
        settingsPanel.SetActive(true);
        ShowGraphicsTab(); 
    }

    public void CloseSettings()
    {
        blurOverlay.SetActive(false);
        settingsPanel.SetActive(false);
    }

    public void ShowGraphicsTab()
    {
        graphicsTab.SetActive(true);
        audioTab.SetActive(false);
        keyTab.SetActive(false);
    }

    public void ShowAudioTab()
    {
        graphicsTab.SetActive(false);
        audioTab.SetActive(true);
        keyTab.SetActive(false);
    }

    public void ShowKeyTab()
    {
        graphicsTab.SetActive(false);
        audioTab.SetActive(false);
        keyTab.SetActive(true);
    }

    public void RefreshSlots()
    {
        foreach (Transform child in loadGameContainer)
        {
            Destroy(child.gameObject);
        }

        int activeWorldsCount = 0;

        for (int i = 1; i <= 3; i++)
        {
            if (PlayerPrefs.GetInt("Slot_" + i + "_Exists", 0) == 1)
            {
                activeWorldsCount++;
                GameObject newSlot = Instantiate(slotPrefab, loadGameContainer);
                
                string worldName = PlayerPrefs.GetString("Slot_" + i + "_Name", "MyWorld " + i);
                newSlot.transform.Find("Btn_LoadGame").GetComponentInChildren<TMP_Text>().text = worldName;

                int slotIndex = i;
                newSlot.transform.Find("Btn_LoadGame").GetComponent<Button>().onClick.AddListener(() => LoadWorld(slotIndex));
                newSlot.transform.Find("Btn_DeleteGame").GetComponent<Button>().onClick.AddListener(() => ShowConfirmDelete(slotIndex, worldName));
            }
        }

        maxLimitText.text = activeWorldsCount + "/3";

        if (activeWorldsCount >= 3)
        {
            newGameButton.interactable = false;
            nameInputField.interactable = false;
            nameInputField.text = "";
        }
        else
        {
            newGameButton.interactable = true;
            nameInputField.interactable = true;
        }
    }

    public void CreateNewGame()
    {
        string worldName = nameInputField.text.Trim();
        if (string.IsNullOrEmpty(worldName)) worldName = "Unknown World";

        int emptySlot = -1;
        for (int i = 1; i <= 3; i++)
        {
            if (PlayerPrefs.GetInt("Slot_" + i + "_Exists", 0) == 0)
            {
                emptySlot = i;
                break;
            }
        }

        if (emptySlot != -1)
        {
            PlayerPrefs.SetInt("Slot_" + emptySlot + "_Exists", 1);
            PlayerPrefs.SetString("Slot_" + emptySlot + "_Name", worldName);
            
            nameInputField.text = "";
            RefreshSlots();
        }
    }

    public void LoadWorld(int slotIndex)
    {
        PlayerPrefs.SetInt("SelectedSlot", slotIndex);
        SceneManager.LoadScene("World");
    }

    public void ShowConfirmDelete(int slotIndex, string worldName)
    {
        slotToDelete = slotIndex;
        confirmWorldNameText.text = "Delete " + worldName + "?";
        confirmDeletePanel.SetActive(true);
    }

    public void ConfirmDelete()
    {
        if (slotToDelete != -1)
        {
            PlayerPrefs.SetInt("Slot_" + slotToDelete + "_Exists", 0);
            slotToDelete = -1;
            confirmDeletePanel.SetActive(false);
            RefreshSlots();
        }
    }

    public void CancelDelete()
    {
        slotToDelete = -1;
        confirmDeletePanel.SetActive(false);
    }

    public void ExitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}