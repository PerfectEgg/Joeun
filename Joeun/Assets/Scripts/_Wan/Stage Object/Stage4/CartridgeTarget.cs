using UnityEngine;
using UnityEngine.Events; 

// ==========================================
// 카트리지 클래스
// ==========================================
public class CartridgeTarget : MonoBehaviour, IPickable, IInteractive, IConditionRequirable
{
    [Header("잠금 설정")]
    [SerializeField] private string _requiredKeyID; // 예: "CardKey_Lv1"

    [Header("Z 레이어 설정")]
    [Tooltip("열쇠의 물리 충돌을 위해 Z 레이어를 낮춰서 화면 앞으로 나타내는 설정값입니다.")]
    [SerializeField] private int _setZLayer = 1; // Z레이어를 낮춰서 플레이어 뒤에 숨기기 위한 설정값

    [Header("조건 설정")]
    [Tooltip("잠금 해제 조건이 필요한지 여부 (예: 선행 오브젝트 처리)")]
    [SerializeField] private int _conditionCount = 0;

    [Header("상호작용 이벤트")]
    public UnityEvent OnInteractive; // 문이 열릴 때 실행할 이벤트 (예: 애니메이션, 사운드 등)

    public bool IsLocked { get; set; } = false;

    void Awake()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z - _setZLayer);
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
        if (_conditionCount > 0)
        {
            DevLog.Log("아직은 상호작용 할 수 없다. 무언가 먼저 해결해야 할 것 같다.");
            return false;
        }

        if (keyId == _requiredKeyID)
        {
            DevLog.Log("삐빅- 인증되었습니다.");
            
            OnInteractive?.Invoke(); // 상호작용 했을 때 실행할 이벤트 호출
            
            return false;
        }
        else
        {
            DevLog.Log("올바른 상호작용이 아닙니다.");
            return false;
        }

        // 모두 return false;로 아이템 소모 X
    }
    #endregion

    

    #region IInteractive 구현
    public void Interact()
    {
        DevLog.Log("카트리지를 넣으세요");
    }
    #endregion
}