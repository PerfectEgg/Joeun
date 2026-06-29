using UnityEngine;

// ==========================================
// Robi BGM 플레이 클래스
// ==========================================
public class RobiBGMPlay : MonoBehaviour
{
    [Tooltip("이 스테이지가 시작될 때 틀어줄 BGM의 ID를 적으세요.")]
    [SerializeField] private string _robiBGM_ID;

    public void OnClickRobi()
    {
        // 버튼을 누르면 해당 BGM을 재생 (이미 같은 곡이 재생 중이면 알아서 무시됨)
        if (!string.IsNullOrEmpty(_robiBGM_ID))
        {
           GameEvent.EBGMPlayWithFade?.Invoke(_robiBGM_ID, 0.75f);
        }
    }
}