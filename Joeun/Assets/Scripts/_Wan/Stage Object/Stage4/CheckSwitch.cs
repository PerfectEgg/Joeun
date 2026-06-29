using UnityEngine;
using TMPro;

// ==========================================
// 체크 스위치 클래스
// 설명: 4스테이지 오브젝트인 체크 스위치에 대한 클래스입니다.
// ==========================================
public class CheckSwitch : MonoBehaviour
{
    [Header("현재 상태")]
    [Tooltip("체크하면 시작 시 빨간색(OFF), 체크 해제하면 파란불(ON) 상태로 시작합니다.")]
    [SerializeField] private bool _isLocked = true;

    [Header("표시등 렌더러 (Sprite)")]
    [SerializeField] private SpriteRenderer _signalRenderer;

    private Color _openColor = new Color(0f, 1f, 0f); // 초록색
    private Color _lockedColor = new Color(1f, 0f, 0f); // 빨간색

    private void Start()
    {
        UpdateStatus();
    }

    #region 📌 유니티 이벤트로 호출할 Public 함수들

    // 1. 현재 상태를 반대로 뒤집는 함수 (가장 많이 쓰임)
    public void OnSwitch()
    {
        if (_isLocked == false) return; // 이미 열려 있으면 아무 작업도 하지 않음

        _isLocked = false; // 상태를 해제로 변경
        UpdateStatus();
    }
    #endregion

    // 핵심 로직: 상태에 따라 렌더러의 색상을 쨍하게 or 어둡게 변경
    private void UpdateStatus()
    {
        if ( _signalRenderer != null)
        {
             // 초록불 켜짐 상태면 쨍한 초록색, 아니면 어두운 색 적용
            _signalRenderer.color = _isLocked ? _lockedColor : _openColor;
        }
    }
}
