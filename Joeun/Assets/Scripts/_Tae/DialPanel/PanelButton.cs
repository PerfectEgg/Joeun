using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>
/// 계기판의 보조 버튼입니다. 지금은 기능이 없지만,
/// 나중에 확장할 수 있도록 클릭/누름/뗌 이벤트 훅만 제공합니다.
/// Inspector의 onClick 등에 동작을 연결하면 됩니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PanelButton : MonoBehaviour,
    IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("식별 (선택)")]
    [Tooltip("여러 버튼을 한 핸들러로 받을 때 구분용")]
    public string buttonId = "";

    [Header("동작 가능 여부")]
    public bool interactable = true;

    [Header("이벤트 (나중에 기능 연결)")]
    public UnityEvent onClick;             // 클릭(누르고 뗌)
    public UnityEvent onPressed;           // 누르는 순간
    public UnityEvent onReleased;          // 떼는 순간
    public UnityEvent<string> onClickWithId;   // id까지 함께 전달

    [Header("눌림 시각 효과 (선택)")]
    public Image targetImage;              // 색을 바꿀 이미지
    public Color normalColor  = Color.white;
    public Color pressedColor = new Color(0.8f, 0.8f, 0.8f);

    void Awake()
    {
        if (targetImage != null) targetImage.color = normalColor;
    }

    public void OnPointerClick(PointerEventData ev)
    {
        if (!interactable) return;
        onClick?.Invoke();
        onClickWithId?.Invoke(buttonId);
        Debug.Log($"[PanelButton] 클릭됨 (id: '{buttonId}')");
    }

    public void OnPointerDown(PointerEventData ev)
    {
        if (!interactable) return;
        if (targetImage != null) targetImage.color = pressedColor;
        onPressed?.Invoke();
    }

    public void OnPointerUp(PointerEventData ev)
    {
        if (targetImage != null) targetImage.color = normalColor;
        if (!interactable) return;
        onReleased?.Invoke();
    }

    /// <summary>코드에서 활성/비활성 전환</summary>
    public void SetInteractable(bool on)
    {
        interactable = on;
    }
}
