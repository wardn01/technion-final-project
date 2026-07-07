using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// Interactive challenge stone: spawns enemy waves for one-time world trials or repeatable training.
    /// Uses the global Interact keybind (<see cref="ShopManager.GetInteractKey"/>) and persists progress via
    /// <see cref="ChallengeTrialRegistry"/>.
    /// </summary>
    public class ChallengeStone : MonoBehaviour
    {
        public enum ChallengeStoneType
        {
            /// <summary>Cleared once per save — the Interact key prompt never returns; rift stays visible.</summary>
            OneTime,
            /// <summary>Trial waves can be started again after each clear (no save lock).</summary>
            RepeatableTraining
        }

        #region Data Types
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
            [Tooltip("-1 = any step within the quest state.")]
            public int targetQuestStep = -1;
            public int nextQuestState = 7;
            [Tooltip("When enabled, clears the wave during the current quest step and calls AdvanceStep instead of CompleteCurrentQuest.")]
            public bool advanceQuestStepOnSuccess;
            public Wave[] waves;
        }
        #endregion

        #region Inspector
        [Header("Trial Identity")]
        [Tooltip("Unique save ID — must differ between stones in the same world.")]
        public string trialId = "trial_001";

        public ChallengeStoneType stoneType = ChallengeStoneType.RepeatableTraining;

        [Header("UI")]
        [Tooltip("Root UI shown near the stone (e.g. Interact_F). Hidden while the player is away or cannot interact.")]
        public GameObject promptContainer;

        [Tooltip("Optional parent of promptContainer (e.g. InteractPrompt). Hidden with the prompt so empty UI rects do not block clicks.")]
        public GameObject promptRoot;

        [Tooltip("The key badge only (e.g. Interact_F/F). Stays hidden after a one-time clear or during repeatable cooldown.")]
        public GameObject interactKeyPrompt;

        [Tooltip("Optional label such as \"Start Trial\".")]
        public TextMeshProUGUI promptTextUI;

        public string promptText = "Start Trial";

        [Header("Timer")]
        [Tooltip("Seconds to clear all waves. 0 = no limit.")]
        public float challengeTimeLimit = 180f;

        public string successMessage = "Challenge Complete";
        public string failMessage = "Challenge Failed";

        [Header("Quest Challenges")]
        [Tooltip("F appears only when mainQuestState matches an entry. Each entry clears once per save.")]
        public QuestChallenge[] questChallenges;

        [Header("Waves")]
        [Tooltip("Used when Quest Challenges is empty. OneTime = once per stone. Repeatable = can retry anytime.")]
        public Wave[] trialWaves;

        [Header("Rift Visuals")]
        public RiftController riftVisualController;

        [Header("Debug")]
        [SerializeField] private int currentWaveIndex;
        [SerializeField] private bool challengeActive;
        [SerializeField] private bool isSpawning;

        public bool IsChallengeActive => challengeActive;
        #endregion

        #region Runtime State
        private Wave[] activeWaves;
        private QuestChallenge activeQuestChallenge;
        private readonly List<EnemyBase> aliveEnemies = new List<EnemyBase>();
        private readonly List<GameObject> spawnedEnemyObjects = new List<GameObject>();
        private Transform player;
        private bool isPlayerNear;
        private float challengeStartedAt;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;

            if (riftVisualController != null)
                RefreshRiftIdleVisual();

            ApplyPermanentInteractKeyState();
            ResolvePromptRoot();
            HidePrompt();
        }

        private void Update()
        {
            if (player == null)
                return;

            if (!challengeActive)
            {
                RefreshRiftIdleVisual();
                HandleIdleInteraction();
                return;
            }

            if (!isSpawning)
                HandleActiveChallenge();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
                isPlayerNear = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            isPlayerNear = false;
            HideInteractPrompt();
        }
        #endregion

        #region Interaction
        private void HandleIdleInteraction()
        {
            if (!isPlayerNear)
                return;

            if (!CanShowPrompt())
            {
                HidePrompt();
                ApplyPermanentInteractKeyState();
                return;
            }

            if (IsUiBlockingInteraction())
            {
                HidePrompt();
                return;
            }

            ShowPrompt();
            SetInteractKeyVisible(true);

            if (Input.GetKeyDown(ShopManager.GetInteractKey()))
            {
                HidePrompt();
                TryStartChallenge();
            }
        }

        private static bool IsUiBlockingInteraction()
        {
            if (Time.timeScale == 0f)
                return true;

            if (ShopManager.Instance != null && ShopManager.Instance.shopPanel.activeSelf)
                return true;

            if (UIManager.Instance != null && UIManager.Instance.isDialogueOpen)
                return true;

            return false;
        }

        private void ShowPrompt()
        {
            if (promptRoot != null && !promptRoot.activeSelf)
                promptRoot.SetActive(true);

            if (promptContainer != null && !promptContainer.activeSelf)
                promptContainer.SetActive(true);

            if (promptTextUI == null)
                return;

            if (!promptTextUI.gameObject.activeSelf)
                promptTextUI.gameObject.SetActive(true);

            promptTextUI.text = promptText;
        }

        private void HidePrompt()
        {
            HideInteractPrompt();

            if (challengeActive)
                return;

            if (promptRoot != null && promptRoot.activeSelf)
                promptRoot.SetActive(false);
        }

        /// <summary>Hides the F key / label only. Never touches the challenge HUD (WaveChallenge).</summary>
        private void HideInteractPrompt()
        {
            if (promptContainer != null && promptContainer.activeSelf)
                promptContainer.SetActive(false);

            SetInteractKeyVisible(false);
        }

        private void ResolvePromptRoot()
        {
            if (promptRoot != null || promptContainer == null)
                return;

            Transform parent = promptContainer.transform.parent;
            if (parent != null)
                promptRoot = parent.gameObject;
        }

        private void SetInteractKeyVisible(bool visible)
        {
            if (interactKeyPrompt == null)
                return;

            if (interactKeyPrompt.activeSelf != visible)
                interactKeyPrompt.SetActive(visible);
        }

        /// <summary>
        /// One-time clears permanently hide the key badge. The rift mesh is never disabled.
        /// </summary>
        private void ApplyPermanentInteractKeyState()
        {
            if (HasQuestChallenges())
                return;

            if (stoneType != ChallengeStoneType.OneTime)
                return;

            if (string.IsNullOrEmpty(trialId) || !ChallengeTrialRegistry.IsOneTimeCompleted(trialId))
                return;

            SetInteractKeyVisible(false);
        }

        private bool HasQuestChallenges()
        {
            return questChallenges != null && questChallenges.Length > 0;
        }

        private bool CanShowPrompt()
        {
            if (HasQuestChallenges())
                return TryResolveQuestChallenge(out _);

            if (string.IsNullOrEmpty(trialId))
                return true;

            if (stoneType == ChallengeStoneType.OneTime)
                return !ChallengeTrialRegistry.IsOneTimeCompleted(trialId);

            return true;
        }
        #endregion

        #region Challenge Flow
        /// <summary>Called by <see cref="ChallengeArenaFailZone"/> when the player leaves the arena trigger.</summary>
        public void NotifyPlayerLeftArena(float graceSeconds)
        {
            if (!challengeActive)
                return;

            if (graceSeconds > 0f && Time.time - challengeStartedAt < graceSeconds)
                return;

            FinishChallenge(success: false);
        }

        private void TryStartChallenge()
        {
            if (!CanShowPrompt())
                return;

            activeQuestChallenge = null;
            activeWaves = ResolveActiveWaves();

            if (activeWaves == null || activeWaves.Length == 0)
                return;

            challengeActive = true;
            challengeStartedAt = Time.time;
            currentWaveIndex = 0;
            aliveEnemies.Clear();
            spawnedEnemyObjects.Clear();

            if (riftVisualController != null)
                riftVisualController.SetVisualState(RiftController.RiftVisualState.ActiveLilac);

            BeginChallengeTimer();
            StartCoroutine(SpawnWaveRoutine());
        }

        private void BeginChallengeTimer()
        {
            if (challengeTimeLimit <= 0f || ChallengeTimerUI.Instance == null)
                return;

            ChallengeTimerUI.Instance.Begin(challengeTimeLimit, HandleChallengeTimeExpired);
        }

        private void StopChallengeTimer()
        {
            if (ChallengeTimerUI.Instance == null)
                return;

            ChallengeTimerUI.Instance.StopTimer();
        }

        private Wave[] ResolveActiveWaves()
        {
            if (TryResolveQuestChallenge(out QuestChallenge questChallenge))
            {
                activeQuestChallenge = questChallenge;
                return questChallenge.waves;
            }

            if (HasQuestChallenges())
                return null;

            if (stoneType == ChallengeStoneType.OneTime)
                return ResolveTrialWaves();

            return ResolveTrialWaves();
        }

        private Wave[] ResolveTrialWaves()
        {
            if (trialWaves != null && trialWaves.Length > 0)
                return trialWaves;

            return null;
        }

        private bool TryResolveQuestChallenge(out QuestChallenge match)
        {
            match = null;

            if (!HasQuestChallenges())
                return false;

            if (QuestManager.Instance == null)
                return false;

            int currentState = QuestManager.Instance.mainQuestState;
            int currentStep = QuestManager.Instance.questStepIndex;

            foreach (QuestChallenge qc in questChallenges)
            {
                if (qc.waves == null || qc.waves.Length == 0)
                    continue;

                if (ChallengeTrialRegistry.IsQuestChallengeCompleted(trialId, qc.targetQuestState, qc.targetQuestStep))
                    continue;

                bool stateMatches = qc.targetQuestState == currentState;
                bool stepMatches = qc.targetQuestStep < 0 || qc.targetQuestStep == currentStep;

                if (stateMatches && stepMatches)
                {
                    match = qc;
                    return true;
                }
            }

            return false;
        }

        private void HandleActiveChallenge()
        {
            if (!challengeActive)
                return;

            aliveEnemies.RemoveAll(enemy => enemy == null || enemy.IsDead);

            if (aliveEnemies.Count > 0)
                return;

            currentWaveIndex++;

            if (activeWaves != null && currentWaveIndex < activeWaves.Length)
                StartCoroutine(SpawnWaveRoutine());
            else
                FinishChallenge(success: true);
        }

        private IEnumerator SpawnWaveRoutine()
        {
            isSpawning = true;

            Wave currentWave = activeWaves[currentWaveIndex];
            if (currentWave.enemiesToSpawn == null)
            {
                isSpawning = false;
                yield break;
            }

            foreach (EnemySpawnInfo info in currentWave.enemiesToSpawn)
            {
                if (!challengeActive)
                    break;

                if (info.enemyPrefab == null)
                    continue;

                for (int i = 0; i < info.count; i++)
                {
                    if (!challengeActive)
                        break;

                    Transform spawnPoint = PickSpawnPoint(info.spawnPoints);
                    Vector3 spawnPos = spawnPoint.position + Vector3.up * 2f;
                    GameObject enemyObj = Instantiate(info.enemyPrefab, spawnPos, spawnPoint.rotation);
                    spawnedEnemyObjects.Add(enemyObj);

                    if (enemyObj.TryGetComponent(out EnemyBase enemy))
                        aliveEnemies.Add(enemy);

                    yield return new WaitForSeconds(currentWave.delayBetweenSpawns);
                }
            }

            isSpawning = false;
        }

        private Transform PickSpawnPoint(Transform[] spawnPoints)
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
                return spawnPoints[Random.Range(0, spawnPoints.Length)];

            return transform;
        }

        private void HandleChallengeTimeExpired()
        {
            if (!challengeActive)
                return;

            FinishChallenge(success: false);
        }

        private void FinishChallenge(bool success)
        {
            challengeActive = false;
            isSpawning = false;
            StopAllCoroutines();
            StopChallengeTimer();

            if (success)
            {
                CompleteChallengeSuccess();
            }
            else
            {
                FailChallenge();
            }
        }

        private void CompleteChallengeSuccess()
        {
            ClearSpawnedEnemies();
            aliveEnemies.Clear();
            spawnedEnemyObjects.Clear();

            if (activeQuestChallenge != null && QuestManager.Instance != null)
            {
                if (activeQuestChallenge.advanceQuestStepOnSuccess)
                {
                    QuestManager.Instance.AdvanceStep(
                        activeQuestChallenge.targetQuestState,
                        activeQuestChallenge.targetQuestStep);
                }
                else if (activeQuestChallenge.nextQuestState > QuestManager.Instance.mainQuestState)
                    QuestManager.Instance.AdvanceToState(activeQuestChallenge.nextQuestState);
                else
                    QuestManager.Instance.CompleteCurrentQuest();

                ChallengeTrialRegistry.MarkQuestChallengeCompleted(
                    trialId,
                    activeQuestChallenge.targetQuestState,
                    activeQuestChallenge.targetQuestStep);
            }
            else if (stoneType == ChallengeStoneType.OneTime)
            {
                ChallengeTrialRegistry.MarkOneTimeCompleted(trialId);
                SetInteractKeyVisible(false);
            }

            activeQuestChallenge = null;
            activeWaves = null;

            RefreshRiftIdleVisual();

            if (ChallengeTimerUI.Instance != null)
                ChallengeTimerUI.Instance.ShowResult(successMessage, success: true);

            if (PauseMenuManager.Instance != null)
                PauseMenuManager.Instance.SaveGameSilently();
        }

        private void FailChallenge()
        {
            ClearSpawnedEnemies();
            aliveEnemies.Clear();
            spawnedEnemyObjects.Clear();

            activeQuestChallenge = null;
            activeWaves = null;

            RefreshRiftIdleVisual();

            if (ChallengeTimerUI.Instance != null)
                ChallengeTimerUI.Instance.ShowResult(failMessage, success: false);
        }

        private void RefreshRiftIdleVisual()
        {
            if (riftVisualController == null || challengeActive)
                return;

            if (CanShowPrompt())
            {
                riftVisualController.SetVisualState(RiftController.RiftVisualState.WaitingBlackGlow);
                return;
            }

            riftVisualController.SetVisualState(RiftController.RiftVisualState.Off);
        }

        private void ClearSpawnedEnemies()
        {
            for (int i = spawnedEnemyObjects.Count - 1; i >= 0; i--)
            {
                GameObject enemyObj = spawnedEnemyObjects[i];
                if (enemyObj != null)
                    Destroy(enemyObj);
            }
        }
        #endregion
    }
}
