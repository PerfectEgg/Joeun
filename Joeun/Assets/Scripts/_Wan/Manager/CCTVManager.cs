using UnityEngine;

// ==========================================
// CCTV 매니저 클래스
// 설명: CCTV 화면으로 현재 위치를 저장하고, 다음 CCTV로 이동할 때 저장된 위치로 이동시킵니다.
// ==========================================
public class CCTVManager : MonoBehaviour
{
    [Header("화면(View) 리스트")]
    [Tooltip("인스펙터에서 View_Main, View_Left 등을 순서대로 넣어주세요.")]
    public GameObject[] views; 

    // 현재 보고 있는 화면의 인덱스
    private int currentViewIndex = 0; 

    private void Start()
    {
        // 시작할 때 첫 번째 화면만 켜고 나머지는 모두 끕니다.
        UpdateViews();
    }

    // 오른쪽 화살표를 눌렀을 때 호출될 함수
    public void GoToNextView()
    {
        currentViewIndex++;
        
        // 마지막 화면을 넘어가면 다시 첫 화면으로 돌아오도록 (배열 순환)
        if (currentViewIndex >= views.Length)
        {
            currentViewIndex = 0;
        }
        
        UpdateViews();
        DevLog.Log($"화면 전환: {views[currentViewIndex].name}");
    }

    // 왼쪽 화살표를 눌렀을 때 호출될 함수
    public void GoToPreviousView()
    {
        currentViewIndex--;
        
        // 첫 화면에서 왼쪽으로 가면 마지막 화면으로 가도록
        if (currentViewIndex < 0)
        {
            currentViewIndex = views.Length - 1;
        }
        
        UpdateViews();
        DevLog.Log($"화면 전환: {views[currentViewIndex].name}");
    }

    // 인덱스 번호로 특정 화면으로 바로 점프할 때 (예: CCTV 모니터 버튼 클릭 시)
    public void JumpToView(int viewIndex)
    {
        if (viewIndex >= 0 && viewIndex < views.Length)
        {
            currentViewIndex = viewIndex;
            UpdateViews();
        }
    }

    // 실질적으로 화면을 켜고 끄는 로직
    private void UpdateViews()
    {
        for (int i = 0; i < views.Length; i++)
        {
            // 현재 인덱스와 일치하는 View만 true, 나머지는 false
            views[i].SetActive(i == currentViewIndex);
        }
    }
}