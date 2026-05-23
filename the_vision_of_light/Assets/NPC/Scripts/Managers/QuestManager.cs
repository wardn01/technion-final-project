using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    public int mainQuestState = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void SaveQuestProgress()
    {
        if (WorldSaveManager.Instance != null)
            WorldSaveManager.Instance.SaveCurrentWorld();
    }
}