using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// ==========================================
// 인벤토리 UI 아이템 클래스
// 설명: 인벤토리에 표시되는 각 아이템의 UI를 관리합니다.
// ==========================================
public class UIInventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Vector2 OriginPosition { get; private set; }
    private Transform _originalParent;

    public ItemData _myItemData;
    private Image _iconImage;
    private Vector3 _originPos;
    private Vector3 _originScale;

    // 빈자리를 지켜줄 투명한 더미 객체
    private GameObject _placeholder;

    protected virtual void Awake()
    {
        _iconImage = GetComponent<Image>();
        _originPos = transform.position;
        _originScale = transform.localScale;
    }

    public void Setup(ItemData data)
    {
        _myItemData = data;
        _iconImage.sprite = data.itemIcon; // 에디터에서 넣은 이미지로 변경
    }
    
    #region IInteractive 구현
    public virtual void Interact()
    {
        // 기본 상호작용 로직 (예: 아이템 설명 텍스트 출력 등)
    }
    #endregion

    #region IDragHandler 구현
    // 드래그 시작 시 빈칸인 플레이스홀더 생성 및 원래 위치 저장
    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        // 원래 위치 저장
        OriginPosition = transform.position;
        _originalParent = transform.parent;

        // 1. 내 자리를 대신할 투명한 플레이스홀더(빈 칸) 생성
        _placeholder = new GameObject("Placeholder");
        
        // 2. 나와 똑같은 UI 크기를 가지도록 RectTransform 설정
        RectTransform rt = _placeholder.AddComponent<RectTransform>();
        rt.sizeDelta = GetComponent<RectTransform>().sizeDelta;
        
        // 3. Grid Layout Group이 인식할 수 있도록 LayoutElement 추가
        LayoutElement le = _placeholder.AddComponent<LayoutElement>();
        le.preferredWidth = rt.sizeDelta.x;
        le.preferredHeight = rt.sizeDelta.y;

        // 4. 플레이스홀더를 인벤토리 패널의 내 원래 자리(SiblingIndex)에 끼워넣기
        _placeholder.transform.SetParent(_originalParent);
        _placeholder.transform.SetSiblingIndex(transform.GetSiblingIndex());

        transform.SetParent(transform.root); // 캔버스 최상단으로 이동
        
        _iconImage.raycastTarget = false;     // 마우스 클릭 방해 해제
    }

    // 드래그 도중 마우스 위치로 아이템 이동
    public virtual void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position; // 마우스 위치로 이동
    }

    // 드래그 끝났을 때 플레이스홀더 제거 및 아이템 위치 결정
    public virtual void OnEndDrag(PointerEventData eventData)
    {
        _iconImage.raycastTarget = true;
        Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(worldPoint);
        
        if (hit != null && hit.TryGetComponent(out IPickable target))
        {
            if (target.TryUnlock(_myItemData.itemID))
            {
                DevLog.Log("아이템 사용 성공!");

                Destroy(_placeholder); 
                Destroy(gameObject);

                GameEvent.EOnItemUsed?.Invoke(_myItemData); // 아이템 사용 이벤트 방송
                return;
            }
        }
        // 실패 시 제자리로 복구
        transform.position = _originPos;
        transform.SetParent(_originalParent);
        transform.SetSiblingIndex(_placeholder.transform.GetSiblingIndex());

        Destroy(_placeholder);
    }
    #endregion

    #region IPointerHandler 구현
    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = transform.localScale * 1.2f;
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = _originScale;
    }
    #endregion
}