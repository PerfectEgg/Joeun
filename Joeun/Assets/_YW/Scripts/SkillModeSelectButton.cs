using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class SkillModeSelectButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private SkillModeType mode = SkillModeType.None;
    [SerializeField] private bool ignoreUnavailableMode = true;
    [SerializeField] private bool allowToggleOff;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        Select();
    }

    private void OnMouseDown()
    {
        Select();
    }

    public void Select()
    {
        if (SkillInteractionLock.IsLocked)
            return;

        if (ignoreUnavailableMode && mode != SkillModeType.None && !IsSelectable(mode))
            return;

        if (allowToggleOff && SkillIconModeView.CurrentMode == mode)
            SkillIconModeView.ClearMode();
        else
            SkillIconModeView.SelectMode(mode);
    }

    private static bool IsSelectable(SkillModeType targetMode)
    {
        return SkillModeStageRules.IsAllowed(targetMode)
            && PuzzleModeLock.IsAllowedByActiveLocks(targetMode);
    }

    public void SelectNone()
    {
        mode = SkillModeType.None;
        Select();
    }

    public void SelectRotate()
    {
        mode = SkillModeType.Rotate;
        Select();
    }

    public void SelectAssemble()
    {
        mode = SkillModeType.Assemble;
        Select();
    }

    public void SelectDecode()
    {
        mode = SkillModeType.Decode;
        Select();
    }
}
