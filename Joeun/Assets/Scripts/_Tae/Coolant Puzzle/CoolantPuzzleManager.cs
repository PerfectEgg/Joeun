using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// 냉각수 밸브 퍼즐 매니저.
///
/// Phase 1 (물탱크만 가동):
///   노브 N → SUPPLY S1..SN 누적 회전(90도). S1~S4 전부 ON 시 Phase 2 진입.
/// Phase 2 (양쪽 가동):
///   노브 N → SUPPLY S1..SN 누적(90도) + RETURN RN 1개(120도).
///   SUPPLY·RETURN 동시 전부 ON 시 완성.
///
/// 인디케이터(latch, 실시간 변화 X):
///   시작 RED/RED → 1단계 통과 GREEN/RED → 2단계 통과 GREEN/GREEN
/// </summary>
public class CoolantPuzzleManager : MonoBehaviour
{
    [Header("밸브 (인덱스 0~3 순서대로)")]
    public List<CoolantValve> supplyValves = new();   // S1..S4
    public List<CoolantValve> returnValves = new();   // R1..R4

    [Header("초기 각도 (도)")]
    [Tooltip("이미지1 기준 예시: 90,0,0,90  (S1 OFF, S2 ON, S3 ON, S4 OFF)")]
    public float[] supplyStart = { 90f, 0f, 0f, 90f };
    [Tooltip("전부 OFF가 되도록. 0이 아닌 값(120/240)")]
    public float[] returnStart = { 120f, 240f, 120f, 240f };

    [Header("인디케이터")]
    public Image tankLight;
    public Image radiatorLight;
    public Color redColor   = new Color(0.75f, 0.22f, 0.17f);
    public Color greenColor = new Color(0.17f, 0.82f, 0.48f);

    [Header("이벤트")]
    public UnityEvent onStage1Cleared;   // 물 흐름 시작 (TANK GREEN)
    public UnityEvent onStage2Cleared;   // 시스템 완성 (RADIATOR GREEN)

    // ── 내부 상태 ───────────────────────────────────────────────────
    int  phase = 1;
    bool stage1 = false;   // latch
    bool stage2 = false;   // latch

    void Start()
    {
        // 밸브 초기화
        for (int i = 0; i < supplyValves.Count; i++)
            supplyValves[i].Init(i < supplyStart.Length ? supplyStart[i] : 0f);
        for (int i = 0; i < returnValves.Count; i++)
        {
            returnValves[i].Init(i < returnStart.Length ? returnStart[i] : 120f);
            returnValves[i].SetPowered(false);   // Phase 1: RETURN 미가동
        }
        UpdateLights();
    }

    /// <summary>노브 N(1~4) 조작. CoolantKnob.OnPointerClick에서 호출.</summary>
    public void OperateKnob(int n)
    {
        if (stage2) return;   // 완성 후 입력 무시 (원하면 제거)

        // SUPPLY: 1..N 누적
        for (int i = 0; i < n && i < supplyValves.Count; i++)
            supplyValves[i].Step();

        // RETURN: Phase 2일 때만, N번 1개
        if (phase == 2 && n - 1 < returnValves.Count)
            returnValves[n - 1].Step();

        Evaluate();
    }

    void Evaluate()
    {
        bool allSupply = supplyValves.All(v => v.IsOn);
        bool allReturn = returnValves.All(v => v.IsOn);

        // 1단계: SUPPLY 전부 ON → 물 흐름 + RETURN 가동
        if (!stage1 && allSupply)
        {
            stage1 = true;
            phase  = 2;
            foreach (var r in returnValves) r.SetPowered(true);
            Debug.Log("[Coolant] 1단계 통과 — SUPPLY 전부 ON, 물 흐름 시작 / RETURN 전원 ON");
            UpdateLights();
            onStage1Cleared?.Invoke();
        }

        // 2단계: 양쪽 동시 전부 ON → 완성
        if (stage1 && !stage2 && allSupply && allReturn)
        {
            stage2 = true;
            Debug.Log("[Coolant] ★ 2단계 통과 — 냉각수 시스템 정상 작동!");
            UpdateLights();
            onStage2Cleared?.Invoke();
        }

        Debug.Log($"[Coolant] phase:{phase} | SUPPLY all ON:{allSupply} | RETURN all ON:{allReturn}");
    }

    void UpdateLights()
    {
        if (tankLight != null)     tankLight.color     = stage1 ? greenColor : redColor;
        if (radiatorLight != null) radiatorLight.color = stage2 ? greenColor : redColor;
    }

    // ── 디버그 ──────────────────────────────────────────────────────
    [ContextMenu("상태 초기화")]
    public void ResetPuzzle()
    {
        phase = 1; stage1 = false; stage2 = false;
        Start();
    }
}
