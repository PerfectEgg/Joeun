using UnityEngine;
using UnityEngine.Events;
using TMPro;

// ==========================================
// 암호화 문 (예시: 금고) 클래스
// ==========================================
public class PasswordDoor : MonoBehaviour, IPickable, IInteractive
{
    [Header("비밀번호 설정")]
    [Tooltip("이 문을 열기 위한 정답 비밀번호를 적어주세요.")]
    [SerializeField] private string _currectPassword;

    [Header("연동할 UI")]
    [SerializeField] private TMP_InputField _passwordInputField;
    [SerializeField] private GameObject _keypadPanel;

    [Header("Z 레이어 설정")]
    [SerializeField] private int _setZLayer = 1;

    [Header("상호작용 성공 이벤트")]
    public UnityEvent OnInteractive; 

    public bool IsLocked { get; private set; } = true;

    private void Awake()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z - _setZLayer);

        if (_passwordInputField != null)
        {
            _passwordInputField.characterLimit = _currectPassword.Length;
            _passwordInputField.onEndEdit.AddListener(CheckPasswordOnEndEdit);
        }
    }

    private void Update()
    {
        if (!IsLocked || _passwordInputField == null) return;
        if (_keypadPanel == null || !_keypadPanel.activeInHierarchy) return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SubmitPassword();
        }
    }

    #region 📌 IPickable 인터페이스 구현 (통합 창구)
    // 인벤토리 아이템 드래그와 키패드 텍스트 검증을 모두 이 하나의 함수에서 처리합니다.
    public bool TryUnlock(string input)
    {
        if (!IsLocked)
        {
            DevLog.Log("이미 잠금이 해제된 상태입니다.");
            return false;
        }

        // 입력값이 정답 비밀번호 경우 해금 성공
        if (input == _currectPassword)
        {
            IsLocked = false;
            DevLog.Log("딸깍- 올바른 암호/아이템입니다. 잠금이 해제되었습니다.");

            if (_passwordInputField != null)
            {
                _passwordInputField.interactable = false;
            }

            OnInteractive?.Invoke(); // 애니메이션, 사운드 등 실행
            return true; // 성공 리턴 (아이템 소모 또는 로직 종료)
        }

        return false; // 실패 리턴
    }
    #endregion

    // 내부 비밀번호 제출 처리 로직
    private void SubmitPassword()
    {
        string currentInput = _passwordInputField.text;
        if (string.IsNullOrEmpty(currentInput)) return;

        // ★ 핵심: 별도의 함수 없이 인터페이스 함수인 TryUnlock을 그대로 재활용합니다.
        if (TryUnlock(currentInput))
        {
            DevLog.Log("비밀번호 정답 처리 완료.");
        }
        else
        {
            DevLog.Log("틀린 비밀번호입니다. 다시 시도하세요.");
            
            _passwordInputField.text = "";
            _passwordInputField.ActivateInputField();
        }
    }

    #region 📌 UI 버튼용 Public 함수들
    public void ClickNumberButton(string number)
    {
        if (!IsLocked || _passwordInputField == null) return;

        if (_passwordInputField.text.Length < _passwordInputField.characterLimit)
        {
            _passwordInputField.text += number;
        }
    }

    public void ClickDeleteButton()
    {
        if (!IsLocked || _passwordInputField == null) return;

        if (_passwordInputField.text.Length > 0)
        {
            _passwordInputField.text = _passwordInputField.text.Substring(0, _passwordInputField.text.Length - 1);
        }
    }

    public void ClickEnterButton()
    {
        SubmitPassword();
    }
    #endregion

    private void CheckPasswordOnEndEdit(string input)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SubmitPassword();
        }
    }

    public void Interact()
    {
        if (IsLocked && _passwordInputField != null)
        {
            _passwordInputField.ActivateInputField();
        }
    }
}