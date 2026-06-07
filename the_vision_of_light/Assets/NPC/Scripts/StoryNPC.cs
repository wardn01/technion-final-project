using UnityEngine;
using TMPro;
using System.Collections;

public class StoryNPC : MonoBehaviour
{
    [Header("NPC Data")]
    public NPCData myData;
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
        if (player != null) playerTransform = player.transform;

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

    void SetupMapIcon()
    {
        if (mapIconObject == null || myData.npcIcon == null) return;

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
        bool isMenuOpen = isDialogueOpen;

        if (overheadUI != null)
        {
            if (isPlayerInRange && !isMenuOpen)
            {
                overheadUI.SetActive(true);
                if (npcNameTextObj != null) npcNameTextObj.SetActive(true);
            }
            else
            {
                overheadUI.SetActive(false);
            }
        }

        if (isPlayerInRange && myData != null && !isBusy)
        {
            if (!isMenuOpen)
            {
                if (ShopManager.Instance != null)
                    ShopManager.Instance.ShowInteractPrompt(myData.npcName);

                if (Input.GetKeyDown(KeyCode.F) && Time.timeScale != 0f)
                {
                    if (ShopManager.Instance != null)
                        ShopManager.Instance.HideInteractPrompt();

                    if (isStaticNPC)
                    {
                        FaceEachOtherInstantly();
                        if (questTrigger != null) questTrigger.TriggerDialogue();
                    }
                    else
                    {
                        if (!hasStoodUp)
                        {
                            StartCoroutine(InteractionSequence());
                        }
                        else 
                        {
                            FaceEachOtherInstantly();
                            if (questTrigger != null) questTrigger.TriggerDialogue();
                        }
                    }
                }
            }
            else
            {
                if (ShopManager.Instance != null)
                    ShopManager.Instance.HideInteractPrompt();
            }
        }
    }

    private void FaceEachOtherInstantly()
    {
        if (playerTransform == null) return;

        Vector3 npcDir = playerTransform.position - transform.position;
        npcDir.y = 0;
        if (npcDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(npcDir);

        Vector3 playerDir = transform.position - playerTransform.position;
        playerDir.y = 0;
        if (playerDir != Vector3.zero) playerTransform.rotation = Quaternion.LookRotation(playerDir);
    }

    private IEnumerator InteractionSequence()
    {
        isBusy = true;

        if (myAnimator != null)
        {
            myAnimator.SetTrigger("StandUp");
            myAnimator.SetBool("IsStanding", true);
            myAnimator.SetInteger("TalkIndex", Random.Range(0, 3));
            myAnimator.SetBool("IsTalk", true);
        }

        yield return new WaitForSeconds(standUpDuration);

        if (npcCollider != null)
            npcCollider.enabled = false;

        yield return StartCoroutine(LookAtEachOther());
        yield return StartCoroutine(StepForwardRoutine());

        if (npcCollider != null)
            npcCollider.enabled = true;

        hasStoodUp = true;
        isBusy = false;

        if (questTrigger != null)
            questTrigger.TriggerDialogue();
    }

    private IEnumerator LookAtEachOther()
    {
        Animator playerAnim = playerTransform.GetComponentInChildren<Animator>();

        if (myAnimator != null) myAnimator.applyRootMotion = false;
        if (playerAnim != null) playerAnim.applyRootMotion = false;

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
        if (playerTransform != null) playerTransform.rotation = playerRot;
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
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;

            if (ShopManager.Instance != null)
                ShopManager.Instance.HideInteractPrompt();

            if (DialogueManager.Instance != null)
                DialogueManager.Instance.EndDialogue();
        }
    }
}