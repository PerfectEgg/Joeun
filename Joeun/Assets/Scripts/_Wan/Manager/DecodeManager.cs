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

    [Header("기본 제공 문자 데이터 (4개)")]
    [Tooltip("처음부터 제공될 C, O, D, E 등 4개의 데이터를 넣으세요.")]
    [SerializeField] private List<DecodeData> _initialData;

    [Header("추가 해금 문자 데이터 (8개)")]
    [Tooltip("나중에 해금될 8개의 데이터를 넣으세요.")]
    [SerializeField] private List<DecodeData> _additionalData;

    // 현재 플레이어가 사용할 수 있도록 활성화된 문자들의 리스트
    private List<DecodeData> _currentActiveData = new List<DecodeData>();

    private void OnEnable()
    {
        // 버스의 OnDecodeOpened 채널 구독
        GameEvent.EOnDecodeOpened += UnlockAndSortAll;
    }

    private void OnDisable()
    {
        // 메모리 누수 방지를 위해 꼭 구독 해제
        GameEvent.EOnDecodeOpened -= UnlockAndSortAll;
    }

    private void Start()
    {
        // 1. 게임 시작 시, 처음 4개만 세팅합니다 (기획 의도대로 정렬 없이 세팅)
        _currentActiveData.Clear();
        _currentActiveData.AddRange(_initialData);
        
        RedrawSlots();
    }

    /// <summary>
    /// 퍼즐 클리어 이벤트 수신 시 나머지 8개를 추가하고 알파벳 순으로 재정렬합니다.
    /// </summary>
    private void UnlockAndSortAll()
    {
        // 1. 나머지 8개의 데이터를 리스트에 밀어 넣습니다.
        _currentActiveData.AddRange(_additionalData);

        // 2. C# 내장 Sort 기능으로 알파벳 순(A-Z)으로 전체 정렬합니다. 
        // 이렇게 하면 'O' 문자가 C, D, E를 비롯한 다른 문자 뒤로 알아서 밀려납니다.
        _currentActiveData.Sort((x, y) => x.decodeLetter.CompareTo(y.decodeLetter));

        // 3. 정렬된 리스트를 바탕으로 UI 슬롯을 다시 그립니다.
        RedrawSlots();
        
        DevLog.Log("[DecodeManager] 추가 문자 8개 해금 및 알파벳 순 정렬 완료!");
    }

    /// <summary>
    /// 현재 활성화된 데이터 리스트를 바탕으로 12칸의 슬롯을 업데이트합니다.
    /// </summary>
    private void RedrawSlots()
    {
        for (int i = 0; i < _uiSlots.Length; i++)
        {
            if (i < _currentActiveData.Count)
            {
                // 데이터가 있으면 켜기
                _uiSlots[i].Setup(_currentActiveData[i]);
            }
            else
            {
                // 없으면 빈 칸(투명) 처리
                _uiSlots[i].Clear();
            }
        }
    }
}