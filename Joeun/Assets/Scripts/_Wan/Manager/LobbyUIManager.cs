using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

// ==========================================
// 메인 로비 UI 매니저
// 설명: 새 게임 및 이어하기 버튼 클릭을 처리하고 세이브 파일을 제어합니다.
// ==========================================
public class LobbyUIManager : MonoBehaviour
{
    [Header("UI 버튼 연결")]
    [SerializeField] private Button _newGameButton;
    [SerializeField] private Button _continueButton;

    private string _saveFilePath;

    private void Start()
    {
        // 세이브 파일이 저장되는 정확한 경로 (SaveManager와 동일한 경로)
        _saveFilePath = Path.Combine(Application.persistentDataPath, "SaveData.json");

        // 1. 게임을 켰을 때 세이브 파일이 존재하는지 확인합니다.
        if (File.Exists(_saveFilePath))
        {
            // 세이브 파일이 있다면 '이어하기' 버튼 활성화
            _continueButton.interactable = true;
            DevLog.Log("[Lobby] 기존 세이브 파일 발견. 이어하기가 활성화됩니다.");
        }
        else
        {
            // 세이브 파일이 없다면 '이어하기' 버튼 비활성화 (클릭 불가)
            _continueButton.interactable = false;
            DevLog.Log("[Lobby] 세이브 파일 없음. 새 게임만 가능합니다.");
        }
    }

    // ==========================================
    // 버튼 클릭 이벤트 연결용 함수
    // ==========================================

    /// <summary>
    /// [새 게임] 버튼을 눌렀을 때 실행됩니다.
    /// </summary>
    public void OnClickNewGame()
    {
        // 1. 기존 세이브 데이터가 있다면 가차 없이 삭제 (데이터 초기화)
        if (File.Exists(_saveFilePath))
        {
            File.Delete(_saveFilePath);
            DevLog.Log("[Lobby] 새 게임 시작: 기존 세이브 데이터를 초기화했습니다.");
        }

        // 2. 코어 씬으로 진입 (SaveManager는 데이터가 없으니 알아서 0번 스테이지를 준비합니다)
        LoadCoreScene();
    }

    /// <summary>
    /// [이어하기] 버튼을 눌렀을 때 실행됩니다.
    /// </summary>
    public void OnClickContinue()
    {
        DevLog.Log("[Lobby] 이어하기: 저장된 데이터로 코어 씬에 진입합니다.");
        
        // 데이터 삭제 없이 바로 코어 씬으로 진입
        LoadCoreScene();
    }

    // ==========================================
    // 내부 씬 전환 메서드
    // ==========================================
    private void LoadCoreScene()
    {
        // 버튼을 연타하는 것을 방지하기 위해 버튼 비활성화
        _newGameButton.interactable = false;
        _continueButton.interactable = false;

        // 코어 씬 로드 (Single 모드로 열어서 로비 씬을 메모리에서 해제)
        SceneManager.LoadScene("Stage Core");
    }
}