using UnityEngine;
using UnityEngine.Events; 

// ==========================================
// 상호작용 가능한 대상 클래스
// ==========================================
public class InteractiveTarget : MonoBehaviour, IPickable, IInteractive, IConditionRequirable
{
    [Header("잠금 설정")]
    [SerializeField] private string _requiredKeyID; // 예: "CardKey_Lv1"
    [SerializeField] private bool _isLocked = true; // 잠겨있는 지에 대한 여부

    [Header("카드/열쇠 여부")]
    [Tooltip("여부에 따른 사운드 변화")]
    [SerializeField] private bool _isPlaySound = false;
    [SerializeField] private bool _isCard = true;

    public bool IsCard { get; private set; } = true;

    [Header("Z 레이어 설정")]
    [Tooltip("열쇠의 물리 충돌을 위해 Z 레이어를 낮춰서 화면 앞으로 나타내는 설정값입니다.")]
    [SerializeField] private int _setZLayer = 1; // Z레이어를 낮춰서 플레이어 뒤에 숨기기 위한 설정값

    [Header("조건 설정")]
    [Tooltip("잠금 해제 조건이 필요한지 여부 (예: 선행 오브젝트 처리)")]
    [SerializeField] private int _conditionCount = 0;

    [Header("상호작용 이벤트")]
    public UnityEvent OnInteractive; // 문이 열릴 때 실행할 이벤트 (예: 애니메이션, 사운드 등)


    public bool IsLocked { get; set; } = true;

    void Awake()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z - _setZLayer);
    }

    void Start()
    {
        IsLocked = _isLocked;
        IsCard = _isCard;
    }

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
        if (!IsLocked)
        {
            DevLog.Log("이미 잠금이 해제된 장치입니다.");
            return false;
        }

        // 핵심: 선행 조건이 필요한데 아직 충족되지 않았다면 아이템 사용 거부
        if (_conditionCount > 0)
        {
            DevLog.Log("아직은 상호작용 할 수 없다. 무언가 먼저 해결해야 할 것 같다.");
            return false; // 실패 처리 (아이템 소모 안 됨)
        }

        if (keyId == _requiredKeyID)
        {
            IsLocked = false;
            DevLog.Log("삐빅- 인증되었습니다.");

            if(_isPlaySound)
            {
                if (IsCard) GameEvent.ESFXPlay?.Invoke("Reader_Failed");
                else GameEvent.ESFXPlay?.Invoke("Door_Locked");
            }
            
            OnInteractive?.Invoke(); // 상호작용 했을 때 실행할 이벤트 호출
            
            return true; // 성공했으니 아이템(카드키) 소모
        }
        else
        {
            if(_isPlaySound)
            {
                if (IsCard) GameEvent.ESFXPlay?.Invoke("Reader_Failed");
                else GameEvent.ESFXPlay?.Invoke("Door_Locked");
            }

            DevLog.Log("올바른 상호작용이 아닙니다.");
            return false; // 실패
        }
    }
    #endregion

    

    #region IInteractive 구현
    public void Interact()
    {
        if (!IsLocked)
        {
            DevLog.Log("이미 잠금이 해제된 장치입니다.");
            return;
        }

        if(_isPlaySound)
        {
            if (IsCard) GameEvent.ESFXPlay?.Invoke("Reader_Failed");
            else GameEvent.ESFXPlay?.Invoke("Door_Locked");
        }

        DevLog.Log("잠겨있다.");
    }
    #endregion
}