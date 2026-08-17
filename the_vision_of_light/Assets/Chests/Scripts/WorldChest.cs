using System.Collections.Generic;
using UnityEngine;
using TMPro;
using VisionOfLight.Enemy;

namespace VisionOfLight.Chest
{
    /// <summary>
    /// One-time world loot chest. Supports immediate open or defeat-guardians-first unlock.
    /// Guardians respawn on a real-world timer even after the chest was opened. The chest itself never returns.
    /// Split across partial files: Prompt, Guardians, OpenAndFade.
    /// </summary>
    public partial class WorldChest : MonoBehaviour
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

        /// <summary>Fired every time all guardians die (first clear and each hourly clear).</summary>
        public event System.Action GuardiansDefeated;
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

            RefreshPlayerNearByDistance();

            if (!isPlayerNear)
                return;

            if (IsUiBlockingInteraction())
            {
                HidePrompt();
                return;
            }

            if (unlockMode == ChestUnlockMode.DefeatEnemies && !AreGuardiansCleared())
            {
                ShowOpenPrompt();

                if (Input.GetKeyDown(ShopManager.GetInteractKey()))
                {
                    if (NotificationManager.Instance != null)
                        NotificationManager.Instance.ShowWarning(lockedPromptText);
                }

                return;
            }

            ShowOpenPrompt();

            if (Input.GetKeyDown(ShopManager.GetInteractKey()))
            {
                HidePrompt();
                TryOpenChest();
            }
        }

        /// <summary>Call when the player warps away without OnTriggerExit (map teleport).</summary>
        public void ClearPlayerProximity()
        {
            if (!isPlayerNear)
            {
                HidePrompt();
                return;
            }

            isPlayerNear = false;
            HidePrompt();
        }

        private void RefreshPlayerNearByDistance()
        {
            if (!isPlayerNear)
                return;

            if (!EnsurePlayerTransform())
                return;

            if (SharedInteractPromptUtility.IsPlayerBeyondRange(
                    transform.position, playerTransform, GetInteractLeaveDistance()))
                ClearPlayerProximity();
        }

        private float GetInteractLeaveDistance()
        {
            float maxExtent = SharedInteractPromptUtility.DefaultLeaveDistance;

            if (interactionColliders == null)
                return maxExtent;

            foreach (Collider col in interactionColliders)
            {
                if (col == null || !col.enabled)
                    continue;

                maxExtent = Mathf.Max(maxExtent, col.bounds.extents.magnitude + 1.5f);
            }

            return maxExtent;
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

            ClearPlayerProximity();
        }
        #endregion
    }
}
