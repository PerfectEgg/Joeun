using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 다이얼 값을 '숫자 이미지(스프라이트)'로 자릿수별 칸에 표시합니다.
/// 0~9 스프라이트를 미리 등록해두고, 값이 바뀌면 각 칸의 Image를 갈아끼웁니다.
/// </summary>
public class SegmentDisplay : MonoBehaviour
{
    [Header("숫자 스프라이트 (0~9 순서대로 10개)")]
    public Sprite[] digitSprites = new Sprite[10];

    [Tooltip("빈 칸(앞자리 공백)에 쓸 스프라이트. 비우면 0으로 채움")]
    public Sprite blankSprite;

    [Header("자릿수 칸 (왼쪽→오른쪽 순서)")]
    [Tooltip("각 자리에 해당하는 Image들. 칸 수가 곧 표시 자리수")]
    public List<Image> digitSlots = new();

    [Header("표시 형식")]
    [Tooltip("true면 앞자리를 0으로 채움, false면 빈 칸(blankSprite)")]
    public bool padWithZeros = true;

    void Awake()
    {
        ShowValue(0f);
    }

    /// <summary>DialControl.onValueChanged(float)에 연결</summary>
    public void ShowValue(float v)
    {
        int slots = digitSlots.Count;
        if (slots == 0) return;

        int iv = Mathf.Max(0, Mathf.RoundToInt(v));
        string s = iv.ToString();

        // 자리수보다 길면 뒷자리(낮은 자리)부터 우선 표시
        if (s.Length > slots)
            s = s.Substring(s.Length - slots);

        // 오른쪽 정렬: 칸은 왼쪽→오른쪽, 값은 뒤에서부터 채움
        int pad = slots - s.Length;

        for (int i = 0; i < slots; i++)
        {
            Image slot = digitSlots[i];
            if (slot == null) continue;

            if (i < pad)
            {
                // 앞쪽 빈 자리
                if (padWithZeros)
                    slot.sprite = GetDigit(0);
                else
                    slot.sprite = blankSprite;
                slot.enabled = (slot.sprite != null);
            }
            else
            {
                int digit = s[i - pad] - '0';
                slot.sprite = GetDigit(digit);
                slot.enabled = (slot.sprite != null);
            }
        }
    }

    Sprite GetDigit(int d)
    {
        if (d >= 0 && d < digitSprites.Length) return digitSprites[d];
        return null;
    }
}