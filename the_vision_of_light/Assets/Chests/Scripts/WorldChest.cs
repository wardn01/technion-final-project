using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using VisionOfLight.Enemy;

namespace VisionOfLight.Chest
{
    /// <summary>
    /// One-time world loot chest. Supports immediate open or defeat-guardians-first unlock.
    /// Guardians respawn on a real-world timer even after the chest was opened. The chest itself never returns.
    /// Assign a <see cref="ChestLootTable"/> for loot. Wire <see cref="promptRoot"/> on each chest instance in the scene.
    /// </summary>
    public class WorldChest : MonoBehaviour
    {
        public enum ChestUnlockMode
        {
            /// <summary>Open with Interact as soon as the player is in range.</summary>
            Immediate,
            /// <summary>Player must defeat assigned/spawned guardians before opening.</summary>
            DefeatEnemies
        }

        #region Data Types
        [System.Serializable]
        public class GuardSpawnInfo
        {
            public GameObject enemyPrefab;
            public int count = 1;
            public Transform[] spawnPoints;
        }

        private struct GuardianPlacement
        {
            public Vector3 position;
            public Quaternion rotation;
            public GameObject prefab;
        }
        #endregion

        #region Inspector
        [Header("Identity")]
        [Tooltip("Unique save ID per chest in the world.")]
        public string chestId = "chest_001";

        public ChestVisualType visualType = ChestVisualType.Wood;

        [Header("Unlock")]
        public ChestUnlockMode unlockMode = ChestUnlockMode.Immediate;

        [Tooltip("Pre-placed enemies in the scene that must die before this chest unlocks.")]
        public EnemyBase[] assignedGuardians;

        [Tooltip("Optional enemies spawned when the player first enters range.")]
        public GuardSpawnInfo[] guardSpawns;

        [Header("Guardian Respawn")]
        [Tooltip("Respawn all guardians this many real-world seconds after they were last defeated. 3600 = 1 hour.")]
        public float guardianRespawnIntervalSeconds = 3600f;

        [Tooltip("0 = guardians always present. Above 0 = guardians only exist while the player is within this distance (spawn on approach, despawn when far).")]
        public float guardianActivationRadius = 0f;

        [Tooltip("Extra distance beyond the activation radius before guardians despawn (prevents flicker at the edge).")]
        public float guardianActivationBuffer = 5f;

        [Tooltip("Fallback prefab used only if a guardian below has no matching entry. Each guardian normally respawns from Guardian Respawn Prefabs.")]
        public GameObject assignedGuardianRespawnPrefab;

        [Tooltip("Drag the enemy PREFAB (from Project) for each guardian, in the SAME order as Assigned Guardians. Each guardian respawns as its own type.")]
        public List<GameObject> guardianRespawnPrefabs = new List<GameObject>();

        [Header("Loot")]
        [Tooltip("Reusable loot table. Fill entries on the asset when placing chests in the world.")]
        public ChestLootTable lootTable;

        [Header("UI")]
        [Tooltip("Shared InteractPrompt root from the scene. Assign on each placed chest — no runtime scene scan.")]
        [SerializeField] private GameObject promptRoot;

        [Tooltip("Optional. Child prompt panel (Interact_F). Resolved from promptRoot when empty.")]
        public GameObject promptContainer;
        public GameObject interactKeyPrompt;

        public TextMeshProUGUI promptTextUI;

        public string openPromptText = "Open Chest";
        public string lockedPromptText = "Defeat enemies";

        [Header("Open Feedback")]
        public AudioClip openSound;

        [Range(0f, 1f)] public float openSoundVolume = 1f;

        [Header("Lid Open")]
        [Tooltip("Optional. Auto-finds chest cover / roof / lid child when empty.")]
        public Transform lidTransform;

        public float lidOpenAngle = -105f;
        public float lidOpenDuration = 0.5f;

        [Tooltip("Seconds for the whole chest to fade out after opening.")]
        public float fadeOutDuration = 1.5f;
        #endregion

