using UnityEngine;
using UnityEngine.EventSystems;

// ==========================================
// 스테이지 로드 매니저 클래스
// 설명: 각 스테이지 별로 로드를 관리하는 매니저 클래스입니다.
// ==========================================
public class StageLoadManager : MonoBehaviour
{
    // 마우스 위에 올려질 오브젝트
    private IHoverable _currentHoveredObject;
    // 드래그 중인 오브젝트
    private IDraggable _currentDraggedObject;

    // 메인 카메라 참조 캐싱
    private Camera _mainCamera;

    private void Awake()
    {
        // 시작할 때 한 번만 메인 카메라를 찾아 저장 (성능 최적화)
        _mainCamera = Camera.main; 
    }

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
        Vector2 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);

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
        if (_currentHoveredObject != newHoveredObject)
        {
            // 마우스가 기존 객체에서 빠져나왔다면
            if (_currentHoveredObject != null)
            {
                _currentHoveredObject.OnHoverExit();
            }

            // 마우스가 새로운 객체 위로 올라갔다면
            if (newHoveredObject != null)
            {
                newHoveredObject.OnHoverEnter();
            }

            // 현재 상태 업데이트
            _currentHoveredObject = newHoveredObject;
        }
    }

    private void HandleInput()
    {
        // 마우스 클릭 (상호작용 및 드래그 시작)
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);

            Collider2D hit = Physics2D.OverlapPoint(mousePos);

            if (hit != null)
            {
                bool isCollectedOrDestroyed = false;

                // 1. 일반 상호작용 먼저 실행 (문 열기, 아이템 획득 등)
                if (hit.TryGetComponent(out IInteractive interactive))
                {
                    interactive.Interact();
                    
                    // 상호작용의 결과로 오브젝트가 비활성화(획득) 되었는지 체크
                    if (!hit.gameObject.activeSelf)
                    {
                        isCollectedOrDestroyed = true;
                    }
                }

                // 2. 오브젝트가 획득되어 사라지지 않았을 때만 드래그 시작
                if (!isCollectedOrDestroyed && hit.TryGetComponent(out IDraggable draggable))
                {
                    _currentDraggedObject = draggable;
                    _currentDraggedObject.OnDragStart();
                }
            }
        }
        // 마우스 누르고 있는 중 (드래그)
        else if (Input.GetMouseButton(0) && _currentDraggedObject != null)
        {
            _currentDraggedObject.OnDrag(_mainCamera.ScreenToWorldPoint(Input.mousePosition));
        }
        // 마우스 뗌 (드래그 종료)
        else if (Input.GetMouseButtonUp(0))
        {
            if (_currentDraggedObject != null)
            {
                _currentDraggedObject.OnDragEnd();
                _currentDraggedObject = null;
            }
        }
    }
}