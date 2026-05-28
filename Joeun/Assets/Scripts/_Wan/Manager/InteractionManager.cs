using UnityEngine;
using UnityEngine.EventSystems;

public class InteractionManager : MonoBehaviour
{
    // 마우스 위에 올려질 오브젝트
    private IHoverable currentHoveredObject;
    // 드래그 중인 오브젝트
    private IDraggable currentDraggedObject;

    private void Update()
    {
        // 1. UI 클릭 방지 (CCTV 화면, 인벤토리 등 UI를 클릭 중일 땐 월드 상호작용 무시)
        if (EventSystem.current.IsPointerOverGameObject()) return;

        HandleHover();
        HandleInput();
    }

    private void HandleHover()
    {
        // 1. 마우스의 화면 좌표를 게임 월드 좌표로 변환
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 2. 마우스 위치에 있는 콜라이더 감지 (isTrigger 체크된 콜라이더도 정상 감지함)
        Collider2D hit = Physics2D.OverlapPoint(mousePos);

        IHoverable newHoveredObject = null;

        // 마우스 위치에 뭔가 있다면 IHoverable 인터페이스가 있는지 확인
        if (hit != null)
        {
            hit.TryGetComponent(out newHoveredObject);
        }

        // 3. 트리거 Enter / Exit 판별 로직
        // 프레임마다 이전 객체와 지금 마우스 아래에 있는 객체가 다를 때만 실행
        if (currentHoveredObject != newHoveredObject)
        {
            // 마우스가 기존 객체에서 빠져나왔다면
            if (currentHoveredObject != null)
            {
                currentHoveredObject.OnHoverExit();
            }

            // 마우스가 새로운 객체 위로 올라갔다면
            if (newHoveredObject != null)
            {
                newHoveredObject.OnHoverEnter();
            }

            // 현재 상태 업데이트
            currentHoveredObject = newHoveredObject;
        }
    }

    private void HandleInput()
    {
        // 마우스 클릭 (상호작용 및 드래그 시작)
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(mousePos);

            if (hit != null)
            {
                // 일반 상호작용 (문 열기, 아이템 획득 등)
                if (hit.TryGetComponent(out IInteractive interactive))
                {
                    interactive.Interact();
                }

                // 드래그 시작 판별
                if (hit.TryGetComponent(out IDraggable draggable))
                {
                    currentDraggedObject = draggable;
                    currentDraggedObject.OnDragStart();
                }
            }
        }
        // 마우스 누르고 있는 중 (드래그)
        else if (Input.GetMouseButton(0) && currentDraggedObject != null)
        {
            currentDraggedObject.OnDrag(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        }
        // 마우스 뗌 (드래그 종료)
        else if (Input.GetMouseButtonUp(0))
        {
            if (currentDraggedObject != null)
            {
                currentDraggedObject.OnDragEnd();
                currentDraggedObject = null;
            }
        }
    }
}