        #region Runtime State
        private readonly List<EnemyBase> trackedGuardians = new List<EnemyBase>();
        private readonly Dictionary<Renderer, Color> baseColors = new Dictionary<Renderer, Color>();
        private MaterialPropertyBlock fadePropertyBlock;
        private Collider[] interactionColliders;
        private AudioSource audioSource;
        private Renderer[] cachedFadeRenderers;
        private bool isPlayerNear;
        private bool guardsSpawned;
        private bool isOpening;
        private bool chestVisuallyOpened;
        private bool guardiansWereCleared;
        private bool proximityGuardiansActive;
        private bool proximityDefeated;
        private Transform playerTransform;
        private readonly List<GuardianPlacement> assignedGuardianPlacements = new List<GuardianPlacement>();
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            CacheFadeRenderers();
            CacheBaseColors();
            interactionColliders = GetComponentsInChildren<Collider>(true);
            EnsureAudioSource();
            ResolveLidTransform();
        }

        private void Start()
        {
            CacheAssignedGuardianPlacements();

            if (ChestRegistry.IsOpened(chestId))
            {
                chestVisuallyOpened = true;
                ApplyOpenedVisualState();
            }
            else
            {
                ResolveSharedInteractUi();
                ResolvePromptRoot();
            }

            if (IsProximityGuardianMode())
            {
                InitProximityGuardians();
                HidePrompt();
                return;
            }

            RegisterAssignedGuardians();
            HidePrompt();
            guardiansWereCleared = AreGuardiansCleared();
            TryRespawnGuardiansIfDue();
        }

        private void Update()
        {
            if (IsProximityGuardianMode())
                UpdateProximityGuardians();
            else
                UpdateGuardianRespawnTimer();

            if (isOpening || chestVisuallyOpened || ChestRegistry.IsOpened(chestId))
                return;

            if (!isPlayerNear)
                return;

            if (IsUiBlockingInteraction())
            {
                HidePrompt();
                return;
            }

            if (unlockMode == ChestUnlockMode.DefeatEnemies && !AreGuardiansCleared())
            {
                ShowLockedPrompt();
                return;
            }

            ShowOpenPrompt();

            if (Input.GetKeyDown(ShopManager.GetInteractKey()))
            {
                HidePrompt();
                TryOpenChest();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            isPlayerNear = true;

            if (unlockMode == ChestUnlockMode.DefeatEnemies && !IsProximityGuardianMode())
                TrySpawnGuards();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            isPlayerNear = false;
            HideInteractPrompt();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (guardianActivationRadius <= 0f)
                return;

            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, guardianActivationRadius);

            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, guardianActivationRadius + guardianActivationBuffer);
        }

        private void OnValidate()
        {
            TryAutoFillGuardianRespawnPrefabs();

            if (unlockMode != ChestUnlockMode.DefeatEnemies)
                return;

            bool hasAssigned = assignedGuardians != null && assignedGuardians.Length > 0;
            bool hasSpawns = guardSpawns != null && guardSpawns.Length > 0;

            if (!hasAssigned && !hasSpawns)
                Debug.LogWarning(
                    $"[{nameof(WorldChest)}] '{name}' uses DefeatEnemies but has no guardians or spawn waves.",
                    this);
        }

        /// <summary>Best-effort: fills empty respawn-prefab slots from each guardian's source prefab. Never overwrites manual entries.</summary>
        private void TryAutoFillGuardianRespawnPrefabs()
        {
            if (assignedGuardians == null || assignedGuardians.Length == 0)
                return;

            if (guardianRespawnPrefabs == null)
                guardianRespawnPrefabs = new List<GameObject>();

            while (guardianRespawnPrefabs.Count < assignedGuardians.Length)
                guardianRespawnPrefabs.Add(null);

            for (int i = 0; i < assignedGuardians.Length; i++)
            {
                if (guardianRespawnPrefabs[i] != null)
                    continue;

                EnemyBase guardian = assignedGuardians[i];
                if (guardian == null)
                    continue;

                GameObject source = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(guardian.gameObject);
                if (source != null)
                    guardianRespawnPrefabs[i] = source;
            }
        }
