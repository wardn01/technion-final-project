using UnityEngine;

/// <summary>
/// Place a trigger collider over a house interior. While the player is inside,
/// <see cref="QuestPathRenderer"/> stays hidden (same idea as blocking the path indoors).
/// </summary>
[RequireComponent(typeof(Collider))]
public class QuestPathSuppressionZone : MonoBehaviour
{
    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }

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
}
