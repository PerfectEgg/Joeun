using UnityEngine;

// ==========================================
// 스테이지 BGM 스타터 (각 스테이지 씬마다 배치)
// ==========================================
public class StageBGMStarter : MonoBehaviour
{
    [Tooltip("이 스테이지가 시작될 때 틀어줄 BGM의 ID를 적으세요.")]
    [SerializeField] private string _stageBGM_ID;

    private void Start()
    {
        // 씬이 켜지자마자 해당 BGM을 재생 (이미 같은 곡이 재생 중이면 알아서 무시됨)
        if (!string.IsNullOrEmpty(_stageBGM_ID))
        {
           GameEvent.EBGMPlayWithFade?.Invoke(_stageBGM_ID, 0.75f);
        }
    }
}