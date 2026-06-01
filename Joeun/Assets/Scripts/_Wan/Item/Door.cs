using UnityEngine;

// ==========================================
// 문 클래스
// ==========================================
public class Door : MonoBehaviour, IInteractive, IPickable, IOpenable
{
    [SerializeField] private string _requiredKeyID; // 에디터에서 "Key_Silver" 등으로 설정
    public bool IsLocked { get; private set; } = true;
    public bool IsOpen { get; private set; } = false;

    private SpriteRenderer _spriteRenderer;
    private Animator _animator;

    private void Awake()
    {
        // 시작할 때 컴포넌트들을 캐싱해둡니다.
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
    }

    public bool TryUnlock(string keyId)
    {
        if (!IsLocked) 
        {
            DevLog.Log("이미 잠금이 해제된 문입니다.");
            return false; 
        }

        // 넘어온 열쇠 ID가 이 문이 요구하는 ID와 일치하는지 확인
        if (keyId == _requiredKeyID)
        {
            IsLocked = false;
            DevLog.Log("찰칵! 자물쇠가 열렸습니다.");
            
            // 중앙 버스에 문이 열렸음을 방송 (업적, 효과음, 퍼즐 연동용)
            GameEvent.EOnLockOpened?.Invoke(gameObject.name);

            // 잠금 해제와 동시에 문을 활짝 윔
            Open();
            return true; // 잠금 해제 성공 반환 -> UI 아이템 소모됨
        }
        else
        {
            DevLog.Log("열쇠가 맞지 않습니다.");
            return false; // 잠금 해제 실패 반환 -> UI 아이템 원위치됨
        }
     }

    #region IInteractive 구현
    public void Interact()
    {
        // 1. 문이 잠겨있을 때 클릭하면
        if (IsLocked)
        {
            DevLog.Log("문이 굳게 잠겨 있다. 열쇠구멍이 보인다.");
            // TODO: 덜컹거리는 소리 재생 또는 문이 흔들리는 애니메이션 실행
            return;
        }

        // 2. 문이 잠겨있지 않을 때 클릭하면 상태에 따라 열거나 닫음
        if (IsOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }
    #endregion
    
    #region IPickable 구현
    public void OnPick()
    {
        // 문이 열리는 애니메이션 재생
        // 필요한 경우 사운드 효과 재생
        // 예시: Animator 컴포넌트의 "Open" 트리거 활성화
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Open");
        }
    }
    #endregion

    #region IOpenable 구현
    public void Open()
    {
        if (IsLocked) return; // 안전 장치

        IsOpen = true;
        DevLog.Log("끼이익- 문이 열립니다.");
        
        // TODO: 열린 문 스프라이트로 변경 (예: spriteRenderer.sprite = openSprite;)
        // CCTV 화면이라면 문이 열렸을 때 다음 방으로 넘어가는 충돌체(Trigger)를 활성화할 수도 있습니다.
    }

    public void Close()
    {
        IsOpen = false;
        DevLog.Log("쾅! 문을 닫았습니다.");
        
        // TODO: 닫힌 문 스프라이트로 복구
    }
    #endregion
}