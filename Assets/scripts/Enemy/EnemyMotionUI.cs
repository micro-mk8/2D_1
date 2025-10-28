using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class EnemyMotionUI : MonoBehaviour
{
    public enum MovePattern
    {
        Straight,     // 直線
        FigureEight,  // 8の字
        Triangle      // 正三角（ウェイポイント）
    }

    [Header("初期位置の記録")]
    [SerializeField] private bool captureStartOnPlay = true;   // 再生時に現在位置を初期位置として記録
    [SerializeField] private bool lockStartAfterCapture = true;// 一度記録したら上書きしない
    private bool startCaptured = false;
    private Vector2 startPos;

    [Header("グローバル速度スケール")]
    [SerializeField, Min(0f)] private float speedScale = 1f;   // 全パターンの速度係数（0.1～1.0推奨）
    private float S(float v) => v * speedScale;

    [Header("シーケンス")]
    [SerializeField] private bool useSequencer = true;
    [SerializeField] private MovePattern[] sequence = new MovePattern[] { MovePattern.Triangle, MovePattern.FigureEight, MovePattern.Straight };
    [SerializeField] private bool randomizeOrder = false;
    [SerializeField, Min(0.1f)] private float patternDurationSec = 3.0f;  // 各行動を実行する秒数
    [SerializeField, Min(0f)] private float restAtStartSec = 0.4f;        // 初期位置へ戻った後の小休止
    [SerializeField, Min(1f)] private float returnSpeedPxPerSec = 420f;   // 初期位置へ戻る速さ
    [SerializeField, Min(0.1f)] private float returnArriveEps = 1.5f;     // 初期位置到達判定

    [Header("直線パターン")]
    [SerializeField] private Vector2 straightDirection = new Vector2(-1f, 0f); // 正規化されます
    [SerializeField, Min(0f)] private float straightSpeedPxPerSec = 260f;

    [Header("8の字パターン（中心＝初期位置）")]
    [SerializeField, Min(0f)] private float eightWidthPx = 140f;
    [SerializeField, Min(0f)] private float eightHeightPx = 90f;
    [SerializeField, Min(0f)] private float eightSpeedHz = 0.6f;
    [SerializeField] private float eightPhase = 0f;

    [Header("三角パターン（中心＝初期位置の内心）")]
    [SerializeField, Min(0f)] private float triangleRadius = 180f; // 初期位置から頂点までの距離
    [SerializeField] private float triangleRotateDeg = 0f;          // 三角の回転
    [SerializeField, Min(0f)] private float triangleMoveSpeedPxPerSec = 260f;
    [SerializeField, Min(0f)] private float waypointArriveEps = 2f;

    private RectTransform rect;

    // シーケンサ状態
    private enum SeqState { PatternRunning, Returning, Resting }
    private SeqState seq = SeqState.PatternRunning;
    private float stateTimer = 0f;
    private int seqIndex = 0;

    // ランタイム用
    private float t = 0f;                       // 8の字の位相時間
    private List<Vector2> triPoints;            // 三角のワールド(UI)位置
    private int triIndex = 0;                   // 現在の三角ターゲット

    // ===== ライフサイクル =====
    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        CaptureStartIfNeeded();
        PrepareTrianglePoints();
        ResetForNewPattern();                   // 位置/時間の初期化

        // 初回パターン選択
        if (useSequencer && sequence != null && sequence.Length > 0)
            currentPattern = sequence[seqIndex % sequence.Length];

        seq = SeqState.PatternRunning;
        stateTimer = 0f;
    }

    // ===== メイン更新 =====
    private MovePattern currentPattern = MovePattern.Straight;

    private void Update()
    {
        float dt = Time.deltaTime;

        if (!useSequencer)
        {
            RunPattern(currentPattern, dt);
            return;
        }

        switch (seq)
        {
            case SeqState.PatternRunning:
                stateTimer += dt;
                RunPattern(currentPattern, dt);
                if (stateTimer >= patternDurationSec)
                {
                    seq = SeqState.Returning;
                    stateTimer = 0f;
                }
                break;

            case SeqState.Returning:
                ReturnToStart(dt);
                break;

            case SeqState.Resting:
                stateTimer += dt;
                if (stateTimer >= restAtStartSec)
                {
                    // 次のパターンへ
                    SelectNextPattern();
                    ResetForNewPattern();
                    seq = SeqState.PatternRunning;
                    stateTimer = 0f;
                }
                break;
        }
    }

    // ===== パターン実行 =====
    private void RunPattern(MovePattern p, float dt)
    {
        switch (p)
        {
            case MovePattern.Straight:
                {
                    var dir = straightDirection.sqrMagnitude > 0f ? straightDirection.normalized : Vector2.left;
                    rect.anchoredPosition += dir * S(straightSpeedPxPerSec) * dt;
                }
                break;

            case MovePattern.FigureEight:
                {
                    float w = 2f * Mathf.PI * (eightSpeedHz * speedScale); // 速度スケールをHzに反映
                    float x = eightWidthPx * Mathf.Sin(w * t + eightPhase);
                    float y = eightHeightPx * Mathf.Sin(2f * w * t + eightPhase);
                    rect.anchoredPosition = startPos + new Vector2(x, y);
                    t += dt;
                }
                break;

            case MovePattern.Triangle:
                {
                    if (triPoints == null || triPoints.Count < 3) PrepareTrianglePoints();
                    Vector2 target = triPoints[triIndex];
                    Vector2 pos = rect.anchoredPosition;
                    Vector2 to = target - pos;
                    float dist = to.magnitude;

                    if (dist <= waypointArriveEps)
                    {
                        triIndex = (triIndex + 1) % 3;
                    }
                    else
                    {
                        Vector2 step = to.normalized * S(triangleMoveSpeedPxPerSec) * dt;
                        if (step.magnitude > dist) step = to;
                        rect.anchoredPosition = pos + step;
                    }
                }
                break;
        }
    }

    // ===== リターン（必ず初期位置へ直帰） =====
    private void ReturnToStart(float dt)
    {
        Vector2 pos = rect.anchoredPosition;
        Vector2 toStart = startPos - pos;
        float dist = toStart.magnitude;

        if (dist <= returnArriveEps)
        {
            rect.anchoredPosition = startPos;
            seq = SeqState.Resting;
            stateTimer = 0f;
            return;
        }

        Vector2 step = toStart.normalized * S(returnSpeedPxPerSec) * dt;
        if (step.magnitude > dist) step = toStart;
        rect.anchoredPosition = pos + step;
    }

    // ===== ユーティリティ =====
    [ContextMenu("Capture Start From Current")]
    public void CaptureStartFromCurrent()
    {
        startPos = GetComponent<RectTransform>().anchoredPosition;
        startCaptured = true;
    }

    private void CaptureStartIfNeeded()
    {
        if (captureStartOnPlay && (!startCaptured || !lockStartAfterCapture))
        {
            startPos = GetComponent<RectTransform>().anchoredPosition;
            startCaptured = true;
        }
    }

    private void PrepareTrianglePoints()
    {
        triPoints = new List<Vector2>(3);
        triPoints.Clear();

        float rad0 = Mathf.Deg2Rad * triangleRotateDeg;
        for (int i = 0; i < 3; i++)
        {
            float th = rad0 + i * 2f * Mathf.PI / 3f;
            Vector2 p = startPos + new Vector2(triangleRadius * Mathf.Cos(th), triangleRadius * Mathf.Sin(th));
            triPoints.Add(p);
        }
        triIndex = 0;
    }

    private void ResetForNewPattern()
    {
        rect.anchoredPosition = startPos;
        t = 0f;
        triIndex = 0;
    }

    private void SelectNextPattern()
    {
        if (sequence == null || sequence.Length == 0)
        {
            currentPattern = MovePattern.Straight;
            return;
        }

        if (randomizeOrder)
        {
            currentPattern = sequence[Random.Range(0, sequence.Length)];
        }
        else
        {
            seqIndex = (seqIndex + 1) % sequence.Length;
            currentPattern = sequence[seqIndex];
        }
    }

    public void ResetToStartForRetry()
    {
        // 初期位置と図形を再準備して、内部状態を初期化
        CaptureStartIfNeeded();
        PrepareTrianglePoints();   // 三角を使わない場合でも安全
        rect.anchoredPosition = startPos;
        t = 0f;
        triIndex = 0;
    }

}
