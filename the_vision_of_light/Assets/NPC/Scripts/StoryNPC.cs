using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Story/quest NPC (e.g. Albedo): interact prompt, stand-up cinematic, overhead name,
/// minimap icon, and dialogue via <see cref="DialogueTrigger"/>.
/// </summary>
public class StoryNPC : MonoBehaviour
{
    [Header("NPC Data")]
    public NPCData myData;

    [Tooltip("When true, skips stand-up/walk sequence and talks immediately.")]
    public bool isStaticNPC = false;

    [Header("Map Settings")]
    public Transform mapIconObject;

    [Header("Overhead UI")]
    public GameObject overheadUI;
    public GameObject npcNameTextObj;
    public TextMeshProUGUI overheadNameText;

    [Header("Movement Settings")]
    public float stepDistance = 0.5f;
    public float stepDuration = 1f;
    public float standUpDuration = 1.5f;

    private bool isPlayerInRange;
    private bool hasStoodUp;
    private bool isBusy;

    private Transform playerTransform;
    private Animator myAnimator;
    private DialogueTrigger questTrigger;
    private Collider npcCollider;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        myAnimator = GetComponentInChildren<Animator>();
        questTrigger = GetComponent<DialogueTrigger>();
        npcCollider = GetComponent<Collider>();

        if (overheadUI != null)
            overheadUI.SetActive(false);

