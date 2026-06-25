using UnityEngine;

[DisallowMultipleComponent]
public sealed class CoolantPuzzleViewBinder : MonoBehaviour
{
    [SerializeField] private GameObject puzzleUiRoot;
    [SerializeField] private CoolantPuzzleManager puzzleManager;
    [SerializeField] private bool hideUiOnAwake = true;
    [SerializeField] private bool resetPuzzleOnOpen;
    [SerializeField] private bool resetPuzzleOnClose;

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
    }
}
