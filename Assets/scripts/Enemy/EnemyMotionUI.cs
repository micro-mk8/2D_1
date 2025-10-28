using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class EnemyMotionUI : MonoBehaviour
{
    public enum MovePattern
    {
        Straight,
        HorizontalSine,
        VerticalSine,
        FigureEight,
        Waypoints
    }

    [Header("基本")]
    [SerializeField] private MovePattern pattern = MovePattern.HorizontalSine;
    [SerializeField] private float speedPxPerSec = 260f;
    [Tooltip("Straight の進行方向。Horizontal/Vertical では主軸の符号に利用")]
    [SerializeField] private Vector2 direction = new Vector2(-1f, 0f);

    [Header("サイン波（Horizontal/Vertical 用）")]
    [SerializeField] private float amplitudePx = 120f;
    [SerializeField] private float frequencyHz = 0.9f;
    [SerializeField] private float phaseOffset = 0f;

    [Header("FigureEight（8の字）")]
    [SerializeField] private float eightWidthPx = 140f;
    [SerializeField] private float eightHeightPx = 90f;
    [SerializeField] private float eightSpeedHz = 0.6f;
    [SerializeField] private float eightPhase = 0f;

    [Header("Waypoints（ローカル座標。Relative の場合は初期位置からの相対）")]
    [SerializeField] private bool relativeWaypoints = true;
    [SerializeField] private List<Vector2> waypoints = new List<Vector2>();
    [SerializeField] private bool loopWaypoints = true;       // ← シーケンサ使用時は false 推奨
    [SerializeField] private float arriveEps = 2f;
    [SerializeField] private float waitAtPointSec = 0f;

    [Header("シーケンス制御（各行動→原点へ戻る→休止→次行動）")]
    [SerializeField] private bool useSequencer = true;
    [Tooltip("実行する行動パターンの並び。RandomizeがONなら毎回ランダム選択")]
    [SerializeField] private MovePattern[] sequence = new MovePattern[] { MovePattern.HorizontalSine, MovePattern.FigureEight, MovePattern.Straight };
    [SerializeField] private bool randomizeOrder = true;

    [Tooltip("各行動を何秒実行するか（Waypointsはこの秒数で打ち切ってリターン）")]
    [SerializeField, Min(0.1f)] private float patternDurationSec = 3.0f;

    [Tooltip("原点へ戻るときの速度(px/s)")]
    [SerializeField] private float returnSpeedPxPerSec = 420f;

    [Tooltip("原点へ戻ったあと、次行動に入る前の待ち秒")]
    [SerializeField] private float restAtStartSec = 0.4f;

    [Tooltip("原点到達と判定する誤差(px)")]
    [SerializeField] private float returnArriveEps = 1.5f;

    [Header("クイック作成：三角ウェイポイント（Waypoints用）")]
    [SerializeField] private float triangleRadius = 180f;   // 初期位置を内心とし、正三角の頂点まで
    [SerializeField] private float triangleRotateDeg = 0f;  // 回転
    [SerializeField] private bool overwriteWaypointsOnPlay = false;

            // ===== 初期位置キャプチャ =====
    [Header("初期位置キャプチャ")]
    [SerializeField] private bool captureStartOnPlay = true;     // 再生開始時に現在位置を初期位置として記録
    [SerializeField] private bool lockStartAfterCapture = true;  // 一度記録したら以後は上書きしない
    private bool startCaptured = false;


    [Header("速度スケール")]
    [SerializeField, Min(0f)] private float speedScale = 1f;  // ← ここを 0.1〜1.0 などで調整
    private float S(float v) => v * speedScale;

    private RectTransform rect;
    private Vector2 startPos;
    private Vector2 sineOffset;
    private float t;
    private int wpIndex;
    private float waitRemain;

    private enum SeqState { PatternRunning, Returning, Resting }
    private SeqState seq = SeqState.PatternRunning;
    private float stateTimer = 0f;
    private int seqIndex = 0;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        rect = GetComponent<RectTransform>();

        // ★ ここで現在位置を初期位置として記録
        CaptureStartIfNeeded();

        // 以降は初期位置を変えない前提で各状態を初期化
        ResetPatternInternal();

        if (overwriteWaypointsOnPlay && waypoints != null)
        {
            waypoints.Clear();
            waypoints.AddRange(BuildTriangleWaypoints(triangleRadius, triangleRotateDeg));
        }

        if (useSequencer && sequence != null && sequence.Length > 0)
            pattern = sequence[seqIndex % sequence.Length];

        seq = useSequencer ? SeqState.PatternRunning : SeqState.PatternRunning;
        stateTimer = 0f;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (!useSequencer)
        {
            RunPattern(dt);
            return;
        }

        switch (seq)
        {
            case SeqState.PatternRunning:
                stateTimer += dt;
                RunPattern(dt);
                if (stateTimer >= patternDurationSec)
                {
                    seq = SeqState.Returning;
                    stateTimer = 0f;
                }
                break;

            case SeqState.Returning:
                Vector2 pos = rect.anchoredPosition;
                Vector2 toStart = startPos - pos;
                float dist = toStart.magnitude;
                if (dist <= returnArriveEps)
                {
                    rect.anchoredPosition = startPos;
                    ResetPatternInternal();
                    seq = SeqState.Resting;
                    stateTimer = 0f;

                    if (sequence != null && sequence.Length > 0)
                    {
                        if (randomizeOrder)
                        {
                            pattern = sequence[Random.Range(0, sequence.Length)];
                        }
                        else
                        {
                            seqIndex = (seqIndex + 1) % sequence.Length;
                            pattern = sequence[seqIndex];
                        }
                    }
                    break;
                }
                Vector2 step = toStart.normalized * S(returnSpeedPxPerSec) * dt;
                if (step.magnitude > dist) step = toStart;
                rect.anchoredPosition = pos + step;
                break;

            case SeqState.Resting:
                stateTimer += dt;
                if (stateTimer >= restAtStartSec)
                {
                    // 次の行動へ
                    seq = SeqState.PatternRunning;
                    stateTimer = 0f;
                }
                break;
        }
    }

    private void RunPattern(float dt)
    {
        switch (pattern)
        {
            case MovePattern.Straight:
                MoveStraight(dt);
                break;
            case MovePattern.HorizontalSine:
                MoveHorizontalSine(dt);
                break;
            case MovePattern.VerticalSine:
                MoveVerticalSine(dt);
                break;
            case MovePattern.FigureEight:
                MoveFigureEight();
                break;
            case MovePattern.Waypoints:
                MoveWaypoints(dt);
                break;
        }
    }

    private void ResetPatternInternal()
    {
        // ★ 各行動のスタート地点は常に初期位置
        rect.anchoredPosition = startPos;
        sineOffset = Vector2.zero;
        t = 0f; wpIndex = 0; waitRemain = 0f;
    }


    private void MoveStraight(float dt)
    {
        Vector2 dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.left;
        rect.anchoredPosition += dir * S(speedPxPerSec) * dt;

    }

    private void MoveHorizontalSine(float dt)
    {
        float vx = (direction.x >= 0f ? 1f : -1f) * S(speedPxPerSec);
        float omega = 2f * Mathf.PI * frequencyHz;
        float yNow = amplitudePx * Mathf.Sin(omega * t + phaseOffset);

        Vector2 baseMove = new Vector2(vx * dt, 0f);
        Vector2 newSine = new Vector2(0f, yNow);
        Vector2 delta = baseMove + (newSine - sineOffset);

        rect.anchoredPosition += delta;
        sineOffset = newSine;
        t += dt;
    }

    private void MoveVerticalSine(float dt)
    {
        float vy = (direction.y >= 0f ? 1f : -1f) * S(speedPxPerSec);
        float omega = 2f * Mathf.PI * frequencyHz;
        float xNow = amplitudePx * Mathf.Sin(omega * t + phaseOffset);

        Vector2 baseMove = new Vector2(0f, vy * dt);
        Vector2 newSine = new Vector2(xNow, 0f);
        Vector2 delta = baseMove + (newSine - sineOffset);

        rect.anchoredPosition += delta;
        sineOffset = newSine;
        t += dt;
    }

    private void MoveFigureEight()
    {
        float w = 2f * Mathf.PI * (eightSpeedHz * speedScale); 
        float x = eightWidthPx * Mathf.Sin(w * t + eightPhase);
        float y = eightHeightPx * Mathf.Sin(2f * w * t + eightPhase);
        rect.anchoredPosition = startPos + new Vector2(x, y);
        t += Time.deltaTime;
    }

    private void MoveWaypoints(float dt)
    {
        if (waypoints == null || waypoints.Count == 0) return;

        if (waitRemain > 0f)
        {
            waitRemain -= dt;
            return;
        }

        Vector2 target = waypoints[wpIndex];
        if (relativeWaypoints) target = startPos + target;

        Vector2 pos = rect.anchoredPosition;
        Vector2 to = target - pos;
        float dist = to.magnitude;

        if (dist <= arriveEps)
        {
            wpIndex++;
            if (wpIndex >= waypoints.Count)
            {
                if (loopWaypoints) wpIndex = 0;
                else { wpIndex = waypoints.Count - 1; return; }
            }
            waitRemain = waitAtPointSec;
            return;
        }

        Vector2 step = to.normalized * S(speedPxPerSec) * dt;
        if (step.magnitude > dist) step = to;
        rect.anchoredPosition = pos + step;
    }

    private static IEnumerable<Vector2> BuildTriangleWaypoints(float r, float deg)
    {
        // ★正三角の外接円半径R = r。内心=重心=外心が一致（UI向けに簡易）
        // 0°, 120°, 240°の3頂点を返す
        float rad0 = Mathf.Deg2Rad * deg;
        for (int i = 0; i < 3; i++)
        {
            float th = rad0 + i * 2f * Mathf.PI / 3f;
            yield return new Vector2(r * Mathf.Cos(th), r * Mathf.Sin(th));
        }
    }


    // いつでも現在位置を“初期位置”として記録したい時に呼べる入口
    [ContextMenu("Capture Start From Current")]
    public void CaptureStartFromCurrent()
    {
        var rt = GetComponent<RectTransform>();
        startPos = rt.anchoredPosition;
        startCaptured = true;
    }

    private void CaptureStartIfNeeded()
    {
        if (captureStartOnPlay && (!startCaptured || !lockStartAfterCapture))
        {
            var rt = GetComponent<RectTransform>();
            startPos = rt.anchoredPosition;
            startCaptured = true;
        }
    }



}
