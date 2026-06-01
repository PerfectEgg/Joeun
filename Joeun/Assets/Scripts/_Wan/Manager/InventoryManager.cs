using UnityEngine;
using System;
using System.Collections.Generic;


// ==========================================
// 인벤토리 매니저 클래스
// 설명: 플레이어의 인벤토리를 관리합니다.
// ==========================================
public class InventoryManager : MonoBehaviour
{
    [Header("UI 연결 설정")]
    [Tooltip("아이템 UI가 생성될 부모 패널 (예: InventoryPanel)")]
    public Transform inventoryUIContent; 
    
    [Tooltip("아까 만든 UI_ItemPrefab을 여기에 끌어다 넣으세요")]
    public GameObject uiItemPrefab;

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
        inventoryList.Add(newData);
        DevLog.Log($"[InventoryManager] {newData.itemName} 데이터 수신 완료. UI를 생성합니다.");

        // 1. UI 프리팹을 inventoryUIContent(패널) 자식으로 생성
        GameObject newUIObj = Instantiate(uiItemPrefab, inventoryUIContent);
        
        // 2. 생성된 UI 스크립트에 데이터(이미지, ID 등)를 넘겨줌
        if (newUIObj.TryGetComponent(out UIInventoryItem uiItem))
        {
            uiItem.Setup(newData);
        }
    }

    private void RemoveItemFromInventory(ItemData item)
    {
        inventoryList.Remove(item);
        DevLog.Log($"[InventoryManager] {item.itemName} 데이터 제거. UI를 업데이트합니다.");
    }
}