        if (myData != null)
        {
            if (overheadNameText != null)
                overheadNameText.text = myData.npcName;

            SetupMapIcon();
        }
    }

    private void SetupMapIcon()
    {
        if (mapIconObject == null)
        {
            GameObject iconObject = new GameObject("MapIcon");
            iconObject.transform.SetParent(transform);
            iconObject.transform.localPosition = new Vector3(0f, 3f, 0f);
            mapIconObject = iconObject.transform;
        }

        if (myData.npcIcon == null)
            return;

        SpriteRenderer sr = mapIconObject.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = mapIconObject.gameObject.AddComponent<SpriteRenderer>();

        sr.sprite = myData.npcIcon;
        sr.sortingOrder = 10;
        mapIconObject.gameObject.layer = LayerMask.NameToLayer("Minimap");
    }

    private void Update()
    {
        bool isDialogueOpen = UIManager.Instance != null && UIManager.Instance.isDialogueOpen;

        if (overheadUI != null)
        {
            if (isPlayerInRange && !isDialogueOpen)
            {
                overheadUI.SetActive(true);
                if (npcNameTextObj != null)
                    npcNameTextObj.SetActive(true);
            }
            else
            {
                overheadUI.SetActive(false);
            }
        }

        if (!isPlayerInRange || myData == null || isBusy)
            return;

        if (isDialogueOpen)
        {
            ShopManager.Instance?.HideInteractPrompt();
            return;
        }

        ShopManager.Instance?.ShowInteractPrompt(myData.npcName);

        if (!Input.GetKeyDown(ShopManager.GetInteractKey()) || Time.timeScale == 0f)
            return;

        ShopManager.Instance?.HideInteractPrompt();
        ShopManager.Instance?.SetPlayerFreeze(true);

        if (!hasStoodUp && !isStaticNPC)
        {
            StartCoroutine(InteractionSequence());
            return;
        }

        if (!hasStoodUp)
            hasStoodUp = true;

        if (isStaticNPC)
        {
            FaceEachOtherInstantly();
            questTrigger?.TriggerDialogue();
            return;
        }

        FaceEachOtherInstantly();
        questTrigger?.TriggerDialogue();
    }

    private void FaceEachOtherInstantly()
    {
        if (playerTransform == null)
            return;

        Vector3 npcDir = playerTransform.position - transform.position;
        npcDir.y = 0;
        if (npcDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(npcDir);

        Vector3 playerDir = transform.position - playerTransform.position;
        playerDir.y = 0;
        if (playerDir != Vector3.zero)
            playerTransform.rotation = Quaternion.LookRotation(playerDir);
    }

    private IEnumerator InteractionSequence()
    {
        isBusy = true;

        if (myAnimator != null)
        {
            myAnimator.SetBool("IsTalk", false);
            myAnimator.SetBool("IsWalking", false);
            myAnimator.ResetTrigger("StandUp");
            myAnimator.SetTrigger("StandUp");
        }

        yield return WaitForStandUpAnimation();
        SnapToStandingIdle();

        if (npcCollider != null)
            npcCollider.enabled = false;

        yield return StartCoroutine(LookAtEachOther());
        yield return StartCoroutine(StepForwardRoutine());

        if (npcCollider != null)
            npcCollider.enabled = true;

        SnapToStandingIdle();

        hasStoodUp = true;
        isBusy = false;

        questTrigger?.TriggerDialogue();
    }

    private IEnumerator WaitForStandUpAnimation()
    {
        if (myAnimator == null)
        {
            yield return new WaitForSeconds(standUpDuration);
            yield break;
        }

        float timeout = Mathf.Max(standUpDuration, 0.5f) + 1.5f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            AnimatorStateInfo state = myAnimator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("SitToStand"))
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        while (myAnimator != null)
        {
            AnimatorStateInfo state = myAnimator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName("SitToStand") || state.normalizedTime >= 0.98f)
                break;

            yield return null;
        }
    }

    private void SnapToStandingIdle()
    {
        if (myAnimator == null)
            return;

        myAnimator.SetBool("IsStanding", true);
        myAnimator.SetBool("IsTalk", false);
        myAnimator.SetBool("IsWalking", false);
        myAnimator.Play("Idle", 0, 0f);
        myAnimator.Update(0f);
    }

    private IEnumerator LookAtEachOther()
    {
        Animator playerAnim = playerTransform.GetComponentInChildren<Animator>();

        if (myAnimator != null)
            myAnimator.applyRootMotion = false;
        if (playerAnim != null)
            playerAnim.applyRootMotion = false;

        Vector3 npcDir = playerTransform.position - transform.position;
        npcDir.y = 0;
        Quaternion npcRot = Quaternion.LookRotation(npcDir == Vector3.zero ? transform.forward : npcDir);

        Vector3 playerDir = transform.position - playerTransform.position;
        playerDir.y = 0;
        Quaternion playerRot = Quaternion.LookRotation(playerDir == Vector3.zero ? playerTransform.forward : playerDir);

        Quaternion npcStart = transform.rotation;
        Quaternion playerStart = playerTransform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(npcStart, npcRot, t);
            if (playerTransform != null)
                playerTransform.rotation = Quaternion.Slerp(playerStart, playerRot, t);
            yield return null;
        }

        transform.rotation = npcRot;
        if (playerTransform != null)
            playerTransform.rotation = playerRot;
    }

    private IEnumerator StepForwardRoutine()
    {
        if (myAnimator != null)
            myAnimator.SetBool("IsWalking", true);

        Vector3 start = transform.position;
        Vector3 end = start + transform.forward * stepDistance;
        float t = 0f;

        while (t < stepDuration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, t / stepDuration);
            yield return null;
        }

        transform.position = end;

        if (myAnimator != null)
            myAnimator.SetBool("IsWalking", false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerTransform = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        isPlayerInRange = false;
        ShopManager.Instance?.HideInteractPrompt();

        if (isBusy)
        {
            StopAllCoroutines();
            isBusy = false;
            ReleasePlayerIfNotInDialogue();
        }

        if (DialogueManager.Instance != null)
            DialogueManager.Instance.EndDialogue();
    }

    private static void ReleasePlayerIfNotInDialogue()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueOpen)
            return;

        ShopManager.Instance?.SetPlayerFreeze(false);
    }

    /// <summary>
    /// Called after quest relocation — snap to standing idle (no sit pose at the new spot).
    /// </summary>
    public void ApplyStandingPoseAfterRelocation()
    {
        hasStoodUp = true;

        if (myAnimator == null)
            myAnimator = GetComponentInChildren<Animator>();

        SnapToStandingIdle();
    }
}
