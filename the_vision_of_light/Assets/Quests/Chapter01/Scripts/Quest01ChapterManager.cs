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
    #region Chapter Identity
    public override int ChapterStateId => 0;
    #endregion

    #region Chapter Components
    [Header("Chapter 01 Components")]
    public IntroCutsceneManager intro;
    public AwakeningManager awakening;
    #endregion

    #region Reference Resolution
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
    #endregion

    #region Door Wiring
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
    #endregion
}
