using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


public class Item : MonoBehaviour, IDraggable, IHoverable
{
    public Vector2 OriginPosition { get; protected set; }
    public bool IsAcquired { get; protected set; }

    protected SpriteRenderer spriteRenderer;
    protected int originSortingOrder;
    protected Vector3 originScale;
    protected bool isDragging;

    [Header("Feedback")]
    [SerializeField, Min(0.1f)] protected float hoverScaleMultiplier = 1.2f;
    [SerializeField, Min(0.1f)] protected float dragScaleMultiplier = 1.35f;

    private void Awake()
    {
        IsAcquired = false;

        // SpriteRenderer 캐싱
        spriteRenderer = GetComponent<SpriteRenderer>();
        originScale = transform.localScale;
    }
    
    #region IDraggable 구현
    public void Interact()
    {
        // 기본 상호작용 로직 (예: 아이템 설명 텍스트 출력 등)
    }
    #endregion
    
    #region IDraggable 구현
    public void OnAcquire()
    {
        IsAcquired = true;
    }

    public void OnDragStart()
    {
        // 원래 위치 저장
        OriginPosition = transform.position;
        isDragging = true;
        transform.localScale = originScale * GetDragScaleMultiplier();

        // 드래그 시작 시 화면 최상단으로 보이게 처리
        if (spriteRenderer != null)
        {
            originSortingOrder = spriteRenderer.sortingOrder;
            spriteRenderer.sortingOrder = 999; 
        }
    }

    public void OnDrag(Vector2 currentMousePosition)
    {
        transform.position = currentMousePosition;
    }

    public void OnDragEnd()
    {
        // 다시 원래 위치로 복구
        transform.position = OriginPosition;
        isDragging = false;
        transform.localScale = originScale;

        // 드래그 종료 시 랜더링 순서 정상 복구
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = originSortingOrder;
        }
    }
    #endregion

    #region IHoverable 구현
    public void OnHoverEnter()
    {
        if (isDragging) return;

        transform.localScale = originScale * GetHoverScaleMultiplier();
    }

    public void OnHoverExit()
    {
        if (isDragging) return;

        transform.localScale = originScale;
    }
    #endregion

    protected float GetHoverScaleMultiplier()
    {
        return hoverScaleMultiplier > 0f ? hoverScaleMultiplier : 1.2f;
    }

    protected float GetDragScaleMultiplier()
    {
        return dragScaleMultiplier > 0f ? dragScaleMultiplier : 1.35f;
    }
}
