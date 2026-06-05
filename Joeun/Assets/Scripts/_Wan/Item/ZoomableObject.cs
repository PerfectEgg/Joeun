using UnityEngine;

// ==========================================
// 확대 오브젝트 클래스
// ==========================================
public class ZoomableObject : MonoBehaviour, IInteractive
{
    [Tooltip("이 오브젝트를 클릭했을 때 띄워줄 확대 뷰의 인덱스")]
    [SerializeField] private int zoomViewIndex;
    
    public void Interact()
    {
        // 이벤트 버스를 통해 인덱스 번호를 전달하며 신호 발생
        GameEvent.EOnCCTVZoomInView?.Invoke(zoomViewIndex);
    }
}