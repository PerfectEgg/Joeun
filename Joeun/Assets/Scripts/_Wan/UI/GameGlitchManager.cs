using System;
using UnityEngine;
using UnityEngine.UI; // Image 컴포넌트 제어용
using System.Collections;
using Random = UnityEngine.Random;

// ==========================================
// 게임 글리치 클래스
// 설명: 스테이지 전환 종료 글리치를 담당합니다.
// ==========================================
public class GameGlitchManager : MonoBehaviour
{
    [Header("글리치 UI 연결")]
    [SerializeField] private CanvasGroup _glitchCanvasGroup;
    [SerializeField] private RawImage _noiseImage;

    [Header("연출 설정")]
    [SerializeField] private float _glitchDuration = 0.5f; 
    [SerializeField] private float _fadeDuration = 0.5f;  
    [SerializeField] private float _shakeSpeed = 0.05f;    

    private Coroutine _shakeCoroutine;

    private void Awake()
    {
        // 평소에는 글리치 패널을 완전히 꺼둡니다. (최적화)
        if (_glitchCanvasGroup != null)
            _glitchCanvasGroup.gameObject.SetActive(false);
    }

    /// <summary>
    /// 외부에서 원하는 타이밍에 연출을 시작하거나, Start()에서 자동 실행되게 할 수 있습니다.
    /// </summary>
    public void PlayEnterSequence(Action onComplete = null)
    {
        StartCoroutine(EnterSequenceRoutine(onComplete));
    }

    private void Start()
    {
        PlayEnterSequence();
    }

    private IEnumerator EnterSequenceRoutine(Action onComplete)
    {
        // 1. 씬 로드 완료 직후 패널을 켜서 연출 시작
        _glitchCanvasGroup.gameObject.SetActive(true);
        _glitchCanvasGroup.alpha = 1f;
        _shakeCoroutine = StartCoroutine(ShakeUVRoutine());

        // 2. 페이드 인 (밝아짐)
        GameEvent.EFadeIn?.Invoke(_fadeDuration);

        // 3. 글리치 서서히 끄기
        float elapsed = 0f;
        while (elapsed < _glitchDuration)
        {
            elapsed += Time.deltaTime;
            _glitchCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / _glitchDuration);
            yield return null;
        }
        
        // 4. 연출 종료 및 정리
        _glitchCanvasGroup.gameObject.SetActive(false);
        _glitchCanvasGroup.alpha = 0f;
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);

        // 연출 종료 알림 (예: 플레이어 조작 잠금 해제 등)
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
