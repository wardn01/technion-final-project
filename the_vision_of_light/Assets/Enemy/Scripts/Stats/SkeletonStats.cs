using UnityEngine;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// ScriptableObject stats for <see cref="Skeleton"/> — weakest normal mob (Hilichurl-tier).
    /// Tune balance in <c>Skeleton/Data/SkeletonData.asset</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "SkeletonData", menuName = "Game Data/Enemy/Normal/Skeleton Stats")]
    public class SkeletonStats : NormalEnemyStats
    {
    }
}
