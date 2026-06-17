using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using VisionOfLight.Enemy;

namespace VisionOfLight.Chest
{
    /// <summary>
    /// One-time world loot chest. Supports immediate open or defeat-guardians-first unlock.
    /// Fades out after opening and persists via <see cref="ChestRegistry"/>.
    /// Wire UI on the scene instance, or leave empty — <see cref="ResolveSharedInteractUi"/> finds Canvas/InteractPrompt at runtime.
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

        public enum ChestVisualType
        {
            Wood,
            Stone,
            Gold
        }

        #region Data Types
        [System.Serializable]
        public class ChestLootEntry
        {
            public ItemData item;
            public int amount = 1;
        }

        [System.Serializable]
        public class GuardSpawnInfo
        {
            public GameObject enemyPrefab;
            public int count = 1;
            public Transform[] spawnPoints;
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

        [Header("Loot")]
        public ChestLootEntry[] lootContents;

        [Header("UI")]
        [Tooltip("Optional. Left empty on prefabs — auto-finds InteractPrompt / Interact_F in the loaded scene.")]
        public GameObject promptContainer;
        public GameObject promptRoot;
        public GameObject interactKeyPrompt;

        [Tooltip("Shared label such as InteractTeleport.")]
        public TextMeshProUGUI promptTextUI;

        public string openPromptText = "Open Chest";
        public string lockedPromptText = "Defeat enemies";

        [Header("Open Feedback")]
        public AudioClip openSound;
        [Range(0f, 1f)] public float openSoundVolume = 1f;

        [Header("Lid Open")]
        [Tooltip("Optional. Auto-finds chest cover / roof child when empty.")]
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
            if (ChestRegistry.IsOpened(chestId))
            {
                gameObject.SetActive(false);
                return;
            }

            ResolveSharedInteractUi();
            ResolvePromptRoot();
            RegisterAssignedGuardians();
            HidePrompt();
        }

        private void Update()
        {
            if (isOpening || ChestRegistry.IsOpened(chestId))
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

            if (unlockMode == ChestUnlockMode.DefeatEnemies)
                TrySpawnGuards();
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

        /// <summary>
        /// Prefab assets cannot reference scene UI. Finds the shared HUD Interact prompt when fields are empty.
        /// </summary>
        private void ResolveSharedInteractUi()
        {
            if (promptRoot == null)
            {
                GameObject interactPrompt = FindSceneObjectByName("InteractPrompt");
                if (interactPrompt != null)
                    promptRoot = interactPrompt;
            }

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

            if (promptContainer == null)
            {
                GameObject interactF = FindSceneObjectByName("Interact_F");
                if (interactF != null && interactF.transform.Find("btn") != null)
                    promptContainer = interactF;
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

        private static GameObject FindSceneObjectByName(string objectName)
        {
            Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

            foreach (Transform transform in transforms)
            {
                if (transform.name != objectName)
                    continue;

                if (!transform.gameObject.scene.IsValid())
                    continue;

                return transform.gameObject;
            }

            return null;
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

            bool hasAssigned = assignedGuardians != null && assignedGuardians.Length > 0;
            bool hasSpawnWave = guardSpawns != null && guardSpawns.Length > 0;

            if (!hasAssigned && !hasSpawnWave)
                return true;

            if (hasSpawnWave && !guardsSpawned)
                return false;

            trackedGuardians.RemoveAll(enemy => enemy == null || enemy.IsDead);
            return trackedGuardians.Count == 0;
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
            if (lootContents == null || InventoryManager.Instance == null)
                return;

            foreach (ChestLootEntry entry in lootContents)
            {
                if (entry == null || entry.item == null || entry.amount <= 0)
                    continue;

                InventoryManager.Instance.AddItem(entry.item, entry.amount);
            }
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
            gameObject.SetActive(false);
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
