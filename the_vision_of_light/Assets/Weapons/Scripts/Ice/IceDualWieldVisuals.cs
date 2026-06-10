using UnityEngine;

/// <summary>
/// Ice Sword dual-blade visuals on <c>IceSword.prefab</c>.
/// Right blade stays visible in combat; left blade appears only during Attack_1/2/3.
/// Both blades hide during Skill_Q.
/// </summary>
public class IceDualWieldVisuals : MonoBehaviour
{
    private const int CombatLayerIndex = 1;

    #region Left Blade Transform
    [Header("Left Hand Blade — adjust in Play Mode or here")]
    [Tooltip("Local position on mixamorig:LeftHand")]
    public Vector3 leftHandLocalPosition = new Vector3(0.06f, 0.04f, 0.03f);

    [Tooltip("Local rotation on mixamorig:LeftHand")]
    public Vector3 leftHandLocalEulerAngles = new Vector3(0f, 180f, -90f);

    [Tooltip("Local scale on mixamorig:LeftHand")]
    public Vector3 leftHandLocalScale = new Vector3(0.8f, 0.8f, 0.8f);
    #endregion

    #region State
    private GameObject leftBlade;
    private Animator animator;
    private Renderer[] rightRenderers;
    private Renderer[] leftRenderers;
    private bool leftBladeVisible;
    #endregion

    #region Unity Lifecycle
    private void OnEnable()
    {
        CacheReferences();
        SetRightBladeVisible(true);
        SetLeftBladeVisible(false);
    }

    private void OnDisable()
    {
        SetRightBladeVisible(false);
        SetLeftBladeVisible(false);
        DestroyLeftBlade();
    }

    private void OnDestroy()
    {
        DestroyLeftBlade();
    }

    private void LateUpdate()
    {
        CacheReferences();
        TryCreateLeftBlade();

        if (animator == null) return;

        bool skillQ = IsSkillQState();
        bool normalAttack = IsNormalAttackState() && !skillQ;

        SetRightBladeVisible(!skillQ);
        SetLeftBladeVisible(normalAttack);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyLeftBladeTransform();
    }
#endif
    #endregion

    #region Animator States
    private bool IsSkillQState()
    {
        if (animator.GetLayerWeight(CombatLayerIndex) <= 0.01f)
            return false;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(CombatLayerIndex);
        return state.IsName("Skill_Q");
    }

    private bool IsNormalAttackState()
    {
        if (animator.GetLayerWeight(CombatLayerIndex) <= 0.01f)
            return false;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(CombatLayerIndex);
        return state.IsName("Attack_1")
            || state.IsName("Attack_2")
            || state.IsName("Attack_3");
    }
    #endregion

    #region Blade Setup
    private void CacheReferences()
    {
        if (animator == null)
            animator = GetComponentInParent<Animator>();

        if (rightRenderers == null || rightRenderers.Length == 0)
            rightRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void TryCreateLeftBlade()
    {
        if (leftBlade != null || animator == null) return;

        Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        if (leftHand == null) return;

        MeshFilter sourceFilter = GetComponentInChildren<MeshFilter>();
        MeshRenderer sourceRenderer = GetComponentInChildren<MeshRenderer>();
        if (sourceFilter == null || sourceRenderer == null) return;

        leftBlade = new GameObject("LeftIceBlade");
        leftBlade.transform.SetParent(leftHand, false);
        ApplyLeftBladeTransform();

        MeshFilter leftFilter = leftBlade.AddComponent<MeshFilter>();
        leftFilter.sharedMesh = sourceFilter.sharedMesh;

        MeshRenderer leftRenderer = leftBlade.AddComponent<MeshRenderer>();
        leftRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
        leftRenderer.enabled = false;

        leftRenderers = new[] { leftRenderer };
        leftBladeVisible = false;
    }

    private void DestroyLeftBlade()
    {
        leftBladeVisible = false;
        leftRenderers = null;

        if (leftBlade == null) return;

        Destroy(leftBlade);
        leftBlade = null;
    }

    private void ApplyLeftBladeTransform()
    {
        if (leftBlade == null) return;

        leftBlade.transform.localPosition = leftHandLocalPosition;
        leftBlade.transform.localRotation = Quaternion.Euler(leftHandLocalEulerAngles);
        leftBlade.transform.localScale = leftHandLocalScale;
    }
    #endregion

    #region Visibility
    private void SetRightBladeVisible(bool visible)
    {
        SetRenderersEnabled(rightRenderers, visible);
    }

    private void SetLeftBladeVisible(bool visible)
    {
        if (leftRenderers == null)
        {
            leftBladeVisible = visible;
            return;
        }

        if (leftBladeVisible == visible) return;

        leftBladeVisible = visible;
        SetRenderersEnabled(leftRenderers, visible);
    }

    private static void SetRenderersEnabled(Renderer[] renderers, bool enabled)
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = enabled;
        }
    }
    #endregion
}
