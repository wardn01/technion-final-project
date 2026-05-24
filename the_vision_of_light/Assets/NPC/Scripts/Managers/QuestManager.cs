using UnityEngine;
using System.Collections.Generic;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;
    public int mainQuestState = 0;

    public List<QuestData> allQuestLibrary; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SaveQuestProgress()
    {
        if (WorldSaveManager.Instance != null)
            WorldSaveManager.Instance.SaveCurrentWorld();
    }
}