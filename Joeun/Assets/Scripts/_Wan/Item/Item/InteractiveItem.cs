using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// ==========================================
// 상호작용 가능한 아이템 추상 클래스
// ==========================================
public abstract class InteractiveItem : MonoBehaviour, IInteractive, ICollectible, IHoverable
{
    // 에디터에서 해당 아이템에 맞는 ScriptableObject 할당
    [Header("아이템 원본 데이터")]
    public ItemData _itemData; // ScriptableObject 연결용

    [Header("특수 획득 이벤트")]
    [Tooltip("아이템을 주울 때 시행할 상호작용을 연결하세요.")]
    public UnityEvent OnItemCollected;

    public bool IsAcquired { get; set; }

    protected SpriteRenderer _spriteRenderer;
    protected int _originSortingOrder;
    protected Vector3 _originScale;
    protected bool _suppressHoverUntilPointerExit;

    [Header("Feedback")]
    [SerializeField, Min(0.1f)] protected float hoverScaleMultiplier = 1.2f;
    [SerializeField, Min(0.1f)] protected float pickupScaleMultiplier = 1.45f;
    [SerializeField, Min(0f)] protected float pickupFeedbackDuration = 0.08f;

    protected virtual void Awake()
    {
        IsAcquired = false;

        _originScale = transform.localScale;

        // SpriteRenderer 캐싱
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected virtual void OnEnable()
    {
        _suppressHoverUntilPointerExit = IsPointerAlreadyOverItem();
    }
    
    #region IInteractive 구현
    public virtual void Interact()
    {
        // 아이템 클릭 시 수집
        Collect();
    }
    #endregion

    #region ICollectible 구현
    public virtual void Collect()
    {
        if (IsAcquired) return;
        OnAcquire(); // 획득 상태로 전환

        // 1. 이벤트 버스에 "나 획득됐어!" 하고 데이터 방송
        GameEvent.EOnItemCollected?.Invoke(_itemData);
        OnItemCollected?.Invoke(); // 아이템 자체에 연결된 이벤트도 실행 (예: 효과음, 이펙트 등)
        
        // 2. 월드에서 내 자신을 숨김 (파괴하지 않음)
        if (pickupFeedbackDuration > 0f && isActiveAndEnabled)
            StartCoroutine(PickupFeedbackRoutine());
        else
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
        if (IsAcquired) return;
        if (_suppressHoverUntilPointerExit) return;

        transform.localScale = _originScale * GetHoverScaleMultiplier();
    }

    public virtual void OnHoverExit()
    {
        if (IsAcquired) return;

        _suppressHoverUntilPointerExit = false;
        transform.localScale = _originScale;
    }
    #endregion

    protected virtual IEnumerator PickupFeedbackRoutine()
    {
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = _originScale * GetPickupScaleMultiplier();
        float duration = Mathf.Max(0.01f, pickupFeedbackDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);
            yield return null;
        }

        gameObject.SetActive(false);
    }

    protected float GetHoverScaleMultiplier()
    {
        return hoverScaleMultiplier > 0f ? hoverScaleMultiplier : 1.2f;
    }

    protected float GetPickupScaleMultiplier()
    {
        return pickupScaleMultiplier > 0f ? pickupScaleMultiplier : 1.45f;
    }

    private bool IsPointerAlreadyOverItem()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return false;

        Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Collider2D[] hits = Physics2D.OverlapPointAll(mousePosition);
        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            Transform hitTransform = hit.transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                return true;
        }

        return false;
    }
}
