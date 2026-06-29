using UnityEngine;

// ==========================================
// 캔버스 카메라 자동 할당기
// 설명: 다른 씬에 있는 Main Camera를 시작 시 자동으로 찾아 연결합니다.
// ==========================================
[RequireComponent(typeof(Canvas))]
public class CanvasCameraAssigner : MonoBehaviour
{
    private void Start()
    {
        Canvas myCanvas = GetComponent<Canvas>();
        
        // Render Mode가 Camera나 World Space일 때만 작동
        if (myCanvas.renderMode == RenderMode.ScreenSpaceCamera || 
            myCanvas.renderMode == RenderMode.WorldSpace)
        {
            // Camera.main은 씬이 달라도 "MainCamera" 태그가 붙은 카메라를 귀신같이 찾아옵니다.
            myCanvas.worldCamera = Camera.main;
            DevLog.Log($"[{gameObject.name}] 코어 씬의 메인 카메라를 성공적으로 연결했습니다.");
        }
    }
}