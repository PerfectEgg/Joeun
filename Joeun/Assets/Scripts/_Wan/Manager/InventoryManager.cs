using UnityEngine;
using System;
using System.Collections.Generic;

// ==========================================
// 인벤토리 매니저 클래스
// 설명: 플레이어의 인벤토리를 관리합니다.
// ==========================================
public class InventoryManager : MonoBehaviour
{
    [Header("고정 슬롯 설정")]
    [Tooltip("에디터에서 8개의 UIInventoryItem 스크립트를 순서대로 넣어주세요.")]
    [SerializeField] private UIInventoryItem[] _uiSlots = new UIInventoryItem[8];

    public List<ItemData> inventoryList = new List<ItemData>();
    
    private void OnEnable()
    {
        // 버스의 OnItemCollected 채널 구독
        GameEvent.EOnItemCollected += AddItemToInventory;
        // 버스의 OnItemUsed 채널 구독 (아이템 사용 시 인벤토리에서 제거)
        GameEvent.EOnItemUsed += RemoveItemFromInventory;
    }

    private void OnDisable()
    {
        // 메모리 누수 방지를 위해 꼭 구독 해제
        GameEvent.EOnItemCollected -= AddItemToInventory;
        GameEvent.EOnItemUsed -= RemoveItemFromInventory;
    }

    private void AddItemToInventory(ItemData newData)
    {
        // 8칸 제한 체크
        if (inventoryList.Count >= _uiSlots.Length)
        {
            DevLog.LogWarning("인벤토리가 가득 찼습니다!");
            return;
        }

        // 리스트에 추가하고 다시 그리기
        inventoryList.Add(newData);
        DevLog.Log($"[InventoryManager] {newData.itemName} 추가 완료.");
        
        RedrawInventory(inventoryList);
    }

    private void RemoveItemFromInventory(ItemData item)
    {
        // 리스트에서 제거하고 다시 그리기 (이때 자동으로 뒤에 있던 아이템들이 앞 칸으로 당겨집니다)
        inventoryList.Remove(item);
        DevLog.Log($"[InventoryManager] {item.itemName} 사용 완료 및 정렬.");
        
        RedrawInventory(inventoryList);
    }

    /// <summary>
    /// 실제 데이터 리스트를 바탕으로 8칸의 슬롯을 처음부터 다시 그립니다. (자동 정렬의 핵심)
    /// </summary>
    public void RedrawInventory(List<ItemData> inventoryList)
    {
        for (int i = 0; i < _uiSlots.Length; i++)
        {
            // 리스트에 데이터가 있는 칸이면 아이템 세팅
            if (i < inventoryList.Count)
            {
                _uiSlots[i].Setup(inventoryList[i]);
            }
            // 리스트 범위를 벗어난 칸(빈 칸)이면 투명하게 초기화
            else
            {
                _uiSlots[i].Clear();
            }
        }
    }
}