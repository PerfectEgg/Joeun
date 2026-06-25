using UnityEngine;
using UnityEngine.UI; // Image 컴포넌트 제어용
using System.Collections;

// ==========================================
// 게임 UI 매니저 클래스
// 설명: 스테이지 전환 시 Fade-in과 Fade-out을 담당합니다.
// ==========================================
public class GameUIManager : SingletonCore<GameUIManager>
{
    [Header("페이드 연출 설정")]
    [Tooltip("화면 전체를 가릴 검은색 Image 패널을 연결하세요.")]
    [SerializeField] private Image _fadePanel;
    
    protected override void Awake()
    {
        base.Awake();

        // 게임 시작 시 패널이 켜져 있다면, 첫 스테이지가 보일 수 있도록 투명하게 초기화
        if (_fadePanel != null)
        {
            _fadePanel.gameObject.SetActive(true);
            Color c = _fadePanel.color;
            c.a = 0f;
            _fadePanel.color = c;
        }
    }

    // 화면을 점진적으로 어둡게 만듭니다 (투명 -> 검은색)
    public IEnumerator FadeOutRoutine(float duration)
    {
        if (_fadePanel == null) yield break;

        _fadePanel.gameObject.SetActive(true);
        float elapsed = 0f;
        Color color = _fadePanel.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // 시간에 따라 알파값을 0에서 1로 보간
            color.a = Mathf.Clamp01(elapsed / duration);
            _fadePanel.color = color;
            yield return null;
        }

        // 확실하게 완벽한 검은색으로 고정
        color.a = 1f;
        _fadePanel.color = color;
        DevLog.Log("[GameUIManager] 페이드 아웃(암전) 완료");
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
}
