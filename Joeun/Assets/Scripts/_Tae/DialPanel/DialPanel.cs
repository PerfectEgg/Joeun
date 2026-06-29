using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 다이얼과 디스플레이 짝을 관리하는 패널 매니저 (선택 사항).
///  - 시작 시 각 다이얼의 onValueChanged를 짝 디스플레이에 자동 연결
///  - 현재 값들을 한곳에서 읽거나, 값 변경 시 콜백을 받을 수 있음
/// </summary>
public class DialPanel : MonoBehaviour
{
    [System.Serializable]
    public class DialPair
    {
        public DialControl    dial;
        public SegmentDisplay display;
    }

    [Header("다이얼 - 디스플레이 짝")]
    public List<DialPair> pairs = new();

    [Header("디버그")]
    public bool logChanges = false;

    void Start()
    {
        for (int i = 0; i < pairs.Count; i++)
        {
            var pair = pairs[i];
            if (pair.dial == null) continue;

            // 디스플레이 갱신 연결
            if (pair.display != null)
                pair.dial.onValueChanged.AddListener(pair.display.ShowValue);

            // 디버그 로그 연결
            if (logChanges)
            {
                int idx = i;
                pair.dial.onValueChanged.AddListener(v =>
                    Debug.Log($"[DialPanel] 다이얼 {idx} 값 = {v}"));
            }

            // 초기값 한 번 반영
            pair.dial.SetValue(pair.dial.value);
        }
    }

    /// <summary>특정 다이얼의 현재 값</summary>
    public float GetValue(int index)
    {
        if (index < 0 || index >= pairs.Count || pairs[index].dial == null) return 0f;
        return pairs[index].dial.value;
    }

    /// <summary>모든 다이얼 값 배열</summary>
    public float[] GetAllValues()
    {
        var arr = new float[pairs.Count];
        for (int i = 0; i < pairs.Count; i++)
            arr[i] = pairs[i].dial != null ? pairs[i].dial.value : 0f;
        return arr;
    }
}
