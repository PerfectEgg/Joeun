using UnityEngine;

public class PatternPuzzleSkillOnOpen : MonoBehaviour
{
    [SerializeField] private SkillModeType skillMode = SkillModeType.Rotate;
    [SerializeField] private bool grantSkill = true;
    [SerializeField] private bool selectSkill = true;
    [SerializeField] private bool clearModeOnClose;

    private void OnEnable()
    {
        GrantAndSelect();
    }

    private void OnDisable()
    {
        if (clearModeOnClose && SkillIconModeView.CurrentMode == skillMode)
            SkillIconModeView.ClearMode();
    }

    public void GrantAndSelect()
    {
        if (grantSkill)
            SkillModeStageRules.Grant(skillMode);

        if (selectSkill)
            SkillIconModeView.SelectMode(skillMode);
    }

    public void GrantRotateAndSelect()
    {
        skillMode = SkillModeType.Rotate;
        GrantAndSelect();
    }

    public void GrantAssembleAndSelect()
    {
        skillMode = SkillModeType.Assemble;
        GrantAndSelect();
    }

    public void GrantDecodeAndSelect()
    {
        skillMode = SkillModeType.Decode;
        GrantAndSelect();
    }
}
