#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using VisionOfLight.Enemy;

/// <summary>
/// One-click setup for Chapter 3: Gareth NPC prefab, scene placement, and wave quest wiring.
/// Menu: Quests / Chapter 03 / Setup Gareth And Wave Quest
/// </summary>
public static class Chapter03GarethSetup
{
    private const string PaladinModelPath = "Assets/NPC/Characters/Gareth/Paladin WProp J Nordstrom.fbx";
    private const string AlbedoAnimatorPath = "Assets/NPC/Characters/Albedo/Animations/Albedo.controller";
    private const string AlbedoPrefabPath = "Assets/NPC/Prefabs/Albedo_NPC.prefab";
    private const string GarethPrefabPath = "Assets/NPC/Prefabs/Gareth_NPC.prefab";
    private const string GarethDataPath = "Assets/NPC/Data/Characters/Gareth_Data.asset";
    private const string GarethProfilePath = "Assets/Quests/Chapter03/Data/Gareth_DialogueProfile.asset";
    private const string Quest03Path = "Assets/Quests/Chapter03/Data/Quest_03_VillageTrial.asset";
    private const string SkeletonPrefabPath = "Assets/Enemy/Skeleton/Prefab/Skeleton.prefab";
    private const string Ch03TrialId = "ch03_village_trial";

    private static readonly Vector3 GarethWorldPosition = new Vector3(178f, 83.5f, -158f);
    private static readonly Vector3 GarethWorldRotation = new Vector3(0f, 120f, 0f);

