using UnityEngine;
using UnityEngine.Events;
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
    [SerializeField] private TextMeshProUGUI _UPSText;

    [Header("모든 상태가 ON(초록불)으로 켜졌을 때 UPS 시스템이 READY(파란불)로 전환되는 이벤트")]
    public UnityEvent OnReadyUPSEvent;

    private bool _powerStatusState = false; // 초기 상태는 빨간불(OFF)으로 시작
    private bool _switchboardState = false; // 초기 상태는 빨간불(OFF)으로 시작
    private bool _coolerState = false;      // 초기 상태는 빨간불(OFF)으로 시작
    private bool _transformerState = false; // 초기 상태는 빨간불(OFF)으로 시작
    private bool _UPSSystem = true;          // 초기 상태는 초록불(ON)으로 시작 (UPS 시스템은 항상 켜져 있어야 하므로 초기값 true로 설정)

    private string _activeText = "ON";
    private string _inactiveText = "FAIL";
    private string _readyText = "READY";

    private Color _activeColor = new Color(0f, 1f, 0f); // 초록색
    private Color _inactiveColor = new Color(1f, 0f, 0f, 1f);   // 빨간색
    private Color _readyColor = new Color(0f, 0f, 1f, 1f); // 파란색

    private void Start()
    {
        if (_powerStatusText != null) UpdateState(_powerStatusText, _powerStatusState);
        if (_switchboardText != null) UpdateState(_switchboardText, _switchboardState);
        if (_coolerText != null) UpdateState(_coolerText, _coolerState);
        if (_transformerText != null) UpdateState(_transformerText, _transformerState);
        if (_UPSText != null) UpdateState(_UPSText, _UPSSystem);
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
    
    private void CheckAllStates()
    {
        // 모든 상태가 ON(초록불)으로 켜져 있는지 확인
        if (_powerStatusState && _switchboardState && _coolerState && _transformerState && _UPSSystem)
        {
            DevLog.Log("모든 시스템이 정상적으로 작동합니다! UPS 시스템이 READY 상태로 전환됩니다.");
            OnReadyUPS(); // UPS 시스템을 READY 상태로 업데이트
        }
    }

    private void OnReadyUPS()
    {
        if (_UPSSystem == false) return; // 이미 파란불이 켜져 있으면 아무 작업도 하지 않음

        _UPSSystem = false; // 파란불 켜짐
        UpdateUPSState(_UPSText, _UPSSystem);
        OnReadyUPSEvent?.Invoke(); // READY 이벤트 발행
    }

    // 핵심 로직: 상태에 따라 렌더러의 색상을 쨍하게 or 어둡게 변경
    private void UpdateState(TextMeshProUGUI textComponent, bool state)
    {
        if (textComponent != null)
        {
            textComponent.text = state ? _activeText : _inactiveText;
            textComponent.color = state ? _activeColor : _inactiveColor;
        }

        CheckAllStates();
    }

    // 핵심 로직: UPS 시스템은 ON 상태에서 READY 상태로 바뀌는 특수한 경우이므로 별도의 함수로 처리
    private void UpdateUPSState(TextMeshProUGUI textComponent, bool state)
    {
        if (textComponent != null)
        {
            textComponent.text = state ? _activeText : _readyText;
            textComponent.color = state ? _activeColor : _readyColor;
        }
    }
}
