using UnityEngine;

public class MonsterQuestManager : MonoBehaviour
{
    public static MonsterQuestManager Instance;

    [Header("Quest Settings")]
    public int targetQuestState = 3; 
    public int monstersToKill = 3;   
    private int currentKills = 0;

    [Header("Monster Spawning")]
    public GameObject monstersGroup;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (monstersGroup != null)
        {
            monstersGroup.SetActive(false);
        }
    }

    private void Update()
    {
        if (QuestManager.Instance != null && QuestManager.Instance.mainQuestState == targetQuestState)
        {
            if (monstersGroup != null && !monstersGroup.activeSelf && currentKills < monstersToKill)
            {
                monstersGroup.SetActive(true);
            }
        }
    }

    public void MonsterKilled()
    {
        if (QuestManager.Instance != null && QuestManager.Instance.mainQuestState == targetQuestState)
        {
            currentKills++;
            Debug.Log($"Monsters Killed: {currentKills} / {monstersToKill}");

            if (currentKills >= monstersToKill)
            {
                CompleteObjective();
            }
        }
    }

    private void CompleteObjective()
    {
        QuestManager.Instance.mainQuestState = 4;
        QuestManager.Instance.SaveQuestProgress();
        
        Debug.Log("Quest Updated! Return to Albedo.");
    }
}