using UnityEngine;
using System;
using System.Collections.Generic;


// ==========================================
// 디코드 매니저 클래스
// 설명: 해석을 위한 알파벳을 관리합니다.
// ==========================================
public class DecodeManager : MonoBehaviour
{
    [Header("고정 슬롯 설정")]
    [Tooltip("에디터에서 12개의 UIDecode 스크립트를 순서대로 넣어주세요.")]
    [SerializeField] private UIDecode[] _uiSlots = new UIDecode[12];

    [Header("정해진 문자 데이터 (12개)")]
    [Tooltip("패널에 표시될 12개의 문자를 순서대로 넣으세요.")]
    [SerializeField] private DecodeData[] _presetData = new DecodeData[12];

    [Header("초기 활성화 인덱스")]
    [Tooltip("게임 시작 시 처음부터 흰색으로 열려있을 슬롯의 번호(0~11)를 적으세요.")]
    [SerializeField] private List<int> _initialUnlockedIndices = new List<int> { 1, 2, 3, 8};

    // 현재 플레이어가 사용할 수 있도록 활성화된 문자들의 리스트
    private List<DecodeData> _currentActiveData = new List<DecodeData>();

    private void OnEnable()
    {
        // 버스의 OnDecodeOpened 채널 구독
        GameEvent.EOnDecodeOpened += UnlockDecodePanel;
        GameEvent.EOnAllDecodeOpened += UnlockAllDecodePanel;
    }

    private void OnDisable()
    {
        // 메모리 누수 방지를 위해 꼭 구독 해제
        GameEvent.EOnDecodeOpened -= UnlockDecodePanel;
        GameEvent.EOnAllDecodeOpened -= UnlockAllDecodePanel;
    }

    private void Start()
    {
        // 1. 게임 시작 시, 처음 4개만 세팅합니다 (기획 의도대로 정렬 없이 세팅)
        InitializeDecodePanel();
    }

    /// <summary>
    /// 12개의 문자를 일괄 배치하고, 초기 해금 상태를 지정합니다.
    /// </summary>
    private void InitializeDecodePanel()
    {
        for (int i = 0; i < _uiSlots.Length; i++)
        {
            if (i < _presetData.Length && _presetData[i] != null)
            {
                // _initialUnlockedIndices 리스트에 포함된 번호만 true, 나머지는 false로 세팅
                bool isUnlockedAtStart = _initialUnlockedIndices.Contains(i);
                _uiSlots[i].Setup(_presetData[i], isUnlockedAtStart);
            }
        }
    }

    /// <summary>
    /// 퍼즐 클리어 이벤트 수신 시 나머지 8개를 추가하고 알파벳 순으로 재정렬합니다.
    /// </summary>
    private void UnlockDecodePanel(int index)
    {
        if (index >= 0 && index < _uiSlots.Length)
        {
            _uiSlots[index].SetUnlockState(true);
            DevLog.Log($"[DecodeManager] {index}번 슬롯 해금 완료!");
        }
    }

    /// <summary>
    /// 남은 모든 슬롯을 한 번에 해금합니다.
    /// </summary>
    public void UnlockAllDecodePanel()
    {
        for (int i = 0; i < _uiSlots.Length; i++)
        {
            _uiSlots[i].SetUnlockState(true);
        }
        DevLog.Log("[DecodeManager] 모든 디코드 문자 해금 완료!");
    }
}