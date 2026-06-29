using UnityEngine;

// ==========================================
// SFX 사운드 매니저 클래스
// 설명: 게임 내 효과음 사운드를 관리합니다.
// ==========================================
public class SFXSoundManager : MonoBehaviour
{
    [Header("사운드 뱅크 설정")]
    [Tooltip("UI 클릭, 걷기 등 모든 스테이지에서 공통으로 쓰이는 사운드 모음")]
    [SerializeField] private StageSound _commonStageSound;

    [Tooltip("스테이지별 사운드 모음 (0번 인덱스 = 1스테이지)")]
    [SerializeField] private StageSound[] _stageSound;
    
    [Header("AudioSource 연결")]
    [SerializeField] private AudioSource _sfxSource; // 효과음 재생용 채널

    [Header("현재 스테이지")]
    [SerializeField] private int _selectStageIndex = -1; // 효과음 재생용 채널

    // 현재 플레이 중인 스테이지 인덱스 (StageLoadManager와 동일한 번호)
    private int _currentStageIndex = 0;

    void OnEnable()
    {
        GameEvent.ESFXPlay += PlaySFX;
        GameEvent.ECurrentStage += SetCurrentStageIndex;
    }

    void OnDisable()
    {
        GameEvent.ESFXPlay -= PlaySFX;
        GameEvent.ECurrentStage -= SetCurrentStageIndex;
    }

    void Start()
    {
        if(_selectStageIndex != -1)
        {
            _currentStageIndex = _selectStageIndex;
        }
    }

    /// <summary>
    /// StageLoadManager에서 씬을 바꿀 때 이 함수를 불러 번호를 동기화해 줍니다.
    /// </summary>
    public void SetCurrentStageIndex(int index)
    {
        _currentStageIndex = index;
        DevLog.Log($"[SoundManager] 현재 사운드 참조 스테이지가 {_currentStageIndex}번으로 맞춰졌습니다.");
    }

    /// <summary>
    /// 사운드 ID를 입력하면 공용 뱅크 -> 스테이지 뱅크 순서로 찾아서 재생합니다.
    /// </summary>
    public void PlaySFX(string soundID)
    {
        SoundData targetData = null;

        // 1. 공용 뱅크에서 먼저 검색 (우선순위 높음)
        if (_commonStageSound != null)
        {
            targetData = _commonStageSound.GetSoundData(soundID);
        }

        // 2. 공용 뱅크에 없다면, 현재 플레이 중인 스테이지 뱅크에서 검색
        if (targetData == null)
        {
            if (_currentStageIndex >= 0 && _currentStageIndex < _stageSound.Length)
            {
                StageSound currentStageBank = _stageSound[_currentStageIndex];
                if (currentStageBank != null)
                {
                    targetData = currentStageBank.GetSoundData(soundID);
                }
            }
        }

        // 3. 최종 재생
        if (targetData != null && targetData.clip != null)
        {
            _sfxSource.PlayOneShot(targetData.clip, targetData.volume);
        }
        else
        {
            DevLog.LogWarning($"[SoundManager] ID '{soundID}'에 해당하는 사운드를 찾을 수 없습니다. (공용 및 {_currentStageIndex}번 스테이지 확인 요망)");
        }
    }
}