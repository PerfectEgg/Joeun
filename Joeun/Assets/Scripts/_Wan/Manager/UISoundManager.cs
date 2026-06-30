using UnityEngine;

// ==========================================
// UI 사운드 매니저 클래스
// 설명: 게임 내 효과음 사운드를 관리합니다.
// ==========================================
public class UISoundManager : MonoBehaviour
{
    [Header("사운드 뱅크 설정")]
    [Tooltip("UI 클릭, 걷기 등 모든 스테이지에서 공통으로 쓰이는 사운드 모음")]
    [SerializeField] private StageSound _uiSound;
    
    [Header("AudioSource 연결")]
    [SerializeField] private AudioSource _sfxSource; // 효과음 재생용 채널

    void OnEnable()
    {
        GameEvent.ESFXPlay += PlaySFX;
    }

    void OnDisable()
    {
        GameEvent.ESFXPlay -= PlaySFX;
    }

    /// <summary>
    /// 사운드 ID를 입력하면 공용 뱅크 -> 스테이지 뱅크 순서로 찾아서 재생합니다.
    /// </summary>
    public void PlaySFX(string soundID)
    {
        SoundData targetData = null;

        // 1. 공용 뱅크에서 먼저 검색 (우선순위 높음)
        if (_uiSound != null)
        {
            targetData = _uiSound.GetSoundData(soundID);
        }

        // 3. 최종 재생
        if (targetData != null && targetData.clip != null)
        {
            _sfxSource.PlayOneShot(targetData.clip, targetData.volume);
        }
        else
        {
            DevLog.LogWarning($"[SoundManager] ID '{soundID}'에 해당하는 사운드를 찾을 수 없습니다.");
        }
    }
}