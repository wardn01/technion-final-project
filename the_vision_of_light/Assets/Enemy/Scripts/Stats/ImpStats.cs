using UnityEngine;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// ScriptableObject stats for <see cref="Imp"/> — weak normal mob (Skeleton-tier).
    /// Tune balance in <c>Imp/Data/ImpData.asset</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "ImpData", menuName = "Game Data/Enemy/Normal/Imp Stats")]
    public class ImpStats : NormalEnemyStats
    {
    }
}
