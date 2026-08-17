using System.Collections.Generic;
using UnityEngine;
using VisionOfLight.Enemy;

namespace VisionOfLight.Chest
{
    /// <summary>Guardian spawn, proximity, and hourly respawn for <see cref="WorldChest"/>.</summary>
    public partial class WorldChest
    {
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
        #endregion

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
            NotifyGuardiansDefeated();

            if (PauseMenuManager.Instance != null)
                PauseMenuManager.Instance.SaveGameSilently();
        }

        private void NotifyGuardiansDefeated()
        {
            GuardiansDefeated?.Invoke();
        }

        private bool EnsurePlayerTransform()
        {
            if (playerTransform != null)
                return true;

            playerTransform = SharedInteractPromptUtility.GetPlayerTransform();
            return playerTransform != null;
        }
        #endregion

        #region Guardian Respawn
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
                NotifyGuardiansDefeated();

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
    }
}
