using UnityEngine;

public class PuzzleModeLock : MonoBehaviour
{
    [SerializeField] private PuzzleModeManager modeManager;
    [SerializeField] private bool allowRotate = true;
    [SerializeField] private bool allowAssemble;
    [SerializeField] private bool clearBlockedMode = true;

    private void Reset()
    {
        modeManager = GetComponent<PuzzleModeManager>();
    }

    private void Awake()
    {
        if (modeManager == null)
            modeManager = GetComponent<PuzzleModeManager>();

        DisableBlockedKeys();
    }

    private void LateUpdate()
    {
        if (modeManager == null)
            return;

        if (!allowRotate && modeManager.IsRotate)
            ClearMode();

        if (!allowAssemble && modeManager.IsAssemble)
            ClearMode();
    }

    private void DisableBlockedKeys()
    {
        if (modeManager == null)
            return;

        if (!allowRotate)
            modeManager.rotateKey = KeyCode.None;

        if (!allowAssemble)
            modeManager.assembleKey = KeyCode.None;
    }

    private void ClearMode()
    {
        if (clearBlockedMode)
            modeManager.SetMode(PuzzleModeManager.Mode.None);
    }
}
