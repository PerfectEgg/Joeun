using UnityEngine;
using UnityEngine.Events;

// ==========================================
// 문 클래스
// ==========================================
public class Door : MonoBehaviour, IInteractive, IOpenable
{
    // 초기 잠금 여부에 따라 열린 문인지 잠긴 문인지 결정
    [SerializeField] private bool _isLocked = true; // 초기값은 잠긴 상태
    [SerializeField] private bool _isRecyclable = true; // 문은 재활용 가능하도록 설정

    public bool IsLocked { get; private set; } = true;
    public bool IsRecyclable { get; private set; } = true;
    public bool IsOpen { get; private set; } = false;

    [Header("Z 레이어 설정")]
    [SerializeField] private int _setZLayer = 1;

    [Header("잠금 해제 이벤트")]
    public UnityEvent OnUnlockSuccess; 

    [Header("상호작용 이벤트")]
    public UnityEvent OnInteractive; // 문이 열릴 때 실행할 이벤트 (예: 애니메이션, 사운드 등)

    void Awake()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z - _setZLayer);
    }

    void Start()
    {
        IsLocked = _isLocked;
        IsRecyclable = _isRecyclable;
    }

    #region 외부 장치 연동
    // 외부의 장치(KeyReader 등)가 잠금을 풀어줄 때 호출하는 함수
    public void UnlockFromExternal()
    {
        if (!IsLocked) return;
        
        IsLocked = false;

        OnUnlockSuccess?.Invoke();

        if (IsRecyclable)
        {
            DevLog.Log("문이 다시 잠길 수 있도록 설정되었습니다.");
        }
        else
        {
            IsOpen = true; // 문이 열린 상태로 고정
            gameObject.SetActive(false); // 문이 완전히 사라지도록 처리 (재활용 불가능)
            DevLog.Log("문이 완전히 잠금 해제되었습니다. 다시 잠기지 않습니다.");
        }

        DevLog.Log("문의 잠금이 해제되는 소리가 들린다!");
    }
    #endregion

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
        if (IsOpen) Close();
        else Open();
    }
    #endregion

    #region IOpenable 구현
    public void Open()
    {
        if (IsLocked) return; // 안전 장치

        IsOpen = true;

        /// TODO.현재 열고 닫는 이미지가 없기 때문에 임시로 비활성화 차후 스프라이트로 교체 예정
        gameObject.SetActive(false);
        OnInteractive?.Invoke(); // 문이 열릴 때 실행할 이벤트 호출

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