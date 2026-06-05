using System;
using UnityEngine;

// ==========================================
// 인게임 이벤트
// ==========================================
public class GameEvent
{
    #region 📦 아이템 및 인벤토리 관련 이벤트
    // 아이템을 획득했을 때 (전달값: 획득한 ItemData)
    public static Action<ItemData> EOnItemCollected;

    // 인벤토리에서 아이템을 사용/소모했을 때 (전달값: 사용한 ItemData)
    public static Action<ItemData> EOnItemUsed;
    #endregion

    #region 🚪 상호작용 및 퍼즐 관련 이벤트
    // 특정 문이나 자물쇠가 열렸을 때 (전달값: 열린 객체의 ID)
    public static Action<string> EOnLockOpened;
    
    // 퍼즐을 클리어했을 때 (전달값: 클리어한 퍼즐 ID)
    public static Action<string> EOnPuzzleSolved;
    #endregion

    #region 📹 CCTV 관련 이벤트
    // 특정 오브젝트로 뷰를 확대했을 때
    public static Action<int> EOnCCTVZoomInView;

    // 특정 오브젝트 확대 뷰를 벗어났을 때
    public static Action EOnCCTVZoomOutView;
    #endregion
}