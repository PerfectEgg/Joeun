using UnityEngine;

// ==========================================
// 디코드 데이터 클래스
// 설명: 디코드의 기본 정보를 저장합니다. (ScriptableObject를 적용하기 위해 Class로 구현)
// ==========================================
[CreateAssetMenu(fileName = "DecodeData", menuName = "Game Data/Decode Data")]
public class DecodeData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("퍼즐 타겟에 전달될 실제 문자 (예: C, O, D, E)")]
    public char decodeLetter;
    
    [Header("UI 시각 자료")]
    public Sprite decodeIcon;        // 인벤토리에 들어갈 픽셀 아트 아이콘
}