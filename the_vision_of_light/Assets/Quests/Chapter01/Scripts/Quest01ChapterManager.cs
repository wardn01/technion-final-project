using UnityEngine;

/// <summary>
/// Chapter 1 bootstrap: intro cutscene + bed awakening.
/// Add to <c>Quest01_Manager</c> under the central QuestManager hierarchy alongside
/// <see cref="IntroCutsceneManager"/> and <see cref="AwakeningManager"/>.
/// </summary>
[DefaultExecutionOrder(-350)]
[DisallowMultipleComponent]
public class Quest01ChapterManager : QuestChapterManager
{
    public override int ChapterStateId => 0;

    [Header("Chapter 01 Components")]
    public IntroCutsceneManager intro;
    public AwakeningManager awakening;

    public override void ResolveReferences()
    {
        intro ??= GetComponent<IntroCutsceneManager>();
        awakening ??= GetComponent<AwakeningManager>();

        if (intro == null || awakening == null)
            return;

        if (intro.awakeningManager == null)
            intro.awakeningManager = awakening;

        if (awakening.introCutsceneManager == null)
            awakening.introCutsceneManager = intro;

        intro.ResolveUiReferences(awakening);
        WireHouseDoorQuestPathFlags();
    }

    private static void WireHouseDoorQuestPathFlags()
    {
        DoorTeleporter[] doors = FindObjectsByType<DoorTeleporter>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (DoorTeleporter door in doors)
        {
            if (door == null)
                continue;

            switch (door.gameObject.name)
            {
                case "EnterHouse":
                    door.hideQuestPathAtDestination = true;
                    door.showQuestPathAtDestination = false;
                    break;
                case "ExitHouse":
                    door.hideQuestPathAtDestination = false;
                    door.showQuestPathAtDestination = true;
                    break;
            }
        }
    }
}
