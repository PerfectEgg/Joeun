using UnityEngine;
using UnityEngine.Events; 

// ==========================================
// 상호작용 가능한 버튼 클래스
// ==========================================
public class InteractiveButton : MonoBehaviour, IInteractive, IConditionRequirable
{
    [Header("버튼 설정")]
    [Tooltip("한 번만 눌리는 버튼인지, 여러 번 누를 수 있는 토글형인지 설정")]
    [SerializeField] private bool _isOneShot = true;

    // 한 번 눌렸는지 추적하는 변수
    private bool _hasBeenPressed = false;
    private Collider2D[] _colliders;

    [Header("Z 레이어 설정")]
    [Tooltip("열쇠의 물리 충돌을 위해 Z 레이어를 낮춰서 화면 앞으로 나타내는 설정값입니다.")]
    [SerializeField] private int _setZLayer = 1; // Z레이어를 낮춰서 플레이어 뒤에 숨기기 위한 설정값

    [Header("조건 설정")]
    [Tooltip("잠금 해제 조건이 필요한지 여부 (예: 선행 오브젝트 처리)")]
    [SerializeField] private int _conditionCount = 0;

    [Header("상호작용 이벤트")]
    public UnityEvent OnPressed; // 버튼이 눌렸을 때 실행할 이벤트 (예: 애니메이션, 사운드 등)

    void Awake()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z - _setZLayer);
        _colliders = GetComponents<Collider2D>();
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

    #region IInteractive 구현
    public void Interact()
    {
        // 1. 선행 조건 검사
        if (_conditionCount > 0)
        {
            DevLog.Log("조건이 더 필요합니다.");
            return;
        }

        // 2. 일회용 버튼 중복 클릭 방지
        if (_isOneShot && _hasBeenPressed)
        {
            DevLog.Log("이미 눌린 버튼입니다.");
            return;
        }

        // 3. 버튼 작동!
        _hasBeenPressed = true;
        if (_isOneShot)
            SetCollidersEnabled(false);
        DevLog.Log($"딸깍- {gameObject.name} 작동!");
        
        OnPressed?.Invoke();
    }
    #endregion
    private void SetCollidersEnabled(bool enabled)
    {
        if (_colliders == null)
            return;

        foreach (Collider2D itemCollider in _colliders)
        {
            if (itemCollider != null)
                itemCollider.enabled = enabled;
        }
    }
}
