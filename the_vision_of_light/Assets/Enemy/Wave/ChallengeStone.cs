using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ChallengeStone : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnInfo
    {
        public GameObject enemyPrefab;    
        public int count = 2;              
        public Transform[] spawnPoints;   
    }

    [System.Serializable]
    public class Wave
    {
        public string waveName = "Wave 1"; 
        public EnemySpawnInfo[] enemiesToSpawn; 
        public float delayBetweenSpawns = 1f; 
    }

    [System.Serializable]
    public class QuestChallenge
    {
        public string challengeName = "Quest Trial";
        public int targetQuestState = 6;
        public int nextQuestState = 7;
        public Wave[] waves;
    }

    [Header("Challenge Settings")]
    public KeyCode interactionKey = KeyCode.F; 
    
    [Header("UI Settings")]
    public GameObject promptContainer; 
    public TextMeshProUGUI promptTextUI;
    public string promptText = "Start Trial";

    [Header("Quest Integration (Add Quests Here)")]
    public QuestChallenge[] questChallenges; 
    
    [Header("Default Training (If no quest is active)")]
    public bool allowDefaultTraining = true;
    public Wave[] defaultWaves;

    [Header("Visual Pack Integration")]
    public Rift_Controller riftVisualController; 
    public GameObject riftBaseMesh;

    [Header("Live Data")]
    [SerializeField] private int currentWaveIndex = 0;
    [SerializeField] private bool challengeActive = false;
    [SerializeField] private bool isSpawning = false;
    
    private Wave[] activeWaves;
    private QuestChallenge activeQuestChallenge = null;
    private List<EnemyBase> aliveEnemies = new List<EnemyBase>();
    private Transform player;
    private bool isPlayerNear = false;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        
        if (riftVisualController != null) 
        {
            riftVisualController.F_ToggleRift(false);
        }
    }

    void Update()
    {
        if (player == null) return;

        bool isMenuOpen = (ShopManager.Instance != null && ShopManager.Instance.shopPanel.activeSelf) || 
                          (UIManager.Instance != null && UIManager.Instance.isDialogueOpen);

        if (!challengeActive)
        {
            if (isPlayerNear)
            {
                bool shouldShow = !isMenuOpen && Time.timeScale != 0f;

                if (shouldShow)
                {
                    if (promptContainer != null && !promptContainer.activeSelf) promptContainer.SetActive(true);
                    
                    if (promptTextUI != null)
                    {
                        if (!promptTextUI.gameObject.activeSelf) promptTextUI.gameObject.SetActive(true);
                        promptTextUI.text = promptText;
                    }

                    if (Input.GetKeyDown(interactionKey))
                    {
                        if (promptContainer != null) promptContainer.SetActive(false);
                        TryStartChallenge();
                    }
                }
                else
                {
                    if (promptContainer != null && promptContainer.activeSelf) promptContainer.SetActive(false);
                }
            }
            return;
        }

        if (challengeActive && !isSpawning)
        {
            aliveEnemies.RemoveAll(enemy => enemy == null || enemy.IsDead);

            if (aliveEnemies.Count == 0)
            {
                currentWaveIndex++; 

                if (activeWaves != null && currentWaveIndex < activeWaves.Length)
                {
                    StartCoroutine(SpawnWaveRoutine());
                }
                else
                {
                    FinishChallenge();
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerNear = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            if (promptContainer != null) promptContainer.SetActive(false);
        }
    }

    void TryStartChallenge()
    {
        activeQuestChallenge = null;
        activeWaves = null;

        if (QuestManager.Instance != null)
        {
            int currentState = QuestManager.Instance.mainQuestState;
            foreach (var qc in questChallenges)
            {
                if (qc.targetQuestState == currentState)
                {
                    activeQuestChallenge = qc;
                    activeWaves = qc.waves;
                    break;
                }
            }
        }

        if (activeWaves == null || activeWaves.Length == 0)
        {
            if (allowDefaultTraining && defaultWaves != null && defaultWaves.Length > 0)
            {
                activeWaves = defaultWaves;
            }
            else return; 
        }

        challengeActive = true;
        currentWaveIndex = 0;
        
        if (riftVisualController != null) riftVisualController.F_ToggleRift(true);
        StartCoroutine(SpawnWaveRoutine());
    }

    IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;
        Wave currentWave = activeWaves[currentWaveIndex];
        
        foreach (var info in currentWave.enemiesToSpawn)
        {
            if (info.enemyPrefab == null) continue; 

            for (int i = 0; i < info.count; i++)
            {
                Transform sp = null;
                if (info.spawnPoints != null && info.spawnPoints.Length > 0)
                    sp = info.spawnPoints[Random.Range(0, info.spawnPoints.Length)];
                
                if (sp == null) sp = transform; 

                Vector3 safeSpawnPosition = sp.position + (Vector3.up * 2f);
                GameObject enemyObj = Instantiate(info.enemyPrefab, safeSpawnPosition, sp.rotation);
                
                EnemyBase eScript = enemyObj.GetComponent<EnemyBase>();
                if (eScript != null) aliveEnemies.Add(eScript);

                yield return new WaitForSeconds(currentWave.delayBetweenSpawns);
            }
        }
        isSpawning = false;
    }

    void FinishChallenge()
    {
        challengeActive = false;
        if (riftVisualController != null) riftVisualController.F_ToggleRift(false);

        if (activeQuestChallenge != null && QuestManager.Instance != null)
        {
            QuestManager.Instance.mainQuestState = activeQuestChallenge.nextQuestState;
            QuestManager.Instance.SaveQuestProgress();
        }

        activeQuestChallenge = null;
        activeWaves = null;
    }
}