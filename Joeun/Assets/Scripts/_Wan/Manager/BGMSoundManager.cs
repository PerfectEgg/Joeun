using UnityEngine;
using System.Collections;

// ==========================================
// BGM 사운드 매니저 클래스
// 설명: 게임 내 배경음 사운드를 관리합니다. (싱글톤 코어 상속)
// ==========================================
public class BGMSoundManager : SingletonCore<BGMSoundManager>
{
    [Header("사운드 뱅크 설정")]
    [Tooltip("타이틀 배경 음악")]
    [SerializeField] private StageSound _titleBGMSound;

    [Tooltip("스테이지별 배경 음악 모음 (0번 인덱스 = 1스테이지)")]
    [SerializeField] private StageSound _stageBGMSound;
    
    [Header("AudioSource 연결")]
    [SerializeField] private AudioSource _bgmSource; // 효과음 재생용 채널

    // 현재 실행 중인 페이드 코루틴을 추적하여 중복 실행 방지
    private Coroutine _fadeCoroutine;

    void OnEnable()
    {
        GameEvent.EBGMPlayWithFade += PlayBGMWithFade;
        GameEvent.EBGMStopWithFade += StopBGMWithFade;
        GameEvent.EBGMPlayInstantly += PlayBGMInstantly;
        GameEvent.EBGMStopInstantly += StopBGMInstantly;
    }

    void OnDisable()
    {
        GameEvent.EBGMPlayWithFade -= PlayBGMWithFade;
        GameEvent.EBGMStopWithFade -= StopBGMWithFade;
        GameEvent.EBGMPlayInstantly -= PlayBGMInstantly;
        GameEvent.EBGMStopInstantly -= StopBGMInstantly;
    }

    void Start()
    {
        _bgmSource.loop = true;
    }

    // ==========================================
    // 📌 1. 페이드 제어 (부드러운 전환)
    // ==========================================
    public void PlayBGMWithFade(string soundID, float fadeTime = 0.75f)
    {
        SoundData targetData = FindBGMData(soundID);

        if (targetData != null && targetData.clip != null)
        {
            if (_bgmSource.clip == targetData.clip && _bgmSource.isPlaying && _bgmSource.volume == targetData.volume) return;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(CrossfadeRoutine(targetData, fadeTime));
        }
        else
        {
            DevLog.LogWarning($"[BGMSoundManager] ID '{soundID}'를 찾을 수 없습니다.");
        }
    }

    public void StopBGMWithFade(float fadeTime = 0.75f)
    {
        if (!_bgmSource.isPlaying) return;

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeOutRoutine(fadeTime));
    }

    // ==========================================
    // 📌 2. 즉시 제어 (연출용 컷컷 전환)
    // ==========================================

    public void PlayBGMInstantly(string soundID)
    {
        SoundData targetData = FindBGMData(soundID);

        if (targetData != null && targetData.clip != null)
        {
            if (_bgmSource.clip == targetData.clip && _bgmSource.isPlaying && _bgmSource.volume == targetData.volume) return;

            // 진행 중인 페이드가 있다면 즉시 강제 종료
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            
            _bgmSource.clip = targetData.clip;
            _bgmSource.volume = targetData.volume;
            _bgmSource.Play();
        }
        else
        {
            DevLog.LogWarning($"[BGMSoundManager] ID '{soundID}'를 찾을 수 없습니다.");
        }
    }

    public void StopBGMInstantly()
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        
        _bgmSource.Stop();
        _bgmSource.volume = 0f;
    }

    // ==========================================
    // ⚙️ 내부 코루틴 및 헬퍼 함수
    // ==========================================

    private IEnumerator CrossfadeRoutine(SoundData newData, float totalFadeTime)
    {
        bool wasPlaying = _bgmSource.isPlaying;
        float halfTime = totalFadeTime / 2f;

        if (wasPlaying)
        {
            float startVol = _bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < halfTime)
            {
                elapsed += Time.deltaTime;
                _bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / halfTime);
                yield return null;
            }
            _bgmSource.volume = 0f;
            _bgmSource.Stop();
        }

        _bgmSource.clip = newData.clip;
        _bgmSource.Play();

        float targetVol = newData.volume;
        float fadeInDuration = wasPlaying ? halfTime : totalFadeTime;
        float fadeInElapsed = 0f;

        while (fadeInElapsed < fadeInDuration)
        {
            fadeInElapsed += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(0f, targetVol, fadeInElapsed / fadeInDuration);
            yield return null;
        }

        _bgmSource.volume = targetVol;
        _fadeCoroutine = null;
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        float startVol = _bgmSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }

        _bgmSource.volume = 0f;
        _bgmSource.Stop();
        _fadeCoroutine = null;
    }

    private SoundData FindBGMData(string soundID)
    {
        SoundData targetData = null;
        if (_titleBGMSound != null) targetData = _titleBGMSound.GetSoundData(soundID);
        if (targetData == null && _stageBGMSound != null) targetData = _stageBGMSound.GetSoundData(soundID);
        return targetData;
    }
}