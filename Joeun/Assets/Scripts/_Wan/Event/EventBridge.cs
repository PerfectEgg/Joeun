using UnityEngine;

// ==========================================
// 이벤트 브릿지 클래스
// 설명: UnityEvent(인스펙터)에서 GameEvent(C# 버스)를 호출할 수 있게 해주는 징검다리입니다.
// ==========================================
public class EventBridge : MonoBehaviour
{
    /// <summary>
    /// 인스펙터에서 사운드 ID를 직접 타이핑해서 넘겨줍니다.
    /// </summary>
    public void PlaySFX(string soundID)
    {
        GameEvent.ESFXPlay?.Invoke(soundID);
        DevLog.Log($"[EventBridge] 사운드 재생 요청: {soundID}");
    }
}
