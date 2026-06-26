using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CoolantPuzzleViewBinder : MonoBehaviour
{
    [SerializeField] private GameObject puzzleUiRoot;
    [SerializeField] private CoolantPuzzleManager puzzleManager;
    [SerializeField] private bool hideUiOnAwake = true;
    [SerializeField] private bool resetPuzzleOnOpen;
    [SerializeField] private bool resetPuzzleOnClose;
    [SerializeField] private bool makeNonInteractiveGraphicsPassThrough = true;

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
    }

    private void OnDisable()
    {
        if (resetPuzzleOnClose && puzzleManager != null)
            puzzleManager.ResetPuzzle();

        SetPuzzleUiActive(false);
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
    }

    public void ClosePuzzleUi()
    {
        if (resetPuzzleOnClose && puzzleManager != null)
            puzzleManager.ResetPuzzle();

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
