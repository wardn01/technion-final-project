#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One-click rebuild for Quest 01 scene wiring. Run from the menu — saves you from hand-editing YAML.
/// Does not run automatically; you choose when to apply and save the scene.
/// </summary>
public static class Quest01SceneSetupTool
{
    private const string DefaultIntroMessage =
        "The Shadow Entity attacked the village...\nThe Magic Stone was stolen...\nYour friends did not survive...";

    [MenuItem("Quests/Chapter 01/Setup Quest01_Manager In Open Scene")]
    public static void SetupQuest01Manager()
    {
        QuestManager questManager = Object.FindAnyObjectByType<QuestManager>();
        if (questManager == null)
        {
            EditorUtility.DisplayDialog("Quest 01 Setup", "QuestManager was not found in the open scene.", "OK");
            return;
        }

        Transform questRoot = questManager.transform;
        Transform quest01Transform = questRoot.Find("Quest01_Manager");
        GameObject quest01Go;

        if (quest01Transform != null)
        {
            quest01Go = quest01Transform.gameObject;
        }
        else
        {
            quest01Go = new GameObject("Quest01_Manager", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(quest01Go, "Create Quest01_Manager");
            quest01Go.transform.SetParent(questRoot, false);
        }

        Quest01ChapterManager chapter = GetOrAdd<Quest01ChapterManager>(quest01Go);
        IntroCutsceneManager intro = GetOrAdd<IntroCutsceneManager>(quest01Go);
        AwakeningManager awakening = GetOrAdd<AwakeningManager>(quest01Go);

        AwakeningManager legacyAwakening = FindLegacyAwakeningManager(awakening);
        if (legacyAwakening != null)
            CopyAwakeningReferences(legacyAwakening, awakening);

        WireIntroUi(intro, awakening);
        WireIntroAudio(intro);

        intro.introMessage = DefaultIntroMessage;
        intro.awakeningManager = awakening;
        awakening.introCutsceneManager = intro;
        chapter.intro = intro;
        chapter.awakening = awakening;
        chapter.ResolveReferences();
        intro.ResolveFlowReferences();
        intro.ResolveUiReferences(awakening);

        if (legacyAwakening != null && legacyAwakening != awakening)
        {
            Undo.RecordObject(legacyAwakening, "Disable legacy AwakeningManager");
            legacyAwakening.enabled = false;
            EditorUtility.DisplayDialog(
                "Quest 01 Setup",
                "Quest01_Manager is wired.\n\nThe old AwakeningManager on CinematicCanvasManager was disabled (not deleted).",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Quest 01 Setup", "Quest01_Manager is wired.", "OK");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = quest01Go;
    }

    private static AwakeningManager FindLegacyAwakeningManager(AwakeningManager current)
    {
        foreach (AwakeningManager candidate in Object.FindObjectsByType<AwakeningManager>(FindObjectsSortMode.None))
        {
            if (candidate != null && candidate != current)
                return candidate;
        }

        return null;
    }

    private static void CopyAwakeningReferences(AwakeningManager from, AwakeningManager to)
    {
        Undo.RecordObject(to, "Copy Awakening references");

        to.blackScreen = from.blackScreen;
        to.mainCanvas = from.mainCanvas;
        to.cinematicAnimator = from.cinematicAnimator;
        to.cinematicDummy = from.cinematicDummy;
        to.realPlayer = from.realPlayer;
        to.sleepCamera = from.sleepCamera;
        to.sitUpDuration = from.sitUpDuration;
        to.standUpDuration = from.standUpDuration;
        to.cameraBlendTime = from.cameraBlendTime;
        to.sitPoint = from.sitPoint;
        to.standPoint = from.standPoint;
        to.playerSpawnYOffset = from.playerSpawnYOffset;
    }

    private static void WireIntroUi(IntroCutsceneManager intro, AwakeningManager awakening)
    {
        if (awakening.blackScreen == null)
        {
            Image blackScreen = GameObject.Find("BlackScreen")?.GetComponent<Image>();
            if (blackScreen != null)
                awakening.blackScreen = blackScreen;
        }

        if (awakening.blackScreen != null)
        {
            intro.introOverlay = awakening.blackScreen.gameObject;
            intro.overlayImage = awakening.blackScreen;
        }

        if (intro.introText == null && intro.introOverlay != null)
        {
            intro.introText = intro.introOverlay.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (intro.introText == null)
        {
            TextMeshProUGUI namedText = GameObject.Find("IntroText")?.GetComponent<TextMeshProUGUI>();
            if (namedText != null)
                intro.introText = namedText;
        }
    }

    private static void WireIntroAudio(IntroCutsceneManager intro)
    {
        if (intro.typeAudioSource == null)
        {
            GameObject uiAudio = GameObject.Find("UIAudioPlayer");
            if (uiAudio != null)
                intro.typeAudioSource = uiAudio.GetComponent<AudioSource>();
        }

        if (intro.typeCharacterClip == null)
        {
            intro.typeCharacterClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Quests/Chapter01/Audio/Quest01_TypeCharacter.mp3");
        }
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component != null)
            return component;

        return Undo.AddComponent<T>(go);
    }
}
#endif
