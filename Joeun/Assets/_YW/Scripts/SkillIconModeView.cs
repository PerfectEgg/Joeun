using UnityEngine;
using UnityEngine.UI;
using System;

[RequireComponent(typeof(Image))]
public class SkillIconModeView : MonoBehaviour
{
    public static SkillModeType CurrentMode { get; private set; } = SkillModeType.None;
    public static event Action<SkillModeType> OnSkillModeChanged;
    private static SkillIconModeView activeView;
    private static bool waitForSkillKeysReleased;

    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Sprite rotateSprite;
    [SerializeField] private Sprite assembleSprite;
    [SerializeField] private Sprite decodeSprite;

    private Image targetImage;
    private PuzzleModeManager boundManager;

    private void Awake()
    {
        targetImage = GetComponent<Image>();
    }

    private void Reset()
    {
        targetImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        activeView = this;
        SkillInteractionLock.OnChanged += HandleInteractionLockChanged;
        BindIfNeeded();
        ApplyMode();
    }

    private void OnDisable()
    {
        SkillInteractionLock.OnChanged -= HandleInteractionLockChanged;

        if (boundManager != null)
            boundManager.SetMode(PuzzleModeManager.Mode.None);

        SetMode(SkillModeType.None);

        if (activeView == this)
            activeView = null;
    }

    public static void SelectMode(SkillModeType mode)
    {
        waitForSkillKeysReleased = true;

        if (mode != SkillModeType.None && SkillInteractionLock.IsLocked)
        {
            mode = SkillModeType.None;
        }

        if (activeView != null)
        {
            activeView.BindIfNeeded();
            activeView.SetMode(mode);
            return;
        }

        if (CurrentMode == mode)
            return;

        CurrentMode = mode;
        OnSkillModeChanged?.Invoke(CurrentMode);
    }

    public static void ClearMode()
    {
        SelectMode(SkillModeType.None);
    }

    private void Update()
    {
        BindIfNeeded();

        if (SkillInteractionLock.IsLocked)
        {
            if (CurrentMode != SkillModeType.None)
                SetMode(SkillModeType.None);

            return;
        }

        if (waitForSkillKeysReleased)
        {
            if (IsAnySkillKeyHeld())
                return;

            waitForSkillKeysReleased = false;
        }

        if (Input.GetKeyDown(KeyCode.Q))
            ToggleMode(SkillModeType.Rotate);

        if (Input.GetKeyDown(KeyCode.W))
            ToggleMode(SkillModeType.Assemble);

        if (Input.GetKeyDown(KeyCode.E))
            ToggleMode(SkillModeType.Decode);

        if (!IsAllowed(CurrentMode))
            SetMode(SkillModeType.None);
    }

    private void BindIfNeeded()
    {
        PuzzleModeManager current = PuzzleModeManager.Instance;
        if (boundManager == current)
            return;

        boundManager = current;
        DisableBuiltInPuzzleKeys();
        ApplyModeToPuzzleManager();
    }

    private void DisableBuiltInPuzzleKeys()
    {
        if (boundManager == null)
            return;

        boundManager.rotateKey = KeyCode.None;
        boundManager.assembleKey = KeyCode.None;
    }

    private void ToggleMode(SkillModeType mode)
    {
        if (SkillInteractionLock.IsLocked)
            return;

        if (!IsAllowed(mode))
            return;

        GameEvent.ESFXPlay?.Invoke("Skill_Select");

        SetMode(CurrentMode == mode ? SkillModeType.None : mode);
    }

    private static bool IsAnySkillKeyHeld()
    {
        return Input.GetKey(KeyCode.Q)
            || Input.GetKey(KeyCode.W)
            || Input.GetKey(KeyCode.E);
    }

    private void SetMode(SkillModeType mode)
    {
        if (mode != SkillModeType.None && SkillInteractionLock.IsLocked)
            mode = SkillModeType.None;

        if (!IsAllowed(mode))
            mode = SkillModeType.None;

        if (CurrentMode == mode)
        {
            ApplyMode();
            return;
        }

        CurrentMode = mode;
        ApplyMode();
        OnSkillModeChanged?.Invoke(CurrentMode);
    }

    private void ApplyMode()
    {
        switch (CurrentMode)
        {
            case SkillModeType.Rotate:
                SetSprite(rotateSprite);
                break;
            case SkillModeType.Assemble:
                SetSprite(assembleSprite);
                break;
            case SkillModeType.Decode:
                SetSprite(decodeSprite);
                break;
            default:
                SetSprite(emptySprite);
                break;
        }

        ApplyModeToPuzzleManager();
    }

    private void ApplyModeToPuzzleManager()
    {
        if (boundManager == null)
            return;

        DisableBuiltInPuzzleKeys();

        switch (CurrentMode)
        {
            case SkillModeType.Rotate:
                boundManager.SetMode(PuzzleModeManager.Mode.Rotate);
                break;
            case SkillModeType.Assemble:
                boundManager.SetMode(PuzzleModeManager.Mode.Assemble);
                break;
            default:
                boundManager.SetMode(PuzzleModeManager.Mode.None);
                break;
        }
    }

    private bool IsAllowed(SkillModeType mode)
    {
        if (mode != SkillModeType.None && SkillInteractionLock.IsLocked)
            return false;

        return SkillModeStageRules.IsAllowed(mode) && PuzzleModeLock.IsAllowedByActiveLocks(mode);
    }

    private void HandleInteractionLockChanged(bool locked)
    {
        if (locked)
            SetMode(SkillModeType.None);
        else
            ApplyMode();
    }

    private void SetSprite(Sprite sprite)
    {
        if (targetImage == null)
            return;

        targetImage.sprite = sprite;
        targetImage.enabled = sprite != null;
    }
}
