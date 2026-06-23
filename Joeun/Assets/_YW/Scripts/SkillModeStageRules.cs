using UnityEngine;

public class SkillModeStageRules : MonoBehaviour
{
    [SerializeField] private bool allowRotate = true;
    [SerializeField] private bool allowAssemble;
    [SerializeField] private bool allowDecode;
    [SerializeField] private bool clearRuntimeGrantsOnEnable = true;

    private static SkillModeStageRules activeRules;
    private static bool runtimeRotateGranted;
    private static bool runtimeAssembleGranted;
    private static bool runtimeDecodeGranted;

    public static bool AllowRotate => activeRules != null && (activeRules.allowRotate || runtimeRotateGranted);
    public static bool AllowAssemble => activeRules != null && (activeRules.allowAssemble || runtimeAssembleGranted);
    public static bool AllowDecode => activeRules != null && (activeRules.allowDecode || runtimeDecodeGranted);

    public static bool IsAllowed(SkillModeType mode)
    {
        switch (mode)
        {
            case SkillModeType.Rotate:
                return AllowRotate;
            case SkillModeType.Assemble:
                return AllowAssemble;
            case SkillModeType.Decode:
                return AllowDecode;
            default:
                return true;
        }
    }

    public static void Grant(SkillModeType mode)
    {
        switch (mode)
        {
            case SkillModeType.Rotate:
                runtimeRotateGranted = true;
                break;
            case SkillModeType.Assemble:
                runtimeAssembleGranted = true;
                break;
            case SkillModeType.Decode:
                runtimeDecodeGranted = true;
                break;
        }
    }

    public static void ClearRuntimeGrants()
    {
        runtimeRotateGranted = false;
        runtimeAssembleGranted = false;
        runtimeDecodeGranted = false;
    }

    private void OnEnable()
    {
        if (clearRuntimeGrantsOnEnable)
            ClearRuntimeGrants();

        PuzzleModeLock.ClearContextLock();
        activeRules = this;
        SkillIconModeView.ClearMode();
    }

    private void OnDisable()
    {
        if (activeRules == this)
            activeRules = null;
    }
}
