using System.Collections.Generic;
using System;

// ==========================================
// 게임 세이브 데이터 클래스
// 설명: 인게임 정보를 저장합니다. (ScriptableObject를 적용하기 위해 Class로 구현)
// ==========================================
[Serializable]
public class GameSaveData
{
    // 1. 현재 스테이지 번호 (또는 씬 이름)
    public int currentStageIndex = 0; 
    
    // 2. 보유 중인 스킬 ID 목록
    public int currentSkillIndex = 0; 
}