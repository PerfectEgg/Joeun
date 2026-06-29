using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CoolantPuzzleViewBinder : MonoBehaviour
{
    private const int SelectRetryFrames = 30;
    private const string OpenSkillSfxId = "Skill_Select";

    [SerializeField] private GameObject puzzleUiRoot;
    [SerializeField] private CoolantPuzzleManager puzzleManager;
    [SerializeField] private bool hideUiOnAwake = true;
    [SerializeField] private bool resetPuzzleOnOpen;
    [SerializeField] private bool resetPuzzleOnClose;
    [SerializeField] private bool makeNonInteractiveGraphicsPassThrough = true;

    [Header("Skill Mode")]
    [SerializeField] private bool selectRotateOnOpen = true;
    [SerializeField] private bool clearRotateOnClose = true;
    [SerializeField] private bool playOpenSkillSfxOnce = true;

    private bool openedWithRotate;
    private bool pendingRotateSelect;
    private bool playedOpenSkillSfx;
    private int pendingRotateSelectFrames;

    private void Reset()
    {
        AutoWire();
    }

    private void Awake()
    {
        AutoWire();

        if (hideUiOnAwake)
            SetPuzzleUiActive(false);
    }

    private void OnEnable()
    {
        AutoWire();

        if (resetPuzzleOnOpen && puzzleManager != null)
            puzzleManager.ResetPuzzle();

        SetPuzzleUiActive(true);
        OpenRotateMode();
    }

    private void OnDisable()
    {
        if (resetPuzzleOnClose && puzzleManager != null)
            puzzleManager.ResetPuzzle();

        CloseRotateMode();
        SetPuzzleUiActive(false);
    }

    private void LateUpdate()
    {
        if (!pendingRotateSelect)
            return;

        TrySelectRotateMode();
    }

    public void SetPuzzleUiRoot(GameObject root)
    {
        puzzleUiRoot = root;
        AutoWire();
    }

    public void OpenPuzzleUi()
    {
        if (resetPuzzleOnOpen && puzzleManager != null)
            puzzleManager.ResetPuzzle();

        SetPuzzleUiActive(true);
        OpenRotateMode();
    }

    public void ClosePuzzleUi()
    {
        if (resetPuzzleOnClose && puzzleManager != null)
            puzzleManager.ResetPuzzle();

        CloseRotateMode();
        SetPuzzleUiActive(false);
    }

    private void AutoWire()
    {
        if (puzzleManager != null)
            return;

        if (puzzleUiRoot != null)
            puzzleManager = puzzleUiRoot.GetComponentInChildren<CoolantPuzzleManager>(true);
    }

    private void SetPuzzleUiActive(bool active)
    {
        if (puzzleUiRoot != null && puzzleUiRoot.activeSelf != active)
            puzzleUiRoot.SetActive(active);

        if (active && makeNonInteractiveGraphicsPassThrough)
            ConfigurePuzzleUiRaycasts();
    }

    private void OpenRotateMode()
    {
        if (!selectRotateOnOpen)
            return;

        openedWithRotate = true;
        pendingRotateSelect = true;
        pendingRotateSelectFrames = SelectRetryFrames;
        TrySelectRotateMode();
    }

    private void CloseRotateMode()
    {
        pendingRotateSelect = false;

        if (clearRotateOnClose && openedWithRotate && SkillIconModeView.CurrentMode == SkillModeType.Rotate)
            SkillIconModeView.ClearMode();

        openedWithRotate = false;
    }

    private void TrySelectRotateMode()
    {
        SkillModeStageRules.Grant(SkillModeType.Rotate);
        SkillIconModeView.SelectMode(SkillModeType.Rotate);

        if (SkillIconModeView.CurrentMode == SkillModeType.Rotate)
        {
            PlayOpenSkillSfxOnce();
            pendingRotateSelect = false;
            return;
        }

        pendingRotateSelectFrames--;
        if (pendingRotateSelectFrames > 0)
            return;

        Debug.LogWarning(
            $"[CoolantPuzzle] Failed to auto-select Rotate. " +
            $"current={SkillIconModeView.CurrentMode}, " +
            $"stageAllowed={SkillModeStageRules.IsAllowed(SkillModeType.Rotate)}, " +
            $"lockAllowed={PuzzleModeLock.IsAllowedByActiveLocks(SkillModeType.Rotate)}, " +
            $"interactionLocked={SkillInteractionLock.IsLocked}",
            this);
        pendingRotateSelect = false;
    }

    private void PlayOpenSkillSfxOnce()
    {
        if (!playOpenSkillSfxOnce || playedOpenSkillSfx)
            return;

        playedOpenSkillSfx = true;
        GameEvent.ESFXPlay?.Invoke(OpenSkillSfxId);
    }

    private void ConfigurePuzzleUiRaycasts()
    {
        if (puzzleUiRoot == null)
            return;

        Graphic[] graphics = puzzleUiRoot.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            if (graphic == null)
                continue;

            graphic.raycastTarget = HasInteractiveHandlerInParents(graphic.transform);
        }
    }

    private bool HasInteractiveHandlerInParents(Transform start)
    {
        Transform root = puzzleUiRoot != null ? puzzleUiRoot.transform : null;
        Transform current = start;

        while (current != null)
        {
            if (HasInteractiveHandler(current))
                return true;

            if (current == root)
                break;

            current = current.parent;
        }

        return false;
    }

    private static bool HasInteractiveHandler(Transform target)
    {
        if (target == null)
            return false;

        if (target.GetComponent<Selectable>() != null)
            return true;

        if (target.GetComponent<EventTrigger>() != null)
            return true;

        MonoBehaviour[] behaviours = target.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IPointerClickHandler
                || behaviour is IPointerDownHandler
                || behaviour is IPointerUpHandler
                || behaviour is IBeginDragHandler
                || behaviour is IDragHandler
                || behaviour is IEndDragHandler
                || behaviour is IPointerEnterHandler
                || behaviour is IPointerExitHandler)
            {
                return true;
            }
        }

        return false;
    }
}
