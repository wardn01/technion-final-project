using UnityEngine;
using VisionOfLight.Chest;
using VisionOfLight.Enemy;

/// <summary>
/// Quest-gated world boss encounter (Orc, Golem, …).
/// Hides the boss + gold chest until the quest reaches a step (or testing unlock).
/// Every boss defeat shows the same center result UI as wave challenges (not quest-only).
/// First kill while on the unlock step can advance the quest; chest uses hourly guardian respawn.
/// </summary>
public class QuestBossEncounter : MonoBehaviour
{
    [Header("Quest Gate")]
    [Tooltip("OFF = always available (use while placing / testing before the chapter exists).")]
    public bool gateWithQuest = false;

    [Tooltip("Chapter stateId that unlocks this boss. Fill when the chapter asset exists.")]
    public int requiredState = 3;

    [Tooltip("Objective index inside that chapter that reveals the boss.")]
    public int requiredStep = 0;

    [Tooltip("When the boss is first defeated during the unlock step, call QuestManager.AdvanceStep.")]
    public bool advanceQuestOnFirstKill = true;

    [Header("Encounter")]
    [Tooltip("Parent that holds the boss + gold chest. Toggled on unlock. Leave empty to toggle Boss + Chest refs.")]
    public GameObject encounterContent;

    [Tooltip("Scene Orc / boss instance (assigned guardian on the chest).")]
    public EnemyBase boss;

    [Tooltip("Gold chest next to the boss (DefeatEnemies + 1h respawn).")]
    public WorldChest rewardChest;

    [Header("Defeat UI (same as Wave)")]
    [Tooltip("Uses ChallengeTimerUI — same panel as Challenge Complete. Shows on EVERY kill, not only the quest.")]
    public bool showDefeatResultUi = true;

    [TextArea(1, 2)]
    public string defeatMessage = "Boss Defeated";

    private bool isEncounterActive;
    private bool firstKillQuestHandled;
    private WorldChest subscribedChest;

    private void Awake()
    {
        AutoWireIfNeeded();
        SetEncounterActive(false);
    }

    private void Start()
    {
        RefreshEncounterAvailability();
        SubscribeToChest();
    }

    private void OnEnable()
    {
        SubscribeToChest();
    }

    private void OnDisable()
    {
        UnsubscribeFromChest();
    }

    private void OnDestroy()
    {
        UnsubscribeFromChest();
    }

    private void Update()
    {
        RefreshEncounterAvailability();
        SubscribeToChest();
    }

    private void AutoWireIfNeeded()
    {
        if (encounterContent == null && transform.childCount > 0)
            encounterContent = transform.GetChild(0).gameObject;

        if (boss == null)
            boss = GetComponentInChildren<EnemyBase>(true);

        if (rewardChest == null)
            rewardChest = GetComponentInChildren<WorldChest>(true);
    }

    private void RefreshEncounterAvailability()
    {
        bool shouldShow = ShouldEncounterBeAvailable();
        if (shouldShow == isEncounterActive)
            return;

        SetEncounterActive(shouldShow);

        if (shouldShow)
            SubscribeToChest();
    }

    private bool ShouldEncounterBeAvailable()
    {
        if (!gateWithQuest)
            return true;

        if (QuestManager.Instance == null)
            return false;

        int state = QuestManager.Instance.mainQuestState;
        int step = QuestManager.Instance.questStepIndex;

        if (state > requiredState)
            return true;

        if (state == requiredState && step >= requiredStep)
            return true;

        if (rewardChest != null &&
            ChestGuardianRespawnRegistry.TryGetDefeatedTime(rewardChest.chestId, out _))
            return true;

        if (rewardChest != null && ChestRegistry.IsOpened(rewardChest.chestId))
            return true;

        return false;
    }

    private void SetEncounterActive(bool active)
    {
        isEncounterActive = active;

        bool contentOwnsBoss = encounterContent != null && boss != null &&
                               boss.transform.IsChildOf(encounterContent.transform);
        bool contentOwnsChest = encounterContent != null && rewardChest != null &&
                                rewardChest.transform.IsChildOf(encounterContent.transform);

        // Preferred: one content root holding boss + chest.
        if (encounterContent != null && (contentOwnsBoss || contentOwnsChest))
        {
            if (encounterContent.activeSelf != active)
                encounterContent.SetActive(active);
        }

        // Also supports boss/chest as siblings of EncounterContent (current Orc setup).
        if (boss != null && !contentOwnsBoss && boss.gameObject.activeSelf != active)
            boss.gameObject.SetActive(active);

        if (rewardChest != null && !contentOwnsChest && rewardChest.gameObject.activeSelf != active)
            rewardChest.gameObject.SetActive(active);
    }

    private void SubscribeToChest()
    {
        if (rewardChest == null)
            rewardChest = GetComponentInChildren<WorldChest>(true);

        if (rewardChest == null || rewardChest == subscribedChest)
            return;

        UnsubscribeFromChest();
        subscribedChest = rewardChest;
        subscribedChest.GuardiansDefeated += OnGuardiansDefeated;
    }

    private void UnsubscribeFromChest()
    {
        if (subscribedChest == null)
            return;

        subscribedChest.GuardiansDefeated -= OnGuardiansDefeated;
        subscribedChest = null;
    }

    private void OnGuardiansDefeated()
    {
        // Always show — first kill, quest kill, and every hourly respawn kill.
        if (showDefeatResultUi && ChallengeTimerUI.Instance != null)
        {
            string message = string.IsNullOrWhiteSpace(defeatMessage) ? "Boss Defeated" : defeatMessage;
            ChallengeTimerUI.Instance.ShowResult(message, success: true);
        }

        TryAdvanceQuestOnFirstKill();
    }

    private void TryAdvanceQuestOnFirstKill()
    {
        if (firstKillQuestHandled || !advanceQuestOnFirstKill)
            return;

        if (QuestManager.Instance == null)
            return;

        if (!QuestManager.Instance.IsAtQuestStep(requiredState, requiredStep))
            return;

        firstKillQuestHandled = true;
        QuestManager.Instance.AdvanceStep(requiredState, requiredStep);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoWireIfNeeded();
    }
#endif
}
