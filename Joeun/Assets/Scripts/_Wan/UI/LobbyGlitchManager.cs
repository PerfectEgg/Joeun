using System;
using UnityEngine;
using UnityEngine.UI; // Image 컴포넌트 제어용
using System.Collections;
using Random = UnityEngine.Random;

// ==========================================
// 로비 글리치 클래스
// 설명: 스테이지 전환 시작 글리치를 담당합니다.
// ==========================================
public class LobbyGlitchManager : MonoBehaviour
{
    [Header("글리치 UI 연결")]
    [SerializeField] private CanvasGroup _glitchCanvasGroup;
    [SerializeField] private RawImage _noiseImage;

    [Header("연출 설정")]
    [SerializeField] private float _glitchDuration = 0.5f; 
    [SerializeField] private float _fadeDuration = 0.5f;  
    [SerializeField] private float _shakeSpeed = 0.05f;    

    private bool _isTransitioning = false;
    private Coroutine _shakeCoroutine;

    private void Awake()
    {
        // 평소에는 글리치 패널을 완전히 꺼둡니다. (최적화)
        if (_glitchCanvasGroup != null)
        {
            _glitchCanvasGroup.alpha = 0f;
            _glitchCanvasGroup.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 외부(씬 로더 등)에서 이 함수를 호출하여 연출을 시작합니다.
    /// </summary>
    /// <param name="onComplete">연출이 끝난 후 실행할 행동 (예: 씬 로드)</param>
    public void PlayExitSequence(Action onComplete = null)
    {
        if (_isTransitioning) return;
        StartCoroutine(ExitSequenceRoutine(onComplete));
    }

    private IEnumerator ExitSequenceRoutine(Action onComplete)
    {
        _isTransitioning = true;

        if (BGMSoundManager.Instance != null) BGMSoundManager.Instance.StopBGMWithFade(_fadeDuration);

        // 1. 연출 시작! 패널을 켜고 알파를 1로 올립니다.
        _glitchCanvasGroup.gameObject.SetActive(true);
        _glitchCanvasGroup.alpha = 0f;
        _shakeCoroutine = StartCoroutine(ShakeUVRoutine());

        // 2. 글리치 서서히 나타나기 (Lerp를 이용한 Fade In)
        float elapsed = 0f;
        while (elapsed < _glitchDuration)
        {
            elapsed += Time.deltaTime;
            // 0에서 1로 지정된 시간(_glitchDuration) 동안 부드럽게 증가
            _glitchCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / _glitchDuration);
            yield return null; // 다음 프레임까지 대기
        }
        _glitchCanvasGroup.alpha = 1f; // 오차 방지를 위해 마지막에 확실하게 1로 고정
        
        // 3. UI 연출이 모두 끝났음을 외부에 알림! (여기서 씬 로드가 실행됨)
        onComplete?.Invoke();
    }

    private IEnumerator ShakeUVRoutine()
    {
        while (true) 
        {
            if (_noiseImage != null)
            {
                // 1. 위치 이동 (기존)
                float randomX = Random.Range(-1f, 1f);
                float randomY = Random.Range(-0.25f, 0.25f);
                
                // 2. 텍스처 찌그러뜨리기 (핵심)
                // 가로(X)는 1배~5배로 무작위로 쭈욱 늘리고, 세로(Y)는 0.05배~0.3배로 아주 얇게 압축합니다.
                float stretchX = Random.Range(0.1f, 0.25f); 
                float stretchY = 1f;   

                // 3. 위치와 찌그러진 비율을 동시에 적용!
                _noiseImage.uvRect = new Rect(randomX, randomY, stretchX, stretchY);
            }
            yield return new WaitForSeconds(_shakeSpeed);
        }
    }
}
