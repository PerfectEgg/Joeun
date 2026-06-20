using System;
using UnityEngine;

/// <summary>
/// 두 퍼즐이 공유하는 입력 모드 매니저입니다.
///  - Q : Rotate 모드 토글
///  - W : Assemble 모드 토글
/// 같은 키를 다시 누르면 None으로 해제, 다른 키를 누르면 모드가 전환됩니다.
/// 씬에 하나만 두세요 (빈 GameObject에 부착).
/// </summary>
public class PuzzleModeManager : MonoBehaviour
{
    public enum Mode { None, Rotate, Assemble }

    public static PuzzleModeManager Instance { get; private set; }

    [Header("키 설정")]
    public KeyCode rotateKey   = KeyCode.Q;
    public KeyCode assembleKey = KeyCode.W;

    public Mode CurrentMode { get; private set; } = Mode.None;

    /// <summary>모드가 바뀔 때 호출 (UI 표시 갱신 등에 활용)</summary>
    public event Action<Mode> OnModeChanged;

    public bool IsRotate   => CurrentMode == Mode.Rotate;
    public bool IsAssemble => CurrentMode == Mode.Assemble;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (SkillInteractionLock.IsLocked)
        {
            if (CurrentMode != Mode.None)
                SetMode(Mode.None);

            return;
        }

        if (Input.GetKeyDown(rotateKey))   Toggle(Mode.Rotate);
        if (Input.GetKeyDown(assembleKey)) Toggle(Mode.Assemble);
    }

    void Toggle(Mode m)
    {
        if (SkillInteractionLock.IsLocked)
            return;

        if (!IsModeAllowed(m))
            return;

        // 같은 모드면 해제(None), 아니면 그 모드로 전환
        CurrentMode = (CurrentMode == m) ? Mode.None : m;
        Debug.Log($"[ModeManager] 현재 모드 → {CurrentMode}");
        OnModeChanged?.Invoke(CurrentMode);
    }

    /// <summary>UI 버튼에서 직접 모드를 켜고 싶을 때 (선택)</summary>
    public void SetMode(Mode m)
    {
        if (m != Mode.None && SkillInteractionLock.IsLocked)
            m = Mode.None;

        if (!IsModeAllowed(m))
            m = Mode.None;

        CurrentMode = m;
        OnModeChanged?.Invoke(CurrentMode);
    }

    bool IsModeAllowed(Mode mode)
    {
        switch (mode)
        {
            case Mode.Rotate:
                return SkillModeStageRules.IsAllowed(SkillModeType.Rotate);
            case Mode.Assemble:
                return SkillModeStageRules.IsAllowed(SkillModeType.Assemble);
            default:
                return true;
        }
    }
}
