using UnityEngine;

/// <summary>
/// ScriptableObject stats for <see cref="Orc"/> — boss tier above Bear.
/// Two-phase fight: normal melee, then enrage with faster attacks and heavy fire.
/// Tune in <c>Orc/Data/OrcData.asset</c>.
/// </summary>
[CreateAssetMenu(fileName = "OrcData", menuName = "Game Data/Enemy/Boss/Orc Stats")]
public class OrcStats : BossEnemyStats
{
}
