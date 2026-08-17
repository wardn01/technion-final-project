using UnityEngine;

namespace VisionOfLight.Enemy
{
    /// <summary>MiniGolem summon at low HP for <see cref="Golem"/>.</summary>
    public partial class Golem
    {
        #region Mini Golem Summon

        private void MarkFightStartedIfNeeded()
        {
            if (fightStartTime >= 0f || isDead || isReturningToCamp || target == null || stats == null)
                return;

            if (Vector3.Distance(transform.position, target.position) <= stats.ChaseRange)
                MarkFightStarted();
        }

        private void MarkFightStarted()
        {
            if (fightStartTime >= 0f)
                return;

            fightStartTime = Time.time;
        }

        private void TrySummonMiniGolems(float projectedHealth = -1f)
        {
            if (miniGolemsSummoned || isDead || isReturningToCamp || miniGolemPrefab == null || fightStartTime < 0f)
                return;

            float healthToCheck = projectedHealth >= 0f ? projectedHealth : currentHealth;
            if (currentMaxHealth <= 0f || healthToCheck > currentMaxHealth * miniGolemSummonHealthPercent)
                return;

            SpawnMiniGolems();
        }

        private void SpawnMiniGolems()
        {
            miniGolemsSummoned = true;
            BossHealthBarUI.Instance?.HideGolemSummonMeter();

            if (miniGolemSpawnPoints != null && miniGolemSpawnPoints.Length > 0)
            {
                int spawnCount = Mathf.Min(miniGolemCount, miniGolemSpawnPoints.Length);
                for (int i = 0; i < spawnCount; i++)
                {
                    Transform spawnPoint = miniGolemSpawnPoints[i];
                    if (spawnPoint == null)
                        continue;

                    SpawnOneMiniGolem(spawnPoint.position, spawnPoint.rotation);
                }

                return;
            }

            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            Vector3 basePosition = transform.position;

            SpawnOneMiniGolem(basePosition + right * 3.5f - forward * 1.5f, transform.rotation);
            if (miniGolemCount > 1)
                SpawnOneMiniGolem(basePosition - right * 3.5f - forward * 1.5f, transform.rotation);
        }

        private void SpawnOneMiniGolem(Vector3 position, Quaternion rotation)
        {
            GameObject miniGolemObject = Instantiate(miniGolemPrefab, position, rotation);
            activeMiniGolems.Add(miniGolemObject);

            if (miniGolemObject.TryGetComponent(out MiniGolem miniGolem))
                miniGolem.InitializeAsSummon(this);
        }

        private void ClearSummonedMiniGolems()
        {
            for (int i = activeMiniGolems.Count - 1; i >= 0; i--)
            {
                if (activeMiniGolems[i] != null)
                    Destroy(activeMiniGolems[i]);
            }

            activeMiniGolems.Clear();
        }

        private bool HasLivingMiniGolems()
        {
            if (!miniGolemsSummoned)
                return false;

            for (int i = activeMiniGolems.Count - 1; i >= 0; i--)
            {
                GameObject miniGolemObject = activeMiniGolems[i];
                if (miniGolemObject == null)
                {
                    activeMiniGolems.RemoveAt(i);
                    continue;
                }

                if (miniGolemObject.TryGetComponent(out MiniGolem miniGolem) && miniGolem.IsDead)
                {
                    activeMiniGolems.RemoveAt(i);
                    continue;
                }

                return true;
            }

            return false;
        }

        #endregion
    }
}
