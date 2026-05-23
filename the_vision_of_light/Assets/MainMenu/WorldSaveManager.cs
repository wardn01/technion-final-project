using UnityEngine;

public class WorldSaveManager : MonoBehaviour
{
    public static WorldSaveManager Instance;

    [HideInInspector]
    public int currentSlot;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        currentSlot = PlayerPrefs.GetInt("SelectedSlot", 1);

        LoadWorldData();
    }

    public void LoadWorldData()
    {
        GameData data = SaveManager.LoadGame(currentSlot);

        if (data != null)
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.mainQuestState = data.mainQuestState;
            }
        }
        else
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.mainQuestState = 0;
            }
        }
    }

    public void SaveCurrentWorld()
    {
        if (PauseMenuManager.Instance != null)
        {
            PauseMenuManager.Instance.SaveGameSilently();
        }
    }
}