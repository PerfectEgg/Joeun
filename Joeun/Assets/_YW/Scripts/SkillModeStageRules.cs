using UnityEngine;
using System;

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

    public static event Action OnAvailabilityChanged;

    public static bool AllowRotate => runtimeRotateGranted || (activeRules != null && activeRules.allowRotate);
    public static bool AllowAssemble => runtimeAssembleGranted || (activeRules != null && activeRules.allowAssemble);
    public static bool AllowDecode => runtimeDecodeGranted || (activeRules != null && activeRules.allowDecode);

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
        bool changed = false;

        switch (mode)
        {
            case SkillModeType.Rotate:
                changed = !runtimeRotateGranted;
                runtimeRotateGranted = true;
                break;
            case SkillModeType.Assemble:
                changed = !runtimeAssembleGranted;
                runtimeAssembleGranted = true;
                break;
            case SkillModeType.Decode:
                changed = !runtimeDecodeGranted;
                runtimeDecodeGranted = true;
                break;
        }

        if (changed)
            OnAvailabilityChanged?.Invoke();
    }

    public static void ClearRuntimeGrants()
    {
        bool changed = runtimeRotateGranted || runtimeAssembleGranted || runtimeDecodeGranted;

        runtimeRotateGranted = false;
        runtimeAssembleGranted = false;
        runtimeDecodeGranted = false;

        if (changed)
            OnAvailabilityChanged?.Invoke();
    }

    private void OnEnable()
    {
        if (clearRuntimeGrantsOnEnable)
            ClearRuntimeGrants();

        PuzzleModeLock.ClearContextLock();
        activeRules = this;
        OnAvailabilityChanged?.Invoke();
        SkillIconModeView.ClearMode();
    }

    private void OnDisable()
    {
        if (activeRules == this)
        {
            activeRules = null;
            OnAvailabilityChanged?.Invoke();
        }
    }
}
