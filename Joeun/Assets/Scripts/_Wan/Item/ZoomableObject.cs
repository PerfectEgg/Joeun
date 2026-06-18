using UnityEngine;

// ==========================================
// 확대 오브젝트 클래스
// ==========================================
public class ZoomableObject : MonoBehaviour, IInteractive, IConditionRequirable
{
    [Tooltip("이 오브젝트를 클릭했을 때 띄워줄 확대 뷰의 인덱스")]
    [SerializeField] private int zoomViewIndex;

    [Header("조건 설정")]
    [Tooltip("잠금 해제 조건이 필요한지 여부 (예: 선행 오브젝트 처리)")]
    [SerializeField] private int _conditionCount = 0;

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
        // 선행 조건 검사
        if (_conditionCount > 0)
        {
            DevLog.Log("조건이 더 필요합니다.");
            return;
        }

        // 이벤트 버스를 통해 인덱스 번호를 전달하며 신호 발생
        GameEvent.EOnCCTVZoomInView?.Invoke(zoomViewIndex);
    }
    #endregion
    
}