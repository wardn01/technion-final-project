using UnityEngine;
using VisionOfLight.Enemy;

/// <summary>
/// Chapter 3 bootstrap. Wires Gareth and the ridge wave stone.
/// </summary>
[DisallowMultipleComponent]
public class Quest03ChapterManager : QuestChapterManager
{
    public override int ChapterStateId => 2;

    [Header("Chapter 03 References")]
    public ChallengeStone waveStone;

    public override void ResolveReferences()
    {
        if (waveStone != null)
            return;

        ChallengeStone[] stones = FindObjectsByType<ChallengeStone>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (ChallengeStone stone in stones)
        {
            if (stone == null || stone.questChallenges == null || stone.questChallenges.Length == 0)
                continue;

            foreach (ChallengeStone.QuestChallenge challenge in stone.questChallenges)
            {
                if (challenge != null && challenge.targetQuestState == ChapterStateId)
                {
                    waveStone = stone;
                    return;
                }
            }
        }

        GameObject waveObject = GameObject.Find("WaveQuest");
        if (waveObject != null)
            waveStone = waveObject.GetComponent<ChallengeStone>();
    }
}
