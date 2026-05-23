using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class StoryNPC : MonoBehaviour
{
    [Header("NPC Data")]
    public NPCData myData;

    [Header("Map Settings")]
    public Transform mapIconObject;

    [Header("Overhead UI")]
    public GameObject overheadUI;
    public GameObject npcNameTextObj;
    public TextMeshProUGUI overheadNameText;
    public Image overheadIconImage;

    [Header("Quest Marker")]
    public GameObject questMarkerIcon;

    [Header("Settings")]
    public float iconVisibleDistance = 50f;

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

            if (overheadIconImage != null && myData.npcIcon != null)
                overheadIconImage.sprite = myData.npcIcon;

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
        bool isShopOpen = ShopManager.Instance != null && ShopManager.Instance.shopPanel.activeSelf;
        bool isDialogueOpen = UIManager.Instance != null && UIManager.Instance.isDialogueOpen;
        bool isMenuOpen = isShopOpen || isDialogueOpen;

        bool hasActiveQuest = false;

        if (questTrigger != null && QuestManager.Instance != null)
        {
            hasActiveQuest = questTrigger.dialogueStates.Exists(
                x => x.stateId == QuestManager.Instance.mainQuestState
            );
        }

        if (playerTransform != null && overheadUI != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);

            if (distance <= iconVisibleDistance && !isMenuOpen)
            {
                overheadUI.SetActive(true);

                if (npcNameTextObj != null)
                    npcNameTextObj.SetActive(isPlayerInRange);

                if (questMarkerIcon != null)
                    questMarkerIcon.SetActive(hasActiveQuest);

                if (overheadIconImage != null)
                    overheadIconImage.gameObject.SetActive(!hasActiveQuest);
            }
            else
            {
                overheadUI.SetActive(false);
            }
        }

        if (isPlayerInRange && myData != null && !isMenuOpen && !isBusy)
        {
            if (ShopManager.Instance != null)
                ShopManager.Instance.ShowInteractPrompt(myData.npcName);

            if (Input.GetKeyDown(KeyCode.F) && Time.timeScale != 0f)
            {
                if (ShopManager.Instance != null)
                    ShopManager.Instance.HideInteractPrompt();

                if (!hasStoodUp)
                    StartCoroutine(InteractionSequence());
                else if (questTrigger != null)
                    questTrigger.TriggerDialogue();
            }
        }
        else if (!isPlayerInRange && ShopManager.Instance != null)
        {
            ShopManager.Instance.HideInteractPrompt();
        }
    }

    private IEnumerator InteractionSequence()
    {
        isBusy = true;

        if (ShopManager.Instance != null)
            ShopManager.Instance.SetPlayerFreeze(true);

        if (myAnimator != null)
        {
            myAnimator.SetTrigger("StandUp");
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