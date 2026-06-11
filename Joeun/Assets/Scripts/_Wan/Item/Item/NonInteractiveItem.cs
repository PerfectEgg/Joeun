using UnityEngine;
using UnityEngine.Events;

// ==========================================
// 상호작용 불가능한 아이템 추상 클래스
// ==========================================
public class NonInteractiveItem : MonoBehaviour, IInteractive, IHoverable
{
    [Header("특수 획득 이벤트")]
    [Tooltip("아이템을 주우려고 시도할 때 시행할 상호작용을 연결하세요.")]
    public UnityEvent OnItemInteractived;

    protected SpriteRenderer _spriteRenderer;
    protected int _originSortingOrder;
    protected Vector3 _originScale;    

    protected virtual void Awake()
    {
        _originScale = transform.localScale;

        // SpriteRenderer 캐싱
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    #region IInteractive 구현
    public virtual void Interact()
    {
        // 아이템을 주우려고 시도할 때 실행할 이벤트 호출
        OnItemInteractived?.Invoke();

        gameObject.SetActive(false); // 아이템이 사라지는 효과 (예: 주워지는 애니메이션이 없으므로 임시로 비활성화 처리)
    }
    #endregion

    #region IHoverable 구현
    public virtual void OnHoverEnter()
    {
        transform.localScale = transform.localScale * 1.2f;
    }

    public virtual void OnHoverExit()
    {
        transform.localScale = _originScale;
    }
    #endregion
}