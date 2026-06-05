using UnityEngine;

// ==========================================
// CCTV 매니저 클래스
// 설명: CCTV 화면으로 현재 위치를 저장하고, 다음 CCTV로 이동할 때 저장된 위치로 이동시킵니다.
// ==========================================
public class CCTVManager : MonoBehaviour
{
    [Header("화면 리스트 분리")]
    [Tooltip("일반 방 뷰들을 순서대로 넣으세요. (예: 정면, 왼쪽, 오른쪽)")]
    public GameObject[] roomViews; 
    
    [Tooltip("확대 뷰(퍼즐, 서랍 등)들을 순서대로 넣으세요.")]
    public GameObject[] zoomViews;

    [Header("UI 버튼 연결")]
    [Tooltip("왼쪽 이동 화살표 버튼 오브젝트를 연결하세요.")]
    [SerializeField] private GameObject leftArrowButton;

    [Tooltip("오른쪽 이동 화살표 버튼 오브젝트를 연결하세요.")]
    [SerializeField] private GameObject rightArrowButton;

    [Tooltip("뒤로가기 화살표 버튼 오브젝트를 연결하세요.")]
    [SerializeField] private GameObject backArrowButton;

    // 현재 보고 있는 화면의 인덱스
    private int currentRoomIndex = 0;
    // 확대 전 원래 보고 있던 방 번호를 기억할 변수
    private int currentZoomIndex = 0;
    // 확대 뷰에 진입했는지 여부 (뒤로가기 버튼 활성화 조건으로 사용)
    private bool isZoomed = false;

    private void OnEnable()
    {
        // 이벤트 구독 연결
        GameEvent.EOnCCTVZoomInView += HandleZoomIn;
        GameEvent.EOnCCTVZoomOutView += HandleZoomOut;
    }

    private void OnDisable()
    {
        // 메모리 누수 방지를 위해 구독 해제
        GameEvent.EOnCCTVZoomInView -= HandleZoomIn;
        GameEvent.EOnCCTVZoomOutView -= HandleZoomOut;
    }

    #region 📌 이벤트 관련 함수
    // 1. 확대 뷰 진입 이벤트 처리
    private void HandleZoomIn(int targetZoomIndex)
    {
        // 확대 뷰로 들어가기 직전, 현재 방 번호를 안전하게 저장
        isZoomed = true;
        currentZoomIndex = targetZoomIndex;
        UpdateViews();
        DevLog.Log($"[CCTV] 확대 뷰 진입: {zoomViews[currentZoomIndex].name}");
    }

    // 2. 확대 뷰 이탈(뒤로가기) 이벤트 처리
    private void HandleZoomOut()
    {
        isZoomed = false; // 상태만 끄면 알아서 원래 방으로 돌아갑니다.
        UpdateViews();
        DevLog.Log($"[CCTV] 일반 방으로 복귀: {roomViews[currentRoomIndex].name}");
    }
    #endregion

    private void Start()
    {
        isZoomed = false;

        // 시작할 때 첫 번째 화면만 켜고 나머지는 모두 끕니다.
        UpdateViews();
    }

    // 오른쪽 화살표를 눌렀을 때 호출될 함수
    public void GoToRightView()
    {
        if (isZoomed) return; // 확대 모드일 땐 작동 방지 (안전장치)

        currentRoomIndex++;
        
        // 마지막 화면을 넘어가면 다시 첫 화면으로 돌아오도록 (배열 순환)
        if (currentRoomIndex >= roomViews.Length)
        {
            currentRoomIndex = 0;
        }
        
        UpdateViews();
    }

    // 왼쪽 화살표를 눌렀을 때 호출될 함수
    public void GoToLeftView()
    {
        if (isZoomed) return; // 확대 모드일 땐 작동 방지 (안전장치)

        currentRoomIndex--;
        
        // 첫 화면에서 왼쪽으로 가면 마지막 화면으로 가도록
        if (currentRoomIndex < 0)
        {
            currentRoomIndex = roomViews.Length - 1;
        }
        
        UpdateViews();
    }

    // 뒤로가기 화살표를 눌렀을 때 호출될 함수
    public void GoToPreviousView()
    {
        GameEvent.EOnCCTVZoomOutView?.Invoke();
    }

    // 실질적으로 화면을 켜고 끄는 로직
    private void UpdateViews()
    {
        // 1. 일반 방(Room)들 업데이트: 확대 모드가 '아니고', 내 인덱스일 때만 켜짐
        for (int i = 0; i < roomViews.Length; i++)
        {
            roomViews[i].SetActive(!isZoomed && i == currentRoomIndex);
        }

        // 2. 확대 뷰(Zoom)들 업데이트: 확대 모드가 '맞고', 내 인덱스일 때만 켜짐
        for (int i = 0; i < zoomViews.Length; i++)
        {
            zoomViews[i].SetActive(isZoomed && i == currentZoomIndex);
        }

        // 3. UI 버튼 토글
        if (leftArrowButton != null) leftArrowButton.SetActive(!isZoomed);
        if (rightArrowButton != null) rightArrowButton.SetActive(!isZoomed);
        if (backArrowButton != null) backArrowButton.SetActive(isZoomed);
    }
}