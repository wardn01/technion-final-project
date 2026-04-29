using UnityEngine;

public class EnemyVFX : MonoBehaviour
{
    [Header("Rage Phase VFX")]
    [SerializeField] private GameObject rageVFXPrefab;
    [SerializeField] private float rageVFXLifetime = 4f;

    [Header("Heavy Attack VFX")]
    [SerializeField] private GameObject heavyAttackVFXPrefab;
    [SerializeField] private float heavyAttackVFXLifetime = 3f;

    public void PlayRageVFX()
    {
        if (rageVFXPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 1f; 
            
            GameObject vfxInstance = Instantiate(rageVFXPrefab, spawnPos, Quaternion.identity);
            
            vfxInstance.transform.SetParent(this.transform);
            
            Destroy(vfxInstance, rageVFXLifetime);
        }
    }

    public void PlayHeavyAttackVFX(Vector3 targetPosition)
    {
        if (heavyAttackVFXPrefab != null)
        {

            GameObject vfxInstance = Instantiate(heavyAttackVFXPrefab, targetPosition, Quaternion.identity);

            Destroy(vfxInstance, heavyAttackVFXLifetime);
        }
    }
}