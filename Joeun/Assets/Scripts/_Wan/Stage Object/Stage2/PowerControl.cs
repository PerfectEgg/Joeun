using UnityEngine;
using TMPro;

// ==========================================
// 파워 컨트롤 클래스
// 설명: 2스테이지 오브젝트인 파워 컨트롤에 대한 클래스입니다.
// ==========================================
public class PowerControl : MonoBehaviour
{
    [Header("상태 표시 (TMP)")]
    [SerializeField] private TextMeshProUGUI _powerStatusText;
    [SerializeField] private TextMeshProUGUI _switchboardText;
    [SerializeField] private TextMeshProUGUI _coolerText;
    [SerializeField] private TextMeshProUGUI _transformerText;

    private bool _powerStatusState = false; // 초기 상태는 빨간불(OFF)으로 시작
    private bool _switchboardState = false; // 초기 상태는 빨간불(OFF)으로 시작
    private bool _coolerState = false;      // 초기 상태는 빨간불(OFF)으로 시작
    private bool _transformerState = false; // 초기 상태는 빨간불(OFF)으로 시작

    private string _activeText = "ON";
    private string _inactiveText = "FAIL";

    private Color _activeColor = new Color(0f, 1f, 0f); // 초록색
    private Color _inactiveColor = new Color(1f, 0f, 0f, 1f);   // 빨간색

    private void Start()
    {
        if (_powerStatusText != null) UpdateState(_powerStatusText, _powerStatusState);
        if (_switchboardText != null) UpdateState(_switchboardText, _switchboardState);
        if (_coolerText != null) UpdateState(_coolerText, _coolerState);
        if (_transformerText != null) UpdateState(_transformerText, _transformerState);
    }

    #region 📌 유니티 이벤트로 호출할 Public 함수들

    // 현재 상태를 반대로 뒤집는 함수 (가장 많이 쓰임)
    public void OnSwitchPowerStatus()
    {
        if (_powerStatusState == true) return; // 이미 초록불이 켜져 있으면 아무 작업도 하지 않음
    
        _powerStatusState = true; // 초록불 켜짐
        UpdateState(_powerStatusText, _powerStatusState);
    }
    
    public void OnSwitchSwitchboard()
    {
        if (_switchboardState == true) return; // 이미 초록불이 켜져 있으면 아무 작업도 하지 않음
    
        _switchboardState = true; // 초록불 켜짐
        UpdateState(_switchboardText, _switchboardState);
    }

    public void OnSwitchCooler()
    {
        if (_coolerState == true) return; // 이미 초록불이 켜져 있으면 아무 작업도 하지 않음
    
        _coolerState = true; // 초록불 켜짐
        UpdateState(_coolerText, _coolerState);
    }

    public void OnSwitchTransformer()
    {
        if (_transformerState == true) return; // 이미 초록불이 켜져 있으면 아무 작업도 하지 않음
    
        _transformerState = true; // 초록불 켜짐
        UpdateState(_transformerText, _transformerState);
    }
    #endregion

    // 핵심 로직: 상태에 따라 렌더러의 색상을 쨍하게 or 어둡게 변경
    private void UpdateState(TextMeshProUGUI textComponent, bool state)
    {
        if (textComponent != null)
        {
            textComponent.text = state ? _activeText : _inactiveText;
            textComponent.color = state ? _activeColor : _inactiveColor;
        }
    }
}
