using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(-1100)]
public class PuzzleModeLock : MonoBehaviour
{
    private static readonly List<PuzzleModeLock> activeLocks = new List<PuzzleModeLock>();
    private static bool hasContextLock;
    private static bool contextAllowRotate;
    private static bool contextAllowAssemble;
    private static bool contextAllowDecode;

    [SerializeField] private PuzzleModeManager modeManager;
    [SerializeField] private bool allowRotate = true;
    [SerializeField] private bool allowAssemble;
    [SerializeField] private bool allowDecode = true;
    [SerializeField] private bool clearBlockedMode = true;
    [SerializeField] private bool controlPuzzleManagerKeys;
    [SerializeField] private KeyCode rotateKeyWhenAllowed = KeyCode.Q;
    [SerializeField] private KeyCode assembleKeyWhenAllowed = KeyCode.W;

    private void Reset()
    {
        ResolveModeManager();
    }

    private void Awake()
    {
        ResolveModeManager();
        ApplyLock();
    }

    private void OnEnable()
    {
        if (!activeLocks.Contains(this))
            activeLocks.Add(this);

        SetContextLock(allowRotate, allowAssemble, allowDecode);
        SkillIconModeView.OnSkillModeChanged += HandleSkillModeChanged;
        ApplyLock();
    }

    private void OnDisable()
    {
        activeLocks.Remove(this);
        SkillIconModeView.OnSkillModeChanged -= HandleSkillModeChanged;
    }

    public static bool IsAllowedByActiveLocks(SkillModeType mode)
    {
        if (mode == SkillModeType.None)
            return true;

        PuzzleModeLock activeLock = GetCurrentActiveLock();
        if (activeLock != null)
            return activeLock.Allows(mode);

        if (hasContextLock)
            return AllowsContext(mode);

        return true;
    }

    public static void ClearContextLock()
    {
        hasContextLock = false;
        contextAllowRotate = false;
        contextAllowAssemble = false;
        contextAllowDecode = false;
    }

    private static PuzzleModeLock GetCurrentActiveLock()
    {
        for (int i = activeLocks.Count - 1; i >= 0; i--)
        {
            PuzzleModeLock modeLock = activeLocks[i];
            if (modeLock == null)
            {
                activeLocks.RemoveAt(i);
                continue;
            }

            if (!modeLock.isActiveAndEnabled)
                continue;

            return modeLock;
        }

        return null;
    }

    private static void SetContextLock(bool rotateAllowed, bool assembleAllowed, bool decodeAllowed)
    {
        hasContextLock = true;
        contextAllowRotate = rotateAllowed;
        contextAllowAssemble = assembleAllowed;
        contextAllowDecode = decodeAllowed;
    }

    private static bool AllowsContext(SkillModeType mode)
    {
        switch (mode)
        {
            case SkillModeType.Rotate:
                return contextAllowRotate;
            case SkillModeType.Assemble:
                return contextAllowAssemble;
            case SkillModeType.Decode:
                return contextAllowDecode;
            default:
                return true;
        }
    }

    private void LateUpdate()
    {
        ApplyLock();
    }

    public void Configure(bool rotateAllowed, bool assembleAllowed, bool clearBlocked = true)
    {
        Configure(rotateAllowed, assembleAllowed, true, clearBlocked);
    }

    public void Configure(bool rotateAllowed, bool assembleAllowed, bool decodeAllowed, bool clearBlocked = true)
    {
        allowRotate = rotateAllowed;
        allowAssemble = assembleAllowed;
        allowDecode = decodeAllowed;
        clearBlockedMode = clearBlocked;
        SetContextLock(allowRotate, allowAssemble, allowDecode);
        ApplyLock();
    }

    private void HandleSkillModeChanged(SkillModeType mode)
    {
        ApplyLock();
    }

    private bool Allows(SkillModeType mode)
    {
        switch (mode)
        {
            case SkillModeType.Rotate:
                return allowRotate;
            case SkillModeType.Assemble:
                return allowAssemble;
            case SkillModeType.Decode:
                return allowDecode;
            default:
                return true;
        }
    }

    private void ApplyLock()
    {
        ResolveModeManager();

        if (modeManager == null)
            return;

        if (controlPuzzleManagerKeys)
        {
            modeManager.rotateKey = allowRotate ? rotateKeyWhenAllowed : KeyCode.None;
            modeManager.assembleKey = allowAssemble ? assembleKeyWhenAllowed : KeyCode.None;
        }
        else
        {
            modeManager.rotateKey = KeyCode.None;
            modeManager.assembleKey = KeyCode.None;
        }

        if (!allowRotate && modeManager.IsRotate)
            ClearPuzzleMode();

        if (!allowAssemble && modeManager.IsAssemble)
            ClearPuzzleMode();

        if (!allowDecode && SkillIconModeView.CurrentMode == SkillModeType.Decode)
            ClearSkillMode();
    }

    private void ResolveModeManager()
    {
        if (modeManager != null)
            return;

        modeManager = GetComponent<PuzzleModeManager>();

        if (modeManager == null)
            modeManager = GetComponentInChildren<PuzzleModeManager>(true);

        if (modeManager == null)
            modeManager = PuzzleModeManager.Instance;
    }

    private void ClearPuzzleMode()
    {
        if (clearBlockedMode)
        {
            modeManager.SetMode(PuzzleModeManager.Mode.None);

            if (SkillIconModeView.CurrentMode == SkillModeType.Rotate && !allowRotate
                || SkillIconModeView.CurrentMode == SkillModeType.Assemble && !allowAssemble)
            {
                SkillIconModeView.ClearMode();
            }
        }
    }

    private void ClearSkillMode()
    {
        if (!clearBlockedMode)
            return;

        SkillIconModeView.ClearMode();
    }
}
