using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("Challenge Settings")]
    public Wave[] waves;                  
    public KeyCode interactionKey = KeyCode.F; 
    public float interactionRange = 4f;    

    [Header("Visual Pack Integration")]
    public Rift_Controller riftVisualController; 
    public GameObject riftBaseMesh;

    [Header("Live Data")]
    [SerializeField] private int currentWaveIndex = 0;
    [SerializeField] private bool challengeActive = false;
    [SerializeField] private bool isSpawning = false;
    
    private List<EnemyBase> aliveEnemies = new List<EnemyBase>();
    private Transform player;

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
        if (player == null || waves.Length == 0) return;

        if (!challengeActive)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= interactionRange)
            {
                if (Input.GetKeyDown(interactionKey))
                {
                    StartChallenge();
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

                if (currentWaveIndex < waves.Length)
                {
                    Debug.Log($"[Challenge] Wave {currentWaveIndex} cleared! Starting next...");
                    StartCoroutine(SpawnWaveRoutine());
                }
                else
                {
                    FinishChallenge();
                }
            }
        }
    }

    void StartChallenge()
    {
        challengeActive = true;
        currentWaveIndex = 0;
        
        if (riftVisualController != null)
        {
            riftVisualController.F_ToggleRift(true);
            Debug.Log("[Challenge] Rift Opened!");
        }

        StartCoroutine(SpawnWaveRoutine());
    }

    IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;
        Wave currentWave = waves[currentWaveIndex];
        
        foreach (var info in currentWave.enemiesToSpawn)
        {
            if (info.enemyPrefab == null) continue; 

            for (int i = 0; i < info.count; i++)
            {
                Transform sp = null;
                if (info.spawnPoints != null && info.spawnPoints.Length > 0)
                {
                    sp = info.spawnPoints[Random.Range(0, info.spawnPoints.Length)];
                }
                
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
        
        if (riftVisualController != null)
        {
            riftVisualController.F_ToggleRift(false);
            Debug.Log("[Challenge] Rift Closed.");
        }
        
        Debug.Log("🎉 Challenge Completed!");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}