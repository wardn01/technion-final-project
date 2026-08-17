using UnityEngine;

/// <summary>
/// Place a trigger collider over a house interior. While the player is inside,
/// <see cref="QuestPathRenderer"/> stays hidden (same idea as blocking the path indoors).
/// </summary>
[RequireComponent(typeof(Collider))]
public class QuestPathSuppressionZone : MonoBehaviour
{
    #region Unity Lifecycle
    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }
    #endregion

    #region Trigger Collision
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            QuestPathSuppression.EnterZone();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            QuestPathSuppression.ExitZone();
    }
    #endregion
}
