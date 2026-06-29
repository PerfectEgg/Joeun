using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// ==========================================
// 클로징 UI 매니저
// 설명: 끝내기 클릭을 처리합니다.
// ==========================================
public class ClosingUIManager : MonoBehaviour
{
    [Header("UI 버튼 연결")]
    [SerializeField] private Button _exitButton;

    [Header("페이드 연출 설정")]
    [Tooltip("화면 전체를 가릴 검은색 Image 패널을 연결하세요.")]
    [SerializeField] private Image _fadePanel;

    void Awake()
    {
        // 게임 시작 시 패널이 켜져 있다면, 첫 스테이지가 보일 수 있도록 투명하게 초기화
        if (_fadePanel != null)
        {
            _fadePanel.gameObject.SetActive(true);
            Color c = _fadePanel.color;
            c.a = 0f;
            _fadePanel.color = c;
        }
    }

    void Start()
    {
        PlayFadeInRoutine(1.25f);
    }

    private void PlayFadeInRoutine(float duration)
    {
        StartCoroutine(FadeInRoutine(duration));
    }

    // 화면을 점진적으로 밝게 만듭니다 (검은색 -> 투명)
    public IEnumerator FadeInRoutine(float duration)
    {
        if (_fadePanel == null) yield break;

        float elapsed = 0f;
        Color color = _fadePanel.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // 시간에 따라 알파값을 1에서 0으로 보간
            color.a = Mathf.Clamp01(1f - (elapsed / duration));
            _fadePanel.color = color;
            yield return null;
        }

        // 확실하게 완벽한 투명으로 고정 후 컴포넌트 비활성화(최적화)
        color.a = 0f;
        _fadePanel.color = color;
        DevLog.Log("[GameUIManager] 페이드 인(화면 켜짐) 완료");
    }

    // ==========================================
    // 버튼 클릭 이벤트 연결용 함수
    // ==========================================

    //// <summary>
    /// [종료하기] 버튼을 눌렀을 때 실행됩니다.
    /// </summary>
    public void OnClickExit()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit(); // 빌드된 앱 종료
        #endif
    }

}