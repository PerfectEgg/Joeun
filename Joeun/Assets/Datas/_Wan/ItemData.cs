using UnityEngine;

// ==========================================
// 아이템 데이터 클래스
// 설명: 아이템의 기본 정보를 저장합니다. (ScriptableObject를 적용하기 위해 Class로 구현)
// ==========================================
[CreateAssetMenu(fileName = "ItemData", menuName = "Game Data/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("기본 정보")]
    public string itemID;          // 아이템 고유 ID (예: "Key_Silver")
    public string itemName;        // 인게임에 표시될 이름
    [TextArea]
    public string description;     // 조사 시 출력될 텍스트
    
    [Header("UI 시각 자료")]
    public Sprite itemIcon;        // 인벤토리에 들어갈 픽셀 아트 아이콘

    [Header("UI Feedback")]
    [Min(0.1f)] public float inventoryHoverScaleMultiplier = 1.2f;
    [Min(0.1f)] public float inventoryDragScaleMultiplier = 1.35f;
}
