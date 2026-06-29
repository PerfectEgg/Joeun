using UnityEngine;
using UnityEngine.Events;

// ==========================================
// 키 인식 오브젝트 클래스
// ==========================================
public class KeyReader : MonoBehaviour, IInteractive, IPickable, IConditionRequirable
{
    [Header("잠금 설정")]
    [SerializeField] private string _requiredKeyID; // 예: "CardKey_Lv1"
    
    [Header("작동할 대상")]
    [Tooltip("이 장치가 해금되었을 때 열릴 문을 연결하세요.")]
    [SerializeField] private Door _targetDoor;

    [Header("카드/열쇠 여부")]
    [Tooltip("여부에 따른 사운드 변화")]
    [SerializeField] private bool _isCard = true;

    public bool IsCard { get; private set; } = true;

    [Header("Z 레이어 설정")]
    [Tooltip("열쇠의 물리 충돌을 위해 Z 레이어를 낮춰서 화면 앞으로 나타내는 설정값입니다.")]
    [SerializeField] private int _setZLayer = 1; // Z레이어를 낮춰서 플레이어 뒤에 숨기기 위한 설정값

    [Header("상호작용 이벤트")]
    public UnityEvent OnInteractive; // 문이 열릴 때 실행할 이벤트 (예: 애니메이션, 사운드 등)

    [Header("조건 설정")]
    [Tooltip("잠금 해제 조건이 필요한지 여부 (예: 선행 오브젝트 처리)")]
    [SerializeField] private int _conditionCount = 0;

    public bool IsLocked { get; private set; } = true;

    void Awake()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z - _setZLayer);
    }

    void Start()
    {
        IsCard = _isCard;
    }

    #region IInteractive 구현
    public void Interact()
    {
        if (!IsLocked)
        {
            DevLog.Log("이미 잠금이 해제된 장치입니다.");
            return;
        }

        if (IsCard) GameEvent.ESFXPlay?.Invoke("Reader_Failed");
        else GameEvent.ESFXPlay?.Invoke("Door_Locked");
        DevLog.Log("잠겨있다.");
    }
    #endregion

    #region IConditionRequirable 구현
    // 외부의 다른 스위치나 퍼즐이 풀렸을 때 UnityEvent를 통해 호출될 함수입니다.
    public void ResolveCondition()
    {
        _conditionCount--;
        if (_conditionCount <= 0)
        {
            DevLog.Log($"{gameObject.name}의 선행 조건이 달성되었습니다! 이제 아이템을 사용할 수 있습니다.");
        }
    }
    #endregion

    #region IPickable 구현
    public bool TryUnlock(string keyId)
    {
        // 1. 선행 조건 검사
        if (_conditionCount > 0)
        {
            DevLog.Log("조건이 더 필요합니다.");
            return false;
        }

        if (!IsLocked)
        {
            DevLog.Log("이미 잠금이 해제된 장치입니다.");
            return false;
        }

        if (keyId == _requiredKeyID)
        {
            IsLocked = false;
            DevLog.Log("삐빅- 인증되었습니다.");

            if (IsCard) GameEvent.ESFXPlay?.Invoke("Reader_Success");
            else GameEvent.ESFXPlay?.Invoke("Door_Unlocked");

            // 중앙 버스에 문이 열렸음을 방송 (업적, 효과음, 퍼즐 연동용)
            OnInteractive?.Invoke();
            
            // 핵심: 내 판정이 성공했으니, 연결된 문에게 잠금을 풀라고 명령!
            _targetDoor.UnlockFromExternal();
            
            return true; // 성공했으니 아이템(카드키) 소모
        }
        else
        {
            if (IsCard) GameEvent.ESFXPlay?.Invoke("Reader_Failed");
            else GameEvent.ESFXPlay?.Invoke("Door_Locked");
            DevLog.Log("접근 권한이 없는 카드키입니다.");
            return false; // 실패
        }
    }
    #endregion
}