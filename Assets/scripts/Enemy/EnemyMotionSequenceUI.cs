// ★ 差し替え版（必要部分だけ変更・追加） ★
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class EnemyMotionSequenceUI : MonoBehaviour
{
    public enum MovePattern { Straight, HorizontalSine, VerticalSine, FigureEight }

    [Serializable]
    public struct Segment
    {
        public MovePattern pattern;
        public float durationSec;
        public float pauseAfterSec;

        public float speedPxPerSec;
        public Vector2 direction;

        public float amplitudePx;
        public float frequencyHz;
        public float phaseOffset;

        public float eightWidthPx;
        public float eightHeightPx;
        public float eightSpeedHz;
        public float eightPhase;
    }

    [Header("シーケンス")]
    [SerializeField] private List<Segment> segments = new();
    [SerializeField] private bool loopSequence = true;

    [Header("壁当たり停止（端で外向き成分を0にする）")]
    [SerializeField] private bool wallStop = true;
    [Tooltip("UIRectClamp の Padding と同じか少し小さく")]
    [SerializeField] private Vector2 wallInnerPadding = new(8f, 8f);
    [Tooltip("端に接しているとみなす許容（px）")]
    [SerializeField] private float wallEpsPx = 0.5f;

    private RectTransform rect;
    private RectTransform container;             // PlayAreaFrame
    private static readonly Vector3[] C = new Vector3[4];
    private static readonly Vector3[] T = new Vector3[4];

    private int idx = -1;
    private float segTime = 0f;
    private float pauseRemain = 0f;

    private Vector2 originPos;
    private Vector2 sineOffset;
    private float t = 0f;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        // UIRectClamp から container を拝借（推奨の付け方）
        var clamp = GetComponent<UIRectClamp>();
        if (clamp != null)
        {
            // リフレクション不要：公開フィールドなので取得可能な前提
            // もし private なら、UIRectClamp に public getter を用意してください。
            var fi = typeof(UIRectClamp).GetField("container", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            container = fi?.GetValue(clamp) as RectTransform;
        }
    }

    void OnEnable()
    {
        if (segments.Count > 0) BeginSegment(0);
    }

    void Update()
    {
        if (segments.Count == 0 || idx < 0) return;

        float dt = Time.deltaTime;

        if (pauseRemain > 0f)
        {
            pauseRemain -= dt;
            if (pauseRemain <= 0f) Advance();
            return;
        }

        var s = segments[idx];
        segTime += dt;
        t += dt;

        switch (s.pattern)
        {
            case MovePattern.Straight:
                {
                    Vector2 dir = s.direction.sqrMagnitude > 0f ? s.direction.normalized : Vector2.left;
                    Vector2 delta = dir * s.speedPxPerSec * dt;
                    delta = ApplyWallStop(delta);
                    rect.anchoredPosition += delta;
                    break;
                }
            case MovePattern.HorizontalSine:
                {
                    float vx = (s.direction.x >= 0f ? 1f : -1f) * Mathf.Abs(s.speedPxPerSec);
                    float omega = 2f * Mathf.PI * s.frequencyHz;
                    float yNow = s.amplitudePx * Mathf.Sin(omega * t + s.phaseOffset);

                    Vector2 baseMove = new(vx * dt, 0f);
                    Vector2 newSine = new(0f, yNow);
                    Vector2 delta = baseMove + (newSine - sineOffset);
                    delta = ApplyWallStop(delta);    // ★ ここで壁停止
                    rect.anchoredPosition += delta;
                    sineOffset = newSine;
                    break;
                }
            case MovePattern.VerticalSine:
                {
                    float vy = (s.direction.y >= 0f ? 1f : -1f) * Mathf.Abs(s.speedPxPerSec);
                    float omega = 2f * Mathf.PI * s.frequencyHz;
                    float xNow = s.amplitudePx * Mathf.Sin(omega * t + s.phaseOffset);

                    Vector2 baseMove = new(0f, vy * dt);
                    Vector2 newSine = new(xNow, 0f);
                    Vector2 delta = baseMove + (newSine - sineOffset);
                    delta = ApplyWallStop(delta);    // ★ ここで壁停止
                    rect.anchoredPosition += delta;
                    sineOffset = newSine;
                    break;
                }
            case MovePattern.FigureEight:
                {
                    float w = 2f * Mathf.PI * s.eightSpeedHz;
                    float x = s.eightWidthPx * Mathf.Sin(w * t + s.eightPhase);
                    float y = s.eightHeightPx * Mathf.Sin(2f * w * t + s.eightPhase);
                    rect.anchoredPosition = originPos + new Vector2(x, y);
                    break;
                }
        }

        if (segTime >= Mathf.Max(0f, s.durationSec))
        {
            pauseRemain = Mathf.Max(0f, s.pauseAfterSec);
            if (pauseRemain <= 0f) Advance();
        }
    }

    private void BeginSegment(int newIndex)
    {
        idx = newIndex;
        segTime = 0f;
        t = 0f;
        sineOffset = Vector2.zero;
        originPos = rect.anchoredPosition;
        pauseRemain = 0f;
    }

    private void Advance()
    {
        if (segments.Count == 0) { idx = -1; return; }
        int next = idx + 1;
        if (next >= segments.Count)
        {
            if (!loopSequence) { idx = -1; return; }
            next = 0;
        }
        BeginSegment(next);
    }

    // === 壁当たり停止 ===
    private Vector2 ApplyWallStop(Vector2 localDelta)
    {
        if (!wallStop || container == null) return localDelta;

        // container & target の四隅（ワールド）
        container.GetWorldCorners(C);
        rect.GetWorldCorners(T);

        float cMinX = Mathf.Min(C[0].x, C[1].x, C[2].x, C[3].x) + wallInnerPadding.x;
        float cMaxX = Mathf.Max(C[0].x, C[1].x, C[2].x, C[3].x) - wallInnerPadding.x;
        float cMinY = Mathf.Min(C[0].y, C[1].y, C[2].y, C[3].y) + wallInnerPadding.y;
        float cMaxY = Mathf.Max(C[0].y, C[1].y, C[2].y, C[3].y) - wallInnerPadding.y;

        float tMinX = Mathf.Min(T[0].x, T[1].x, T[2].x, T[3].x);
        float tMaxX = Mathf.Max(T[0].x, T[1].x, T[2].x, T[3].x);
        float tMinY = Mathf.Min(T[0].y, T[1].y, T[2].y, T[3].y);
        float tMaxY = Mathf.Max(T[0].y, T[1].y, T[2].y, T[3].y);

        // localDelta → 親ローカル → ワールド の変換（UIなので TransformVector でOK）
        var parent = rect.parent as RectTransform;
        Vector3 worldDelta = localDelta;
        if (parent != null) worldDelta = parent.TransformVector(localDelta);

        // 端に接していて、さらに外向きへ進もうとしているなら、その成分を 0 に
        bool atLeft = Mathf.Abs(tMinX - cMinX) <= wallEpsPx;
        bool atRight = Mathf.Abs(tMaxX - cMaxX) <= wallEpsPx;
        bool atBottom = Mathf.Abs(tMinY - cMinY) <= wallEpsPx;
        bool atTop = Mathf.Abs(tMaxY - cMaxY) <= wallEpsPx;

        if ((atLeft && worldDelta.x < 0f) || (atRight && worldDelta.x > 0f))
            localDelta.x = 0f;
        if ((atBottom && worldDelta.y < 0f) || (atTop && worldDelta.y > 0f))
            localDelta.y = 0f;

        return localDelta;
    }
}
