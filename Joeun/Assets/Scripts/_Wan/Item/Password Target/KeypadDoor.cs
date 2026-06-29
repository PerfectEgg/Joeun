using UnityEngine;
using UnityEngine.Events;
using TMPro;

// ==========================================
// 12버튼 키패드 클래스 (PasswordBase 상속)
// 설명: 0~9, 정정, 확인 버튼 입력을 처리하여 부모에게 검증을 요청합니다.
// ==========================================
public class KeypadDoor : PasswordBase
{
    [Header("UI 화면 (선택 사항)")]
    [Tooltip("입력한 숫자를 보여줄 TextMeshPro 텍스트를 연결하세요.")]
    [SerializeField] private TextMeshProUGUI _displayText;

    private string _currentInput = "";

    protected override void Awake()
    {
        base.Awake(); // 부모의 Awake(Z축 설정 등) 실행
        UpdateDisplay();
    }

    #region 📌 12버튼 조작용 Public 함수들

    public void PressNumber(string number)
    {
        if (!IsLocked) return;

        // 부모의 _correctPassword에 접근하여 길이 제한 체크
        if (_currentInput.Length < _correctPassword.Length)
        {
            _currentInput += number;
            GameEvent.ESFXPlay?.Invoke("Safe_Tried");
            UpdateDisplay();
        }
        else
        {
            GameEvent.ESFXPlay?.Invoke("Safe_Overflow");
        }
    }

    public void PressBackspace()
    {
        if (!IsLocked) return;

        if (_currentInput.Length > 0)
        {
            _currentInput = _currentInput.Substring(0, _currentInput.Length - 1);
            GameEvent.ESFXPlay?.Invoke("Safe_Tried");
            UpdateDisplay();
        }
        else
        {
            GameEvent.ESFXPlay?.Invoke("Safe_Overflow");
        }
    }

    public void PressEnter()
    {
        if (!IsLocked || string.IsNullOrEmpty(_currentInput)) return;

        // ★ 핵심: 부모의 TryUnlock을 호출하여 정답 검증 요청
        if (TryUnlock(_currentInput))
        {
            // 성공 (부모 클래스에서 IsLocked = false 및 OnUnlockSuccess 처리됨)
            DevLog.Log("비밀번호 정답 처리 완료.");
        }
        else
        {
            // 실패 (입력값 초기화)
            DevLog.Log("틀린 비밀번호입니다.");
            _currentInput = ""; 
            UpdateDisplay();
        }
    }
    #endregion

    private void UpdateDisplay()
    {
        if (_displayText != null)
        {
            _displayText.text = _currentInput; 
        }
    }

    public override void Interact()
    {
        base.Interact();
        if (IsLocked)
        {
            DevLog.Log("키패드가 달려있습니다. 버튼을 직접 눌러보세요.");
        }
    }
}