    [MenuItem("Quests/Chapter 03/Setup Gareth And Wave Quest")]
    public static void SetupAll()
    {
        GameObject garethPrefab = CreateOrUpdateGarethPrefab();
        PlaceGarethInScene(garethPrefab);
        WireWaveQuest();
        WireQuestManager();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Chapter03] Gareth NPC, wave quest, and QuestManager wiring complete.");
    }

    [MenuItem("Quests/Chapter 03/Fix Gareth Prefab Only")]
    public static void FixGarethPrefabOnly()
    {
        GameObject garethPrefab = CreateOrUpdateGarethPrefab();
        if (garethPrefab == null)
            return;

        AssetDatabase.SaveAssets();
        Debug.Log("[Chapter03] Gareth_NPC prefab rebuilt with Animator, scripts, and quest marker.");
    }

    [MenuItem("Quests/Chapter 03/Reset Wave Trial For Testing")]
    public static void ResetWaveTrialForTesting()
    {
        ChallengeTrialRegistry.ClearQuestChallenge(Ch03TrialId, 2, 1);
        ChallengeTrialRegistry.ClearOneTimeTrial(Ch03TrialId);

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.mainQuestState = 2;
            QuestManager.Instance.questStepIndex = 1;
        }

        if (Application.isPlaying && PauseMenuManager.Instance != null)
            PauseMenuManager.Instance.SaveGameSilently();

        Debug.Log("[Chapter03] Wave trial cleared. Trial Id stays '" + Ch03TrialId + "'. Set quest to state 2 step 1 and try WaveQuest again.");
    }

    private static GameObject CreateOrUpdateGarethPrefab()
    {
        GameObject paladinModel = AssetDatabase.LoadAssetAtPath<GameObject>(PaladinModelPath);
        RuntimeAnimatorController animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AlbedoAnimatorPath);
        NPCData garethData = AssetDatabase.LoadAssetAtPath<NPCData>(GarethDataPath);
        NPCDialogueProfile dialogueProfile = AssetDatabase.LoadAssetAtPath<NPCDialogueProfile>(GarethProfilePath);
        QuestData quest03 = AssetDatabase.LoadAssetAtPath<QuestData>(Quest03Path);
        GameObject albedoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AlbedoPrefabPath);

        if (paladinModel == null || animatorController == null || garethData == null || dialogueProfile == null || quest03 == null)
        {
            Debug.LogError("[Chapter03] Missing Paladin model, Albedo animator, Gareth data, dialogue profile, or Quest 03 asset.");
            return null;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(GarethPrefabPath) != null)
            AssetDatabase.DeleteAsset(GarethPrefabPath);

        GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(paladinModel);
        PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        root.name = "Gareth_NPC";
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;

        Animator animator = root.GetComponent<Animator>();
        if (animator == null)
            animator = root.AddComponent<Animator>();

        Avatar avatar = LoadAvatarFromModel(PaladinModelPath);
        if (avatar == null)
        {
            Debug.LogError("[Chapter03] No Humanoid Avatar on Paladin FBX. Set Rig to Humanoid and click Apply.");
            Object.DestroyImmediate(root);
            return null;
        }

        animator.avatar = avatar;
        animator.runtimeAnimatorController = animatorController;
        animator.applyRootMotion = false;

        GameObject body = root;

        CapsuleCollider capsule = body.GetComponent<CapsuleCollider>();
        if (capsule == null)
            capsule = body.AddComponent<CapsuleCollider>();
        capsule.height = 2f;
        capsule.radius = 0.5f;
        capsule.center = new Vector3(0f, 1f, 0f);
        capsule.isTrigger = false;

        SphereCollider trigger = body.GetComponent<SphereCollider>();
        if (trigger == null)
            trigger = body.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 3f;
        trigger.center = Vector3.zero;

        StoryNPC storyNpc = body.GetComponent<StoryNPC>();
        if (storyNpc == null)
            storyNpc = body.AddComponent<StoryNPC>();
        storyNpc.myData = garethData;
        storyNpc.isStaticNPC = true;

        DialogueTrigger dialogueTrigger = body.GetComponent<DialogueTrigger>();
        if (dialogueTrigger == null)
            dialogueTrigger = body.AddComponent<DialogueTrigger>();
        dialogueTrigger.dialogueProfile = dialogueProfile;
        dialogueTrigger.dialogueStates.Clear();

        CopyOverheadUiFromTemplate(albedoPrefab, body.transform, storyNpc, garethData.npcName);
        AddQuestIcon(body, quest03, albedoPrefab);

        EnsureFolder("Assets/NPC/Prefabs");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, GarethPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static Avatar LoadAvatarFromModel(string modelPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
        foreach (Object asset in assets)
        {
            if (asset is Avatar avatar)
                return avatar;
        }

        return null;
    }

    private static void CopyOverheadUiFromTemplate(GameObject templatePrefab, Transform body, StoryNPC storyNpc, string npcName)
    {
        if (templatePrefab == null)
            return;

        Transform existingOverhead = body.Find("OverheadUI");
        if (existingOverhead != null)
            Object.DestroyImmediate(existingOverhead.gameObject);

        Transform overheadTemplate = FindDeepChild(templatePrefab.transform, "OverheadUI");
        if (overheadTemplate == null)
            return;

        Transform overhead = Object.Instantiate(overheadTemplate.gameObject, body, false).transform;
        overhead.name = "OverheadUI";
        overhead.localPosition = new Vector3(0f, 2.4f, 0f);
        overhead.localRotation = Quaternion.identity;
        storyNpc.overheadUI = overhead.gameObject;

        Transform nameObj = FindDeepChild(overhead, "NPC_Name");
        if (nameObj == null)
            return;

        storyNpc.npcNameTextObj = nameObj.gameObject;
        TextMeshProUGUI nameText = nameObj.GetComponent<TextMeshProUGUI>();
        if (nameText == null)
            return;

        storyNpc.overheadNameText = nameText;
        nameText.text = npcName;
    }

    private static void AddQuestIcon(GameObject body, QuestData questData, GameObject templatePrefab)
    {
        Transform existingIcon = body.transform.Find("QuestIcon");
        if (existingIcon != null)
            Object.DestroyImmediate(existingIcon.gameObject);

        if (templatePrefab == null)
            return;

        Transform questIconTemplate = FindDeepChild(templatePrefab.transform, "QuestIcon");
        if (questIconTemplate == null)
            return;

        GameObject questIcon = Object.Instantiate(questIconTemplate.gameObject, body.transform, false);
        questIcon.name = "QuestIcon";
        questIcon.transform.localPosition = new Vector3(0f, 2.4f, 0f);
        questIcon.transform.localRotation = Quaternion.identity;
        questIcon.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        QuestMarkerUI marker = questIcon.GetComponent<QuestMarkerUI>();
        if (marker == null)
            return;

        marker.relatedQuests = new[] { questData };
        marker.requiredStep = 0;
    }

    private static void PlaceGarethInScene(GameObject garethPrefab)
    {
        if (garethPrefab == null)
            return;

        GameObject existing = GameObject.Find("Gareth_NPC");
        if (existing != null)
            Object.DestroyImmediate(existing);

        Transform npcParent = GameObject.Find("NPC")?.transform;
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(garethPrefab);
        instance.name = "Gareth_NPC";

        if (npcParent != null)
            instance.transform.SetParent(npcParent, true);

        instance.transform.position = GarethWorldPosition;
        instance.transform.rotation = Quaternion.Euler(GarethWorldRotation);
    }

    private static void WireWaveQuest()
    {
        GameObject waveObject = GameObject.Find("WaveQuest");
        if (waveObject == null)
        {
            Debug.LogWarning("[Chapter03] WaveQuest not found in scene.");
            return;
        }

        waveObject.SetActive(true);
        ChallengeStone stone = waveObject.GetComponent<ChallengeStone>();
        if (stone == null)
            return;

        stone.trialId = Ch03TrialId;
        stone.stoneType = ChallengeStone.ChallengeStoneType.OneTime;
        stone.promptText = "Start Trial";
        stone.challengeTimeLimit = 180f;
        stone.successMessage = "Trial Complete";
        stone.failMessage = "Trial Failed";

        GameObject skeletonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkeletonPrefabPath);
        Transform spawnPoint = waveObject.transform.Find("Spawn_1");
        if (skeletonPrefab == null || spawnPoint == null)
        {
            Debug.LogWarning("[Chapter03] Could not auto-wire wave enemies. Assign Skeleton prefab and Spawn_1 manually.");
            return;
        }

        ChallengeStone.QuestChallenge challenge = new ChallengeStone.QuestChallenge
        {
            challengeName = "Chapter 3 Village Trial",
            targetQuestState = 2,
            targetQuestStep = 1,
            nextQuestState = 2,
            advanceQuestStepOnSuccess = true,
            waves = new[]
            {
                new ChallengeStone.Wave
                {
                    waveName = "Wave 1",
                    delayBetweenSpawns = 1f,
                    enemiesToSpawn = new[]
                    {
                        new ChallengeStone.EnemySpawnInfo
                        {
                            enemyPrefab = skeletonPrefab,
                            count = 3,
                            spawnPoints = new[] { spawnPoint }
                        }
                    }
                }
            }
        };

        stone.questChallenges = new[] { challenge };
        stone.trialWaves = System.Array.Empty<ChallengeStone.Wave>();
        EditorUtility.SetDirty(stone);
    }

    private static void WireQuestManager()
    {
        QuestManager questManager = Object.FindFirstObjectByType<QuestManager>();
        if (questManager == null)
            return;

        QuestData quest03 = AssetDatabase.LoadAssetAtPath<QuestData>(Quest03Path);
        if (quest03 == null)
            return;

        if (!questManager.allQuestLibrary.Contains(quest03))
            questManager.allQuestLibrary.Add(quest03);

        Transform managerRoot = questManager.transform;
        Transform quest03Manager = managerRoot.Find("Quest03_Manager");
        if (quest03Manager == null)
        {
            GameObject managerObject = new GameObject("Quest03_Manager");
            managerObject.transform.SetParent(managerRoot, false);
            managerObject.AddComponent<Quest03ChapterManager>();
        }

        EditorUtility.SetDirty(questManager);
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent.name == childName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeepChild(parent.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folderName = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif
