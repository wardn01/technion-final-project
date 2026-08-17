using UnityEngine;

/// <summary>
/// Base for per-chapter quest logic placed as a child under the central <see cref="QuestManager"/> object.
/// Each chapter (Quest01_Manager, Quest02_Manager, …) wires its own scripts in <see cref="ResolveReferences"/>.
/// </summary>
public abstract class QuestChapterManager : MonoBehaviour
{
    #region Abstract API
    public abstract int ChapterStateId { get; }

    public abstract void ResolveReferences();
    #endregion

    #region Bootstrap
    /// <summary>Story chapters at or before this one may still need scene bootstrap (intro, cinematics).</summary>
    public virtual bool ShouldRunChapterBootstrap()
    {
        if (QuestManager.Instance == null)
            return ChapterStateId == 0;

        return QuestManager.Instance.mainQuestState <= ChapterStateId;
    }
    #endregion

    #region Unity Lifecycle
    protected virtual void Awake()
    {
        ResolveReferences();
    }
    #endregion

    #region Editor
#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        ResolveReferences();
    }
#endif
    #endregion
}
