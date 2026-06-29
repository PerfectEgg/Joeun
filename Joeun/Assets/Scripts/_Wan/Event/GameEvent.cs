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
    // 특정 디코드 해금 했을 때
    public static Action<int> EOnDecodeOpened;

    // 모든 디코드 해금 했을 때
    public static Action EOnAllDecodeOpened;
    #endregion

    #region 📹 CCTV 관련 이벤트
    // 특정 오브젝트로 뷰를 확대했을 때
    public static Action<int> EOnCCTVZoomInView;

    // 특정 오브젝트 확대 뷰를 벗어났을 때
    public static Action EOnCCTVZoomOutView;
    #endregion

    #region 🎬 스테이지 및 씬 전환 관련 이벤트
    public static Action EStageClear;   // 스테이지 클리어 시 (전달값 없음)
    public static Action<int> ECurrentStage;   // 현재 스테이지 인덱스 전달
    public static Action<float> EFadeOut;      // 페이드 아웃
    public static Action<float> EFadeIn;       // 페이드 인
    #endregion

    #region 📣 사운드 관련 이벤트
    public static Action<string, float> EBGMPlayWithFade;   // 배경 음악 재생 시
    public static Action<float> EBGMStopWithFade;   // 배경 음악 정지 시
    public static Action<string> EBGMPlayInstantly;   // 배경 음악 재생 시
    public static Action EBGMStopInstantly;   // 배경 음악 정지 시


    public static Action<string> ESFXPlay;   // 효과음 재생 시
    #endregion

    
}