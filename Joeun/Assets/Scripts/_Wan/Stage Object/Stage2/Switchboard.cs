using UnityEngine;

// ==========================================
// 배전반 클래스
// 설명: 2스테이지 오브젝트인 배전반에 대한 클래스입니다.
// ==========================================
public class Switchboard : MonoBehaviour
{
    [Header("현재 상태")]
    [Tooltip("체크하면 시작 시 초록불(ON), 체크 해제하면 빨간불(OFF) 상태로 시작합니다.")]
    [SerializeField] private bool _isGreenOn = true;

    [Header("표시등 렌더러 (Sprite)")]
    [SerializeField] private SpriteRenderer _greenLightRenderer;
    [SerializeField] private SpriteRenderer _redLightRenderer;
    [SerializeField] private SpriteRenderer _orangeLightRenderer;

    private Color _activeGreenColor = new Color(0f, 1f, 0f); // 초록색
    private Color _activeRedColor = new Color(1f, 0f, 0f); // 빨간색

    private Color _inactiveGreenColor = new Color(0.1f, 0.5f, 0.1f, 1f);
    private Color _inactiveRedColor = new Color(0.5f, 0.1f, 0.1f, 1f);
    private Color _inactiveOrangeColor = new Color(0.5f, 0.25f, 0.1f, 1f);

    private void Start()
    {
        // 게임 시작 시 인스펙터에 설정된 초기 상태에 맞춰 불빛 색상을 칠합니다.
        UpdateLights();
    }

    #region 📌 유니티 이벤트로 호출할 Public 함수들

    // 1. 현재 상태를 반대로 뒤집는 함수 (가장 많이 쓰임)
    public void OnSwitch()
    {
        if (_isGreenOn == true) return; // 이미 초록불이 켜져 있으면 아무 작업도 하지 않음

        _isGreenOn = true; // 초록불 켜짐
        UpdateLights();
    }
    #endregion

    // 핵심 로직: 상태에 따라 렌더러의 색상을 쨍하게 or 어둡게 변경
    private void UpdateLights()
    {
        if (_greenLightRenderer != null)
        {
            // 초록불 켜짐 상태면 쨍한 초록색, 아니면 어두운 색 적용
            _greenLightRenderer.color = _isGreenOn ? _activeGreenColor : _inactiveGreenColor;
        }

        if (_redLightRenderer != null)
        {
            // 빨간불은 초록불 상태의 반대로 작동
            _redLightRenderer.color = !_isGreenOn ? _activeRedColor : _inactiveRedColor;
        }

        if (_orangeLightRenderer != null)
        {
            _orangeLightRenderer.color = _inactiveOrangeColor; // 주황불은 항상 어두운 색으로 유지
        }
    }
}
