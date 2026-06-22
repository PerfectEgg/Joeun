using UnityEngine;

public class PuzzleModeLock : MonoBehaviour
{
    [SerializeField] private PuzzleModeManager modeManager;
    [SerializeField] private bool allowRotate = true;
    [SerializeField] private bool allowAssemble;
    [SerializeField] private bool clearBlockedMode = true;
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
        SkillIconModeView.OnSkillModeChanged += HandleSkillModeChanged;
        ApplyLock();
    }

    private void OnDisable()
    {
        SkillIconModeView.OnSkillModeChanged -= HandleSkillModeChanged;
    }

    private void LateUpdate()
    {
        ApplyLock();
    }

    public void Configure(bool rotateAllowed, bool assembleAllowed, bool clearBlocked = true)
    {
        allowRotate = rotateAllowed;
        allowAssemble = assembleAllowed;
        clearBlockedMode = clearBlocked;
        ApplyLock();
    }

    private void HandleSkillModeChanged(SkillModeType mode)
    {
        ApplyLock();
    }

    private void ApplyLock()
    {
        ResolveModeManager();

        if (modeManager == null)
            return;

        if (allowRotate)
            modeManager.rotateKey = rotateKeyWhenAllowed;
        else
            modeManager.rotateKey = KeyCode.None;

        if (allowAssemble)
            modeManager.assembleKey = assembleKeyWhenAllowed;
        else
            modeManager.assembleKey = KeyCode.None;

        if (!allowRotate && modeManager.IsRotate)
            ClearMode();

        if (!allowAssemble && modeManager.IsAssemble)
            ClearMode();
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

    private void ClearMode()
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
}
