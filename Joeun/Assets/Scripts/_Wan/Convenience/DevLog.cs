using UnityEngine;
using System.Diagnostics; // Conditional을 사용하기 위해 필수

// ==========================================
// Debug.Log를 대체하는 개발용 로그 클래스
// 설명: 에디터 상태에서만 로그가 출력됩니다.
// ==========================================
public static class DevLog
{
    // [Conditional] 속성은 지정된 심볼이 정의되어 있을 때만 함수 호출을 컴파일합니다.
    // UNITY_EDITOR: 유니티 에디터에서만 실행
    [Conditional("UNITY_EDITOR")]
    public static void Log(object message)
    {
        UnityEngine.Debug.Log(message);
    }

    [Conditional("UNITY_EDITOR")]
    public static void LogWarning(object message)
    {
        UnityEngine.Debug.LogWarning(message);
    }

    [Conditional("UNITY_EDITOR")]
    public static void LogError(object message)
    {
        UnityEngine.Debug.LogError(message);
    }
}