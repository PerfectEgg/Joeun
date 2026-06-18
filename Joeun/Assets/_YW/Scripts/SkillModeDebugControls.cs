using UnityEngine;

/// <summary>
/// Editor/development-only shortcut controls for testing skill modes.
/// Attach to Stage Core or a debug object when needed.
/// </summary>
public class SkillModeDebugControls : MonoBehaviour
{
    [SerializeField] bool enableInEditor = true;
    [SerializeField] bool enableInDevelopmentBuild = true;

    [Header("Grant + Select")]
    [SerializeField] KeyCode rotateKey = KeyCode.F1;
    [SerializeField] KeyCode assembleKey = KeyCode.F2;
    [SerializeField] KeyCode decodeKey = KeyCode.F3;

    [Header("Utility")]
    [SerializeField] KeyCode clearModeKey = KeyCode.F4;
    [SerializeField] KeyCode clearGrantsKey = KeyCode.F5;

    void Update()
    {
        if (!IsDebugEnabled())
            return;

        if (Input.GetKeyDown(rotateKey))
            GrantAndSelect(SkillModeType.Rotate);

        if (Input.GetKeyDown(assembleKey))
            GrantAndSelect(SkillModeType.Assemble);

        if (Input.GetKeyDown(decodeKey))
            GrantAndSelect(SkillModeType.Decode);

        if (Input.GetKeyDown(clearModeKey))
            SkillIconModeView.ClearMode();

        if (Input.GetKeyDown(clearGrantsKey))
        {
            SkillModeStageRules.ClearRuntimeGrants();
            SkillIconModeView.ClearMode();
        }
    }

    void GrantAndSelect(SkillModeType mode)
    {
        SkillModeStageRules.Grant(mode);
        SkillIconModeView.SelectMode(mode);
    }

    bool IsDebugEnabled()
    {
#if UNITY_EDITOR
        if (enableInEditor)
            return true;
#endif

        return enableInDevelopmentBuild && Debug.isDebugBuild;
    }
}
