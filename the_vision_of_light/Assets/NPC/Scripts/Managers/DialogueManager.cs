using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject dialoguePanel;
    public GameObject hudScreen;
    public GameObject quickSlotsBar;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    [Header("Buttons")]
    public GameObject continueButton;
    public GameObject shopButton;
    public GameObject leaveButton;

    [Header("Settings")]
    public float typingSpeed = 0.04f;

    [HideInInspector] 
    public bool isDialogueOpen = false;

    private Queue<string> sentences;
    private bool isTyping = false;
    private bool isShopDialogue = false;
    private bool isPlayerTurnToSpeak = false;
    private string currentSentence = "";
    private ShopkeeperNPC currentShopNPC;
    private Animator currentStoryNPCAnim;
    private CinemachineCamera npcCamera;
    private CinemachineCamera playerCamera;
    private System.Action onDialogueEndCallback;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        sentences = new Queue<string>();
    }

    private void Start()
    {
        dialoguePanel.SetActive(false);
        if (shopButton != null) shopButton.GetComponent<Button>().onClick.AddListener(OpenShop);
        if (leaveButton != null) leaveButton.GetComponent<Button>().onClick.AddListener(EndDialogue);
    }

    public void StartDialogue(
        string npcName,
        string[] lines,
        bool isShop = false,
        ShopkeeperNPC shopNPC = null,
        Animator storyAnim = null,
        CinemachineCamera camNPC = null,
        CinemachineCamera camPlayer = null,
        System.Action onEnd = null)
    {
        isDialogueOpen = true;
        isShopDialogue = isShop;
        currentShopNPC = shopNPC;
        currentStoryNPCAnim = storyAnim;
        npcCamera = camNPC;
        playerCamera = camPlayer;
        isPlayerTurnToSpeak = false;
        onDialogueEndCallback = onEnd;

        if (UIManager.Instance != null) UIManager.Instance.isDialogueOpen = true;
        if (ShopManager.Instance != null) ShopManager.Instance.SetPlayerFreeze(true);

        if (hudScreen != null) hudScreen.SetActive(false);
        if (quickSlotsBar != null) quickSlotsBar.SetActive(false);

        dialoguePanel.SetActive(true);
        continueButton.SetActive(true);
        if (shopButton != null) shopButton.SetActive(false);
        if (leaveButton != null) leaveButton.SetActive(false);

        nameText.text = npcName;
        sentences.Clear();

        foreach (string line in lines) sentences.Enqueue(line);
        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = currentSentence;
            isTyping = false;
            CheckIfLastSentence();
            SetNPCTalkingState(false);
            return;
        }

        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        if (npcCamera != null && playerCamera != null)
        {
            if (isPlayerTurnToSpeak)
            {
                playerCamera.Priority = 20;
                npcCamera.Priority = 0;
            }
            else
            {
                npcCamera.Priority = 20;
                playerCamera.Priority = 0;
            }
            isPlayerTurnToSpeak = !isPlayerTurnToSpeak;
        }

        currentSentence = sentences.Dequeue();
        StartCoroutine(TypeSentence(currentSentence));
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";

        if (!isPlayerTurnToSpeak) SetNPCTalkingState(true);

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        CheckIfLastSentence();
        SetNPCTalkingState(false);
    }

    private void SetNPCTalkingState(bool isTalking)
    {
        Animator anim = currentShopNPC != null ? currentShopNPC.GetComponent<Animator>() : currentStoryNPCAnim;
        if (anim != null)
        {
            if (isTalking) anim.SetInteger("TalkIndex", Random.Range(0, 3));
            anim.SetBool("IsTalk", isTalking);
        }
    }

    private void CheckIfLastSentence()
    {
        if (sentences.Count == 0 && isShopDialogue)
        {
            continueButton.SetActive(false);
            if (shopButton != null) shopButton.SetActive(true);
            if (leaveButton != null) leaveButton.SetActive(true);
        }
    }

    public void ShowShopOptions()
    {
        dialoguePanel.SetActive(true);
        continueButton.SetActive(false);
        if (shopButton != null) shopButton.SetActive(true);
        if (leaveButton != null) leaveButton.SetActive(true);
    }

    public void OpenShop()
    {
        dialoguePanel.SetActive(false);
        if (currentShopNPC != null && ShopManager.Instance != null)
            ShopManager.Instance.OpenShop(currentShopNPC.itemsToSell);
    }

    public void EndDialogue()
    {
        SetNPCTalkingState(false);

        if (npcCamera != null) npcCamera.Priority = 0;
        if (playerCamera != null) playerCamera.Priority = 0;

        dialoguePanel.SetActive(false);

        if (hudScreen != null) hudScreen.SetActive(true);
        if (quickSlotsBar != null) quickSlotsBar.SetActive(true);

        isDialogueOpen = false;
        isShopDialogue = false;
        currentShopNPC = null;
        currentStoryNPCAnim = null;

        if (UIManager.Instance != null) UIManager.Instance.isDialogueOpen = false;
        if (ShopManager.Instance != null && !ShopManager.Instance.shopPanel.activeSelf)
            ShopManager.Instance.SetPlayerFreeze(false);

        if (onDialogueEndCallback != null)
        {
            onDialogueEndCallback.Invoke();
            onDialogueEndCallback = null;
        }
    }
}