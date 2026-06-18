using System.Collections.Generic;
using UnityEngine;
using System.IO;

// ==========================================
// 세이브 매니저 클래스
// 설명: 인게임 정보를 저장하는 클래스입니다.
// ==========================================
public class SaveManager : SingletonCore<SaveManager>
{
    private GameSaveData _saveData = new GameSaveData();
    private string _saveFilePath;

    protected override void Awake()
    {
        base.Awake();
        _saveFilePath = Path.Combine(Application.persistentDataPath, "SaveData.json");
        LoadGame(); // 게임 시작 시 무조건 한 번 로드
    }

    // ==========================================
    // 데이터 읽기/쓰기 (다른 스크립트에서 자유롭게 사용)
    // ==========================================

    public int CurrentStageIndex 
    {
        get => _saveData.currentStageIndex;
        set => _saveData.currentStageIndex = value;
    }

    public int CurrentSkillIndex
    {
        get => _saveData.currentSkillIndex;
        set => _saveData.currentSkillIndex = value;
    } 

    // ==========================================
    // 실제 파일 I/O (원할 때만 호출)
    // ==========================================

    /// <summary>
    /// 현재 메모리에 있는 상태를 실제 파일로 굽습니다.
    /// (스테이지 클리어 직후나 중요한 스킬 획득 직후에 호출)
    /// </summary>
    /// <summary>
    /// EStageClear 이벤트가 터졌을 때 자동으로 실행됨
    /// </summary>
    public void StageClear()
    {
        // 스테이지를 깼으니, 다음 스테이지 번호로 올리고 저장합니다.
        // (만약 StageLoadManager에서 Index를 관리한다면 이 줄은 빼셔도 됩니다)

        DevLog.Log("[SaveManager] 스테이지 클리어! 현재 진행 상황을 디스크에 기록합니다.");
        SaveGame(); 
    }

    private void SaveGame()
    {
        try
        {
            string json = JsonUtility.ToJson(_saveData, true);
            File.WriteAllText(_saveFilePath, json);
            DevLog.Log($"[SaveManager] 💾 세이브 완료 (스테이지: {_saveData.currentStageIndex}, 스킬: {_saveData.currentSkillIndex})");
        }
        catch (System.Exception e)
        {
            DevLog.LogError($"[SaveManager] 세이브 실패: {e.Message}");
        }
    }

    public void LoadGame()
    {
        if (File.Exists(_saveFilePath))
        {
            string json = File.ReadAllText(_saveFilePath);
            _saveData = JsonUtility.FromJson<GameSaveData>(json);
        }
        else
        {
            _saveData = new GameSaveData();
        }
    }

    public bool HasSaveData()
    {
        return File.Exists(_saveFilePath);
    }

    public void DeleteSaveData()
    {
        if (File.Exists(_saveFilePath))
        {
            File.Delete(_saveFilePath);
            _saveData = new GameSaveData();
            DevLog.Log("[SaveManager] 세이브 데이터 삭제 완료");
        }
    }
}