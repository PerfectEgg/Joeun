using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// ==========================================
// 스테이지 로드 매니저 클래스
// 설명: 각 스테이지를 Additive 방식으로 로드를 관리하는 매니저 클래스입니다.
// ==========================================
public class StageLoadManager : MonoBehaviour
{
    [Header("스테이지 리스트 설정")]
    [Tooltip("여기에 플레이할 스테이지 씬 이름들을 순서대로 적어주세요.")]
    [SerializeField] private List<string> _stageList = new List<string>();

    [Header("디버그/상태 (확인용)")]
    [SerializeField] private int _currentStageIndex = 0; // 현재 플레이 중인 스테이지의 리스트 번호

    private string _currentLoadedStage = "";
    private bool _isTransitioning = false;

    private void OnEnable()
    {
        // 싱글톤 대신 이벤트 리스너를 등록하여 신호를 대기합니다.
        GameEvent.EStageClear += HandleStageLoadRequest;
    }

    private void OnDisable()
    {
        GameEvent.EStageClear -= HandleStageLoadRequest;
    }

    private void Start()
    {
        // 리스트가 비어있지 않다면, 0번 인덱스의 스테이지를 로드하며 게임 시작
        if (_stageList.Count > 0)
        {
            _currentStageIndex = 0;
            StartCoroutine(LoadStageRoutine(_stageList[_currentStageIndex]));
        }
        else
        {
            DevLog.LogWarning("스테이지 리스트가 비어있습니다! 인스펙터를 확인하세요.");
        }
    }

    // ★ 핵심: 이제 이름을 몰라도 알아서 '다음' 스테이지로 넘어갑니다.
    public void HandleStageLoadRequest()
    {
        if (_isTransitioning) return;

        // 리스트의 마지막 스테이지인지 검사
        if (_currentStageIndex + 1 < _stageList.Count)
        {
            _currentStageIndex++; // 다음 번호로 이동
            string nextStageName = _stageList[_currentStageIndex];
            StartCoroutine(TransitionRoutine(nextStageName));
        }
        else
        {
            // 준비된 스테이지를 모두 클리어했을 때의 엔딩 처리
            DevLog.Log("모든 스테이지를 클리어했습니다! 엔딩 씬으로 이동하거나 UI를 띄웁니다.");
            // 예: SceneManager.LoadScene("Scene_Ending");
        }
    }

    // 씬 교체 연출 및 연산을 처리하는 핵심 코루틴
    private IEnumerator TransitionRoutine(string nextStageName)
    {
        _isTransitioning = true;

        // 1. 화면 페이드 아웃 (암전 처리)
        // UIManager.Instance.FadeOut(1.0f);
        yield return new WaitForSeconds(1.0f); // 페이드 아웃 애니메이션 시간 대기

        // 2. 기존 스테이지 씬 언로드 (메모리 해제)
        if (!string.IsNullOrEmpty(_currentLoadedStage))
        {
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(_currentLoadedStage);
            while (!unloadOp.isDone)
            {
                yield return null; // 언로드가 끝날 때까지 대기
            }
            DevLog.Log($"[Core] {_currentLoadedStage} 언로드 완료 및 메모리 회수");
        }

        // 3. 새 스테이지 씬 로드
        yield return StartCoroutine(LoadStageRoutine(nextStageName));

        // 4. 화면 페이드 인 (새 화면 보여주기)
        // UIManager.Instance.FadeIn(1.0f);

        _isTransitioning = false;
    }

    // 순수하게 씬을 부르고 활성화하는 서브 코루틴
    private IEnumerator LoadStageRoutine(string stageName)
    {
        // ★ 핵심: LoadSceneMode.Additive로 설정하여 기존 Core 씬을 유지한 채 겹쳐서 켭니다.
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(stageName, LoadSceneMode.Additive);
        
        while (!loadOp.isDone)
        {
            yield return null; // 로딩이 끝날 때까지 대기
        }

        _currentLoadedStage = stageName;
        DevLog.Log($"[Core] {stageName} Additive 로드 완료");

        // 로드된 스테이지 씬을 'Active Scene'으로 설정 (새로 생성되는 오브젝트들이 이 씬으로 들어가도록 설정)
        Scene newlyLoadedScene = SceneManager.GetSceneByName(stageName);
        SceneManager.SetActiveScene(newlyLoadedScene);
    }
}