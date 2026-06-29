using UnityEngine;
using UnityEngine.Events;

// ==========================================
// 암호화 장치 부모 클래스
// 설명: 비밀번호 검증과 잠금 해제 상태를 관리하는 핵심 로직
// ==========================================
public class PasswordBase : MonoBehaviour, IPickable, IInteractive
{
    [Header("비밀번호 설정")]
    [Tooltip("이 장치를 열기 위한 정답 비밀번호를 적어주세요.")]
    [SerializeField] protected string _correctPassword; // 자식 클래스에서 접근 가능하도록 protected 사용

    [Header("Z 레이어 설정")]
    [SerializeField] protected int _setZLayer = 1;

    [Header("잠금 해제 이벤트")]
    public UnityEvent OnUnlockSuccess; 

    public bool IsLocked { get; protected set; } = true;

    protected virtual void Awake()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z - _setZLayer);
    }

    #region 📌 IPickable 인터페이스 구현
    public virtual bool TryUnlock(string input)
    {
        if (!IsLocked)
        {
            DevLog.Log("이미 잠금이 해제된 상태입니다.");
            return false;
        }

        if (input == _correctPassword)
        {
            IsLocked = false;
            DevLog.Log("딸깍- 올바른 암호/아이템입니다. 잠금이 해제되었습니다.");
            
            GameEvent.ESFXPlay?.Invoke("Safe_Open");
            OnUnlockSuccess?.Invoke(); 
            return true; 
        }
        else
        {
            DevLog.Log("잘못된 암호입니다. 다시 입력해주세요.");
            GameEvent.ESFXPlay?.Invoke("Safe_Failed");
            return false; 
        }
    }
    #endregion

    #region 📌 IInteractive 인터페이스 구현
    public virtual void Interact()
    {
        if (IsLocked)
        {
            DevLog.Log("잠겨있습니다. 비밀번호를 입력해야 합니다.");
        }
    }
    #endregion
}