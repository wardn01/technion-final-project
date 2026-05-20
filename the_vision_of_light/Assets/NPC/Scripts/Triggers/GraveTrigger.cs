using UnityEngine;

public class GraveTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && QuestManager.Instance.mainQuestState == 1)
        {
            QuestManager.Instance.mainQuestState = 2;
        }
    }
}