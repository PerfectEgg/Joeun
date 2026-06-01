using UnityEngine;

// ==========================================
// 상호작용 가능한 아이템 추상 클래스
// ==========================================
public abstract class InteractiveItem : MonoBehaviour, IInteractive, ICollectible, IHoverable
{
    // 에디터에서 해당 아이템에 맞는 ScriptableObject 할당
    [Header("아이템 원본 데이터")]
    public ItemData _itemData; // ScriptableObject 연결용

    public bool IsAcquired { get; protected set; }

    protected SpriteRenderer _spriteRenderer;
    protected int _originSortingOrder;
    protected Vector3 _originScale;

    protected virtual void Awake()
    {
        IsAcquired = false;

        _originScale = transform.localScale;

        // SpriteRenderer 캐싱
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    #region IInteractive 구현
    public virtual void Interact()
    {
        // 기본 상호작용 로직 (예: 아이템 설명 텍스트 출력 등)
    }
    #endregion

    #region ICollectible 구현
    public virtual void Collect()
    {
        if (IsAcquired) return;
        OnAcquire(); // 획득 상태로 전환

        // 1. 이벤트 버스에 "나 획득됐어!" 하고 데이터 방송
        GameEvent.EOnItemCollected?.Invoke(_itemData);
        
        // 2. 월드에서 내 자신을 숨김 (파괴하지 않음)
        gameObject.SetActive(false);
    }

    public virtual void OnAcquire()
    {
        IsAcquired = true;
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