#endif
        #endregion

        #region Interaction
        private void ShowOpenPrompt()
        {
            if (promptRoot != null && !promptRoot.activeSelf)
                promptRoot.SetActive(true);

            if (promptContainer != null && !promptContainer.activeSelf)
                promptContainer.SetActive(true);

            SetInteractKeyVisible(true);
            SetPromptText(openPromptText);
        }

        private void ShowLockedPrompt()
        {
            if (promptRoot != null && !promptRoot.activeSelf)
                promptRoot.SetActive(true);

            if (promptContainer != null && !promptContainer.activeSelf)
                promptContainer.SetActive(true);

            SetInteractKeyVisible(false);
            SetPromptText(lockedPromptText);
        }

        private void HidePrompt()
        {
            HideInteractPrompt();

            if (promptRoot != null && promptRoot.activeSelf)
                promptRoot.SetActive(false);
        }

        private void HideInteractPrompt()
        {
            if (promptContainer != null && promptContainer.activeSelf)
                promptContainer.SetActive(false);

            SetInteractKeyVisible(false);
        }

        private void SetPromptText(string text)
        {
            if (promptTextUI == null)
                return;

            if (!promptTextUI.gameObject.activeSelf)
                promptTextUI.gameObject.SetActive(true);

            promptTextUI.text = text;
        }

        private void SetInteractKeyVisible(bool visible)
        {
            if (interactKeyPrompt == null)
                return;

            if (interactKeyPrompt.activeSelf != visible)
                interactKeyPrompt.SetActive(visible);
        }

        private void ResolvePromptRoot()
        {
            if (promptRoot != null || promptContainer == null)
                return;

            Transform parent = promptContainer.transform.parent;
            if (parent != null)
                promptRoot = parent.gameObject;
        }

        private void ResolveSharedInteractUi()
        {
            if (promptContainer == null && promptRoot != null)
            {
                foreach (Transform descendant in promptRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (descendant.name != "Interact_F")
                        continue;

                    if (descendant.Find("btn") == null)
                        continue;

                    promptContainer = descendant.gameObject;
                    break;
                }
            }

            if (interactKeyPrompt == null && promptContainer != null)
            {
                Transform btn = promptContainer.transform.Find("btn");
                if (btn != null)
                    interactKeyPrompt = btn.gameObject;
            }

            if (promptTextUI == null && promptContainer != null)
                promptTextUI = promptContainer.GetComponentInChildren<TextMeshProUGUI>(true);
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
        #endregion

        #region Guards
        private void CacheAssignedGuardianPlacements()
        {
            assignedGuardianPlacements.Clear();

            if (assignedGuardians == null)
                return;

            for (int i = 0; i < assignedGuardians.Length; i++)
            {
                EnemyBase guardian = assignedGuardians[i];
                if (guardian == null)
                    continue;

                GameObject sourcePrefab = null;
                if (guardianRespawnPrefabs != null && i < guardianRespawnPrefabs.Count)
                    sourcePrefab = guardianRespawnPrefabs[i];

                assignedGuardianPlacements.Add(new GuardianPlacement
                {
                    position = guardian.transform.position,
                    rotation = guardian.transform.rotation,
                    prefab = sourcePrefab
                });
            }
        }

        private void RegisterAssignedGuardians()
        {
            trackedGuardians.Clear();

            if (assignedGuardians == null)
                return;

            foreach (EnemyBase guardian in assignedGuardians)
            {
                if (guardian != null)
                    trackedGuardians.Add(guardian);
            }
        }

        private void TrySpawnGuards()
        {
            if (guardsSpawned || guardSpawns == null || guardSpawns.Length == 0)
                return;

            SpawnGuardsFromWaves();
        }

        private void SpawnGuardsFromWaves()
        {
            if (guardSpawns == null || guardSpawns.Length == 0)
                return;

            guardsSpawned = true;

            foreach (GuardSpawnInfo info in guardSpawns)
            {
                if (info == null || info.enemyPrefab == null || info.count <= 0)
                    continue;

                for (int i = 0; i < info.count; i++)
                {
                    Transform spawnPoint = PickSpawnPoint(info.spawnPoints);
                    Vector3 spawnPos = spawnPoint.position + Vector3.up * 1.5f;
                    GameObject enemyObj = Instantiate(info.enemyPrefab, spawnPos, spawnPoint.rotation);

                    if (enemyObj.TryGetComponent(out EnemyBase enemy))
                        trackedGuardians.Add(enemy);
                }
            }
        }

        private Transform PickSpawnPoint(Transform[] spawnPoints)
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
                return spawnPoints[Random.Range(0, spawnPoints.Length)];

            return transform;
        }

        private bool AreGuardiansCleared()
        {
            if (unlockMode != ChestUnlockMode.DefeatEnemies)
                return true;

            if (IsProximityGuardianMode())
                return proximityDefeated;

            bool hasAssigned = assignedGuardians != null && assignedGuardians.Length > 0;
            bool hasSpawnWave = guardSpawns != null && guardSpawns.Length > 0;

            if (!hasAssigned && !hasSpawnWave)
                return true;

            if (hasSpawnWave && !guardsSpawned)
                return false;

            trackedGuardians.RemoveAll(enemy => enemy == null || enemy.IsDead);
            return trackedGuardians.Count == 0;
        }

        private bool HasGuardianSetup()
        {
            bool hasAssigned = assignedGuardianPlacements.Count > 0;
            bool hasSpawnWave = guardSpawns != null && guardSpawns.Length > 0;
            return hasAssigned || hasSpawnWave;
        }

        #region Proximity Guardians
        private bool IsProximityGuardianMode()
        {
            return unlockMode == ChestUnlockMode.DefeatEnemies && guardianActivationRadius > 0f;
        }

        /// <summary>Sets up proximity mode: removes always-present scene guardians and restores defeated cooldown from save.</summary>
        private void InitProximityGuardians()
        {
            if (assignedGuardians != null)
            {
                foreach (EnemyBase guardian in assignedGuardians)
                {
                    if (guardian != null)
                        Destroy(guardian.gameObject);
                }
            }

            trackedGuardians.Clear();
            guardsSpawned = false;
            proximityGuardiansActive = false;
            proximityDefeated = false;

            if (ChestGuardianRespawnRegistry.TryGetDefeatedTime(chestId, out double defeatedAtUtc))
            {
                double elapsed = ChestGuardianRespawnRegistry.GetUtcNow() - defeatedAtUtc;
                if (guardianRespawnIntervalSeconds <= 0f || elapsed < guardianRespawnIntervalSeconds)
                    proximityDefeated = true;
                else
                    ChestGuardianRespawnRegistry.ClearDefeatedTime(chestId);
            }
        }

        private void UpdateProximityGuardians()
        {
            if (!HasGuardianSetup())
                return;

            if (proximityDefeated)
            {
                TryClearDefeatCooldown();
                return;
            }

            if (!EnsurePlayerTransform())
                return;

            float distance = Vector3.Distance(playerTransform.position, transform.position);

            if (distance <= guardianActivationRadius)
            {
                if (!proximityGuardiansActive)
                {
                    SpawnProximityGuardians();
                    return;
                }

                trackedGuardians.RemoveAll(enemy => enemy == null || enemy.IsDead);
                if (trackedGuardians.Count == 0)
                    MarkProximityDefeated();
            }
            else if (distance > guardianActivationRadius + guardianActivationBuffer && proximityGuardiansActive)
            {
                DespawnProximityGuardians();
            }
        }

        private void TryClearDefeatCooldown()
        {
            if (guardianRespawnIntervalSeconds <= 0f)
                return;

            if (!ChestGuardianRespawnRegistry.TryGetDefeatedTime(chestId, out double defeatedAtUtc))
            {
                proximityDefeated = false;
                return;
            }

            double elapsed = ChestGuardianRespawnRegistry.GetUtcNow() - defeatedAtUtc;
            if (elapsed >= guardianRespawnIntervalSeconds)
            {
                proximityDefeated = false;
                ChestGuardianRespawnRegistry.ClearDefeatedTime(chestId);
            }
        }

        private void SpawnProximityGuardians()
        {
            trackedGuardians.Clear();
            guardsSpawned = false;

            if (assignedGuardianPlacements.Count > 0)
                RespawnAssignedGuardians();

            if (guardSpawns != null && guardSpawns.Length > 0)
                SpawnGuardsFromWaves();

            proximityGuardiansActive = trackedGuardians.Count > 0;
        }

        private void DespawnProximityGuardians()
        {
            foreach (EnemyBase enemy in trackedGuardians)
            {
                if (enemy != null)
                    Destroy(enemy.gameObject);
            }

            trackedGuardians.Clear();
            guardsSpawned = false;
            proximityGuardiansActive = false;
        }

        private void MarkProximityDefeated()
        {
            proximityDefeated = true;
            proximityGuardiansActive = false;
            ChestGuardianRespawnRegistry.MarkAllDefeated(chestId);

            if (PauseMenuManager.Instance != null)
                PauseMenuManager.Instance.SaveGameSilently();
        }

        private bool EnsurePlayerTransform()
        {
            if (playerTransform != null)
                return true;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;

            return playerTransform != null;
        }
        #endregion

        private void UpdateGuardianRespawnTimer()
        {
            if (unlockMode != ChestUnlockMode.DefeatEnemies || !HasGuardianSetup())
                return;

            if (guardianRespawnIntervalSeconds <= 0f)
                return;

            bool guardiansCleared = AreGuardiansCleared();

            if (guardiansCleared && !guardiansWereCleared)
            {
                ChestGuardianRespawnRegistry.MarkAllDefeated(chestId);

                if (PauseMenuManager.Instance != null)
                    PauseMenuManager.Instance.SaveGameSilently();
            }

            guardiansWereCleared = guardiansCleared;

            if (!guardiansCleared)
                return;

            if (!ChestGuardianRespawnRegistry.TryGetDefeatedTime(chestId, out double defeatedAtUtc))
                return;

            double elapsed = ChestGuardianRespawnRegistry.GetUtcNow() - defeatedAtUtc;
            if (elapsed < guardianRespawnIntervalSeconds)
                return;

            RespawnAllGuardians();
        }

        private void TryRespawnGuardiansIfDue()
        {
            if (unlockMode != ChestUnlockMode.DefeatEnemies || !HasGuardianSetup())
                return;

            if (guardianRespawnIntervalSeconds <= 0f)
                return;

            if (!ChestGuardianRespawnRegistry.TryGetDefeatedTime(chestId, out double defeatedAtUtc))
                return;

            double elapsed = ChestGuardianRespawnRegistry.GetUtcNow() - defeatedAtUtc;
            if (elapsed < guardianRespawnIntervalSeconds)
                return;

            RespawnAllGuardians();
        }

        private void RespawnAllGuardians()
        {
            trackedGuardians.Clear();
            guardsSpawned = false;
            guardiansWereCleared = false;
            ChestGuardianRespawnRegistry.ClearDefeatedTime(chestId);

            if (assignedGuardianPlacements.Count > 0)
                RespawnAssignedGuardians();

            if (guardSpawns != null && guardSpawns.Length > 0)
                SpawnGuardsFromWaves();

            if (PauseMenuManager.Instance != null)
                PauseMenuManager.Instance.SaveGameSilently();
        }

        private void RespawnAssignedGuardians()
        {
            GameObject fallbackPrefab = ResolveAssignedGuardianPrefab();

            foreach (GuardianPlacement placement in assignedGuardianPlacements)
            {
                GameObject prefab = placement.prefab != null ? placement.prefab : fallbackPrefab;
                if (prefab == null)
                    continue;

                Vector3 spawnPos = placement.position + Vector3.up * 1.5f;
                GameObject enemyObj = Instantiate(prefab, spawnPos, placement.rotation);

                if (enemyObj.TryGetComponent(out EnemyBase enemy))
                    trackedGuardians.Add(enemy);
            }
        }

        private GameObject ResolveAssignedGuardianPrefab()
        {
            if (assignedGuardianRespawnPrefab != null)
                return assignedGuardianRespawnPrefab;

            if (guardSpawns == null)
                return null;

            foreach (GuardSpawnInfo info in guardSpawns)
            {
                if (info != null && info.enemyPrefab != null)
                    return info.enemyPrefab;
            }

            return null;
        }
        #endregion

        #region Open Flow
        private void TryOpenChest()
        {
            if (isOpening || ChestRegistry.IsOpened(chestId))
                return;

            if (unlockMode == ChestUnlockMode.DefeatEnemies && !AreGuardiansCleared())
                return;

            isOpening = true;
            DisableInteractionColliders();
            GrantLoot();
            PlayOpenSound();
            ChestRegistry.MarkOpened(chestId);

            if (PauseMenuManager.Instance != null)
                PauseMenuManager.Instance.SaveGameSilently();

            StartCoroutine(OpenChestSequence());
        }

        private IEnumerator OpenChestSequence()
        {
            yield return PlayLidOpenAnimation();
            yield return FadeOutRoutine();
        }

        private IEnumerator PlayLidOpenAnimation()
        {
            ResolveLidTransform();

            if (lidTransform == null || lidOpenDuration <= 0f)
                yield break;

            Quaternion startRotation = lidTransform.localRotation;
            Quaternion endRotation = startRotation * Quaternion.Euler(lidOpenAngle, 0f, 0f);
            float elapsed = 0f;

            while (elapsed < lidOpenDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / lidOpenDuration);
                lidTransform.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
                yield return null;
            }

            lidTransform.localRotation = endRotation;
        }

        private void ResolveLidTransform()
        {
            if (lidTransform != null)
                return;

            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child == transform)
                    continue;

                string name = child.name.ToLowerInvariant();
                if (name.Contains("cover") || name.Contains("roof") || name.Contains("lid"))
                {
                    lidTransform = child;
                    return;
                }
            }
        }

        private void GrantLoot()
        {
            if (lootTable == null)
                return;

            lootTable.GrantToPlayer();
        }

        private void PlayOpenSound()
        {
            if (openSound == null || openSoundVolume <= 0f)
                return;

            EnsureAudioSource();
            if (audioSource != null)
                audioSource.PlayOneShot(openSound, openSoundVolume);
        }

        private void DisableInteractionColliders()
        {
            if (interactionColliders == null)
                return;

            foreach (Collider col in interactionColliders)
            {
                if (col != null)
                    col.enabled = false;
            }
        }

        private IEnumerator FadeOutRoutine()
        {
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
                ApplyFadeAlpha(alpha);
                yield return null;
            }

            ApplyFadeAlpha(0f);
            HideChestVisuals();
            chestVisuallyOpened = true;
        }

        private void ApplyOpenedVisualState()
        {
            DisableInteractionColliders();
            ApplyFadeAlpha(0f);
            HideChestVisuals();
        }

        /// <summary>Hides the chest meshes after opening while keeping this GameObject active for guardian respawns.</summary>
        private void HideChestVisuals()
        {
            if (cachedFadeRenderers == null)
                return;

            foreach (Renderer renderer in cachedFadeRenderers)
            {
                if (renderer != null)
                    renderer.enabled = false;
            }
        }
        #endregion

        #region Fade Helpers
        private void CacheFadeRenderers()
        {
            cachedFadeRenderers = GetComponentsInChildren<Renderer>(true);
        }

        private void CacheBaseColors()
        {
            baseColors.Clear();

            if (cachedFadeRenderers == null)
                return;

            foreach (Renderer renderer in cachedFadeRenderers)
            {
                if (renderer == null)
                    continue;

                baseColors[renderer] = ReadRendererColor(renderer);
            }
        }

        private static Color ReadRendererColor(Renderer renderer)
        {
            Material mat = renderer.sharedMaterial;
            if (mat == null)
                return Color.white;

            if (mat.HasProperty(BaseColorId))
                return mat.GetColor(BaseColorId);

            if (mat.HasProperty(ColorId))
                return mat.GetColor(ColorId);

            return Color.white;
        }

        private void ApplyFadeAlpha(float alpha)
        {
            if (cachedFadeRenderers == null)
                return;

            fadePropertyBlock ??= new MaterialPropertyBlock();

            foreach (Renderer renderer in cachedFadeRenderers)
            {
                if (renderer == null || !baseColors.TryGetValue(renderer, out Color baseColor))
                    continue;

                Color faded = baseColor;
                faded.a = baseColor.a * alpha;

                renderer.GetPropertyBlock(fadePropertyBlock);
                if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(BaseColorId))
                    fadePropertyBlock.SetColor(BaseColorId, faded);
                else
                    fadePropertyBlock.SetColor(ColorId, faded);

                renderer.SetPropertyBlock(fadePropertyBlock);
            }
        }

        private void EnsureAudioSource()
        {
            if (audioSource != null)
                return;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
        #endregion
    }
}
