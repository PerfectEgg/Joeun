using UnityEngine;
using UnityEngine.Events;

// ==========================================
// 문 클래스
// ==========================================
public class Door : MonoBehaviour, IInteractive, IOpenable, IConditionRequirable
{
    // 초기 잠금 여부에 따라 열린 문인지 잠긴 문인지 결정
    [SerializeField] private bool _isLocked = true; // 초기값은 잠긴 상태
    [SerializeField] private bool _isRecyclable = true; // 문은 재활용 가능하도록 설정
    [SerializeField] private bool _isExitDoor = false; // 탈출구 설정
    [SerializeField] private bool _isOpen = true; // 열러 있는 지 체크

    public bool IsLocked { get; set; } = true;
    public bool IsRecyclable { get; private set; } = true;
    public bool IsExitDoor { get; private set; } = false;
    public bool IsOpen { get; set; } = false;


    [SerializeField] private bool _isFirstStage = false; // 1스테이지인지 체크
    

    [Header("Z 레이어 설정")]
    [SerializeField] private int _setZLayer = 1;

    [Header("잠금 해제 이벤트")]
    public UnityEvent OnUnlockSuccess; 

    [Header("상호작용 이벤트")]
    public UnityEvent OnInteractive; // 문이 열릴 때 실행할 이벤트 (예: 애니메이션, 사운드 등)

    [Header("조건 설정")]
    [Tooltip("잠금 해제 조건이 필요한지 여부 (예: 선행 오브젝트 처리)")]
    [SerializeField] private int _conditionCount = 0;

    public bool IsConditionRequired { get; set; } = false; // 실제 조건이 충족되었는지 내부적으로 추적하는 변수

    void Awake()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z - _setZLayer);
    }

    void Start()
    {
        IsLocked = _isLocked;
        IsRecyclable = _isRecyclable;
        IsExitDoor = _isExitDoor;
        IsOpen = _isOpen;
    }

    #region IConditionRequirable 구현
    // 외부의 다른 스위치나 퍼즐이 풀렸을 때 UnityEvent를 통해 호출될 함수입니다.
    public void ResolveCondition()
    {
        _conditionCount--;
        if (_conditionCount <= 0)
        {
            IsConditionRequired = false;
            DevLog.Log($"{gameObject.name}의 선행 조건이 달성되었습니다! 이제 아이템을 사용할 수 있습니다.");
        }
    }
    #endregion

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
            Open();
            DevLog.Log("문이 완전히 잠금 해제되었습니다. 다시 잠기지 않습니다.");
        }

        DevLog.Log("문의 잠금이 해제되는 소리가 들린다!");
    }
    #endregion

    #region IInteractive 구현
    public void Interact()
    {
        // 1. 선행 조건 검사
        if (_conditionCount > 0)
        {
            DevLog.Log("선행 조건이 존재합니다.");
            return;
        }

        // 2. 문이 잠겨있을 때 클릭하면
        if (IsLocked)
        {
            DevLog.Log("문이 굳게 잠겨 있다. 열쇠구멍이 보인다.");
            // TODO: 덜컹거리는 소리 재생 또는 문이 흔들리는 애니메이션 실행
            return;
        }

        OnInteractive?.Invoke(); // 문이 열릴 때 실행할 이벤트 호출

        if(IsExitDoor)
        {
            if (_isFirstStage)
            {
                GameEvent.ESFXPlay?.Invoke("Stage1_Exit_Door_Open");
            }
            else
            {
                GameEvent.ESFXPlay?.Invoke("Exit_Door_Open");
            }

            GameEvent.EStageClear?.Invoke();

            return;
        }

        // 3. 문이 잠겨있지 않을 때 클릭하면 상태에 따라 열거나 닫음
        if (IsOpen) Open();
        else Close();
    }
    #endregion

    #region IOpenable 구현
    public void Open()
    {
        if (IsLocked) return; // 안전 장치

        /// TODO.현재 열고 닫는 이미지가 없기 때문에 임시로 비활성화 차후 스프라이트로 교체 예정
        gameObject.SetActive(false);
        
        DevLog.Log("끼이익- 문이 열립니다.");
        GameEvent.ESFXPlay?.Invoke("Door_Open");
    }

    public void Close()
    {
        if (IsLocked) return; // 안전 장치
        
        /// TODO.현재 열고 닫는 이미지가 없기 때문에 임시로 비활성화 차후 스프라이트로 교체 예정
        gameObject.SetActive(false);

        DevLog.Log("끼이익- 문을 닫습니다.");
        GameEvent.ESFXPlay?.Invoke("Door_Close");
    }
    #endregion
}