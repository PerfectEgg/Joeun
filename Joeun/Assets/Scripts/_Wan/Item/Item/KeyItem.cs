using UnityEngine;

// ==========================================
// 열쇠 아이템 클래스
// ==========================================
public class KeyItem : InteractiveItem
{
    #region IInteractive 구현
    public override void Interact()
    {
        if (!IsAcquired)
        {
            DevLog.Log($"[{_itemData.itemName}] 발견! 획득을 진행합니다.");
            
            // Item 클래스에 있는 획득 로직 실행
            Collect();

            GameEvent.ESFXPlay?.Invoke("Acquired_Item");
        }
    }
    #endregion
}