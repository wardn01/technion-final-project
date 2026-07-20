using System.Collections.Generic;
using UnityEngine;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// Spawns a group of enemies when the player enters the activation radius and despawns them
    /// when the player leaves. Fresh spawns run <see cref="EnemyBase"/> Start(), so enemies always
    /// scale to the player's current level. Place on an empty GameObject (an enemy "camp").
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyProximitySpawner : MonoBehaviour
    {
        [System.Serializable]
        public class SpawnEntry
        {
            public GameObject enemyPrefab;
            public int count = 1;

            [Tooltip("Optional spawn points. If empty, enemies spawn around this object.")]
            public Transform[] spawnPoints;
        }

        [Header("Enemies")]
        [Tooltip("Enemy prefabs and how many of each to spawn.")]
        public SpawnEntry[] enemies;

        [Header("Activation")]
        [Tooltip("Player must be within this distance for enemies to spawn.")]
        public float activationRadius = 30f;

        [Tooltip("Extra distance beyond the radius before enemies despawn (prevents flicker at the edge).")]
        public float deactivationBuffer = 5f;

        [Tooltip("Seconds between distance checks. Higher = cheaper.")]
        public float checkInterval = 0.3f;

        [Header("Respawn")]
        [Tooltip("If true, enemies respawn only after the player leaves and returns. If false, respawn after a cooldown while the player stays.")]
        public bool respawnOnlyAfterLeaving = true;

        [Tooltip("When respawnOnlyAfterLeaving is false: seconds after all enemies die before respawning while the player stays.")]
        public float respawnCooldown = 60f;

        [Header("Spawn Placement")]
        [Tooltip("Random horizontal spread when no spawn points are assigned.")]
        public float scatterRadius = 4f;

        [Tooltip("Vertical offset so enemies drop onto the ground/NavMesh.")]
        public float spawnHeightOffset = 1.5f;

        private readonly List<EnemyBase> spawnedEnemies = new List<EnemyBase>();
        private Transform playerTransform;
        private bool isSpawned;
        private float nextCheckTime;
        private float clearedTime = -1f;

        private void Update()
        {
            if (Time.time < nextCheckTime)
                return;

            nextCheckTime = Time.time + Mathf.Max(0.05f, checkInterval);

            if (!EnsurePlayer())
                return;

            float distance = Vector3.Distance(playerTransform.position, transform.position);

            if (!isSpawned)
            {
                if (distance <= activationRadius && CanSpawnNow())
                    SpawnAll();

                return;
            }

            if (distance > activationRadius + deactivationBuffer)
            {
                DespawnAll();
                return;
            }

            if (!respawnOnlyAfterLeaving)
                HandleInPlaceRespawn();
        }

        private bool CanSpawnNow()
        {
            if (respawnOnlyAfterLeaving)
                return true;

            if (clearedTime < 0f)
                return true;

            return Time.time - clearedTime >= respawnCooldown;
        }

        private void HandleInPlaceRespawn()
        {
            spawnedEnemies.RemoveAll(e => e == null || e.IsDead);

            if (spawnedEnemies.Count > 0)
            {
                clearedTime = -1f;
                return;
            }

            if (clearedTime < 0f)
                clearedTime = Time.time;
            else if (Time.time - clearedTime >= respawnCooldown)
                SpawnAll();
        }

        private bool EnsurePlayer()
        {
            if (playerTransform != null)
                return true;

            playerTransform = SharedInteractPromptUtility.GetPlayerTransform();
            return playerTransform != null;
        }

        private void SpawnAll()
        {
            DespawnAll();

            if (enemies == null)
                return;

            foreach (SpawnEntry entry in enemies)
            {
                if (entry == null || entry.enemyPrefab == null || entry.count <= 0)
                    continue;

                for (int i = 0; i < entry.count; i++)
                {
                    Vector3 position = ResolveSpawnPosition(entry.spawnPoints);
                    Quaternion rotation = ResolveSpawnRotation(entry.spawnPoints);
                    GameObject enemyObj = Instantiate(entry.enemyPrefab, position, rotation);

                    if (enemyObj.TryGetComponent(out EnemyBase enemy))
                        spawnedEnemies.Add(enemy);
                }
            }

            isSpawned = true;
            clearedTime = -1f;
        }

        private void DespawnAll()
        {
            foreach (EnemyBase enemy in spawnedEnemies)
            {
                if (enemy != null)
                    Destroy(enemy.gameObject);
            }

            spawnedEnemies.Clear();
            isSpawned = false;
            clearedTime = -1f;
        }

        private Vector3 ResolveSpawnPosition(Transform[] spawnPoints)
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
                if (point != null)
                    return point.position + Vector3.up * spawnHeightOffset;
            }

            Vector2 circle = Random.insideUnitCircle * scatterRadius;
            return transform.position + new Vector3(circle.x, spawnHeightOffset, circle.y);
        }

        private Quaternion ResolveSpawnRotation(Transform[] spawnPoints)
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
                if (point != null)
                    return point.rotation;
            }

            return transform.rotation;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, activationRadius);

            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, activationRadius + deactivationBuffer);
        }
#endif
    }
}
