using UnityEngine;
using TMPro;

/// <summary>
/// Quest 01 Villager beside Xiao's bed. Step 0 = awakening talk. Step 2 = return for Wind Sword.
/// </summary>
[RequireComponent(typeof(Collider))]
public class VillagerQuestNPC : MonoBehaviour
{
    private const int AwakeningStep = 0;
    private const int ReturnStep = 2;

    [Header("Identity")]
    public string npcDisplayName = "Albedo";
    public string interactPromptName = "Albedo";

    [Header("Quest 01 Dialogue")]
    [Tooltip("Step 0 — first talk after waking up.")]
    public DialogueData awakeningDialogue;

    [Tooltip("Step 2 — return after visiting the graves.")]
    public DialogueData returnDialogue;

    [Header("Quest Routing")]
    public int questStateId = 0;
    public QuestData questChapter;

    [Header("Optional UI")]
    public GameObject overheadUI;
    public TextMeshProUGUI overheadNameText;

    private bool isPlayerInRange;
    private Transform playerTransform;
    private Animator npcAnimator;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        npcAnimator = GetComponentInChildren<Animator>();

        if (overheadNameText != null)
            overheadNameText.text = npcDisplayName;

        if (overheadUI != null)
            overheadUI.SetActive(false);

        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }

    private void Update()
    {
        bool dialogueOpen = DialogueManager.Instance != null && DialogueManager.Instance.isDialogueOpen;

        if (overheadUI != null)
            overheadUI.SetActive(isPlayerInRange && !dialogueOpen);

        if (!isPlayerInRange || dialogueOpen)
        {
            if (!isPlayerInRange)
                ShopManager.Instance?.HideInteractPrompt();
            return;
        }

        if (!TryGetActiveDialogue(out DialogueData dialogue, out int activeStep))
        {
            ShopManager.Instance?.HideInteractPrompt();
            return;
        }

        ShopManager.Instance?.ShowInteractPrompt(interactPromptName);

        if (!Input.GetKeyDown(ShopManager.GetInteractKey()) || Time.timeScale == 0f)
            return;

        ShopManager.Instance?.HideInteractPrompt();
        BeginDialogue(dialogue, activeStep);
    }

    private bool TryGetActiveDialogue(out DialogueData dialogue, out int step)
    {
        dialogue = null;
        step = -1;

        if (QuestManager.Instance == null)
        {
            dialogue = awakeningDialogue;
            step = AwakeningStep;
            return dialogue != null;
        }

        if (QuestManager.Instance.IsAtQuestStep(questStateId, AwakeningStep))
        {
            dialogue = awakeningDialogue;
            step = AwakeningStep;
            return dialogue != null;
        }

        if (QuestManager.Instance.IsAtQuestStep(questStateId, ReturnStep))
        {
            dialogue = returnDialogue;
            step = ReturnStep;
            return dialogue != null;
        }

        return false;
    }

    private void BeginDialogue(DialogueData dialogueAsset, int step)
    {
        if (DialogueManager.Instance == null || dialogueAsset == null)
            return;

        FacePlayer();

        string speakerName = string.IsNullOrEmpty(dialogueAsset.npcName)
            ? npcDisplayName
            : dialogueAsset.npcName;

        DialogueManager.Instance.StartDialogue(
            speakerName,
            dialogueAsset.dialogueLines,
            isShop: false,
            shopNPC: null,
            storyAnim: npcAnimator,
            camNPC: null,
            camPlayer: null,
            onEnd: () => OnDialogueFinished(step),
            isQuest: false);
    }

    private void OnDialogueFinished(int step)
    {
        if (QuestManager.Instance == null)
            return;

        if (step == AwakeningStep && QuestManager.Instance.IsAtQuestStep(questStateId, AwakeningStep))
        {
            QuestManager.Instance.AdvanceStep(questStateId, AwakeningStep);
            return;
        }

        if (step == ReturnStep && QuestManager.Instance.IsAtQuestStep(questStateId, ReturnStep))
            QuestManager.Instance.CompleteCurrentQuest(questChapter);
    }

    private void FacePlayer()
    {
        if (playerTransform == null)
            return;

        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        isPlayerInRange = true;
        playerTransform = other.transform;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        isPlayerInRange = false;
        ShopManager.Instance?.HideInteractPrompt();

        if (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueOpen)
            DialogueManager.Instance.EndDialogue();
    }
}
