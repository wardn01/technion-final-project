using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoryNPC : MonoBehaviour
{
    [Header("NPC Data")]
    public NPCData myData;

    [Header("Map Settings")]
    public Transform mapIconObject; 

    [Header("Overhead UI (World Space)")]
    public GameObject overheadUI; 
    public GameObject npcNameTextObj; 
    public TextMeshProUGUI overheadNameText; 
    public Image overheadIconImage; 
    public float iconVisibleDistance = 15f; 

    private bool isPlayerInRange = false;
    private Transform playerTransform; 
    
    private Animator myAnimator;
    private DialogueTrigger questTrigger;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
        
        myAnimator = GetComponentInChildren<Animator>();
        questTrigger = GetComponent<DialogueTrigger>();

        if (overheadUI != null) overheadUI.SetActive(false);

        if (myData != null)
        {
            if (overheadNameText != null) overheadNameText.text = myData.npcName;
            if (overheadIconImage != null && myData.npcIcon != null) overheadIconImage.sprite = myData.npcIcon;
            SetupMapIcon();
        }
    }

    void SetupMapIcon()
    {
        if (mapIconObject == null || myData.npcIcon == null) return;
        SpriteRenderer sr = mapIconObject.GetComponent<SpriteRenderer>();
        if (sr == null) sr = mapIconObject.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = myData.npcIcon;
        sr.sortingOrder = 10;
        mapIconObject.gameObject.layer = LayerMask.NameToLayer("Minimap");
    }

    private void Update()
    {
        bool isShopOpen = ShopManager.Instance != null && ShopManager.Instance.shopPanel.activeSelf;
        bool isDialogueOpen = UIManager.Instance != null && UIManager.Instance.isDialogueOpen;
        bool isMenuOpen = isShopOpen || isDialogueOpen;

        bool hasActiveQuest = questTrigger != null && QuestManager.Instance != null && 
                             questTrigger.dialogueStates.Exists(x => x.stateId == QuestManager.Instance.mainQuestState);

        if (playerTransform != null && overheadUI != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance <= iconVisibleDistance && !isMenuOpen)
            {
                overheadUI.SetActive(true); 
                if (npcNameTextObj != null) npcNameTextObj.SetActive(isPlayerInRange);
                if (overheadIconImage != null) overheadIconImage.gameObject.SetActive(hasActiveQuest);
            }
            else overheadUI.SetActive(false); 
        }

        if (isPlayerInRange && myData != null && !isMenuOpen)
        {
            ShopManager.Instance.ShowInteractPrompt(myData.npcName);
            if (Input.GetKeyDown(KeyCode.F) && Time.timeScale != 0f)
            {
                ShopManager.Instance.HideInteractPrompt();
                if (myAnimator != null) 
                {
                    myAnimator.SetTrigger("StandUp");
                    myAnimator.SetInteger("TalkIndex", Random.Range(0, 3));
                    myAnimator.SetBool("IsTalk", true);
                }
                if (questTrigger != null) questTrigger.TriggerDialogue();
            }
        }
        else
            {
                if (!isPlayerInRange)
                    ShopManager.Instance.HideInteractPrompt();
            }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            ShopManager.Instance.HideInteractPrompt();
            if (DialogueManager.Instance != null) DialogueManager.Instance.EndDialogue();
        }
    }
}