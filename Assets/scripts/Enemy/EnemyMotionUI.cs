using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Canvas/UI( RectTransform ) 用のエネミー移動コンポーネント（弾や当たり判定は無し）。
/// 原案の「動き」部分のみを再現しやすい代表パターンを Inspector から選べます。
/// プレイエリアは UI 上の anchoredPosition を用いて移動します。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class EnemyMotionUI : MonoBehaviour
{
    public enum MovePattern
    {
        Straight,       // 一定方向へ直進
        HorizontalSine, // Xに進みつつYをサイン波で揺らす（横移動＋縦揺れ）
        VerticalSine,   // Yに進みつつXをサイン波で揺らす（縦移動＋横揺れ）
        FigureEight,    // その場で8の字（リサージュ）
        Waypoints       // 複数点を順に移動（ループ可）
    }

    [Header("基本")]
    [SerializeField] private MovePattern pattern = MovePattern.HorizontalSine;
    [SerializeField] private float speedPxPerSec = 260f; // 基本移動速度（px/s）
    [Tooltip("Straight の進行方向。HorizontalSine/VerticalSineでは主軸の進行向きに使用。")]
    [SerializeField] private Vector2 direction = new Vector2(-1f, 0f); // 既定は左へ

    [Header("サイン波（Horizontal/Vertical 用）")]
    [SerializeField] private float amplitudePx = 120f;   // 揺れ幅（ピーク）
    [SerializeField] private float frequencyHz = 0.9f;   // 揺れ周波数（1秒あたり）
    [SerializeField] private float phaseOffset = 0f;     // 初期位相（ラジアン）

    [Header("FigureEight（8の字）")]
    [SerializeField] private float eightWidthPx = 140f;   // 横幅
    [SerializeField] private float eightHeightPx = 90f;   // 縦幅
    [SerializeField] private float eightSpeedHz = 0.6f;   // 回る速さ（周回/秒）
    [SerializeField] private float eightPhase = 0f;       // 初期位相

    [Header("Waypoints（ローカル座標。Relative の場合は初期位置からの相対）")]
    [SerializeField] private bool relativeWaypoints = true;
    [SerializeField] private List<Vector2> waypoints = new List<Vector2>(); // PlayAreaFrame 基準のローカル点
    [SerializeField] private bool loopWaypoints = true;
    [SerializeField] private float arriveEps = 2f; // 到達判定しきい値(px)
    [SerializeField] private float waitAtPointSec = 0f;

    private RectTransform rect;
    private Vector2 startPos;     // 開始時の anchoredPosition
    private Vector2 sineOffset;   // サイン分のオフセットを差分適用するため保持
    private float t;              // 経過時間
    private int wpIndex;          // 現在のウェイポイント
    private float waitRemain;     // ウェイポイントでの待機残り

    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        startPos = rect.anchoredPosition;
        sineOffset = Vector2.zero;
        t = 0f;
        wpIndex = 0;
        waitRemain = 0f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;

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

    // ===== パターン実装 =====

    private void MoveStraight(float dt)
    {
        Vector2 dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.left;
        rect.anchoredPosition += dir * speedPxPerSec * dt;
    }

    private void MoveHorizontalSine(float dt)
    {
        // 主軸はX。dirの符号で左右どちらへ進むかを決める
        float vx = (direction.x >= 0f ? 1f : -1f) * speedPxPerSec;
        // サインはYにかける（差分適用でドリフトを防ぐ）
        float omega = 2f * Mathf.PI * frequencyHz;
        float yNow = amplitudePx * Mathf.Sin(omega * t + phaseOffset);

        // 直前のオフセットとの差分だけ足す
        Vector2 baseMove = new Vector2(vx * dt, 0f);
        Vector2 newSine = new Vector2(0f, yNow);
        Vector2 delta = baseMove + (newSine - sineOffset);

        rect.anchoredPosition += delta;
        sineOffset = newSine;
    }

    private void MoveVerticalSine(float dt)
    {
        // 主軸はY。dirの符号で上下どちらへ進むかを決める
        float vy = (direction.y >= 0f ? 1f : -1f) * speedPxPerSec;
        float omega = 2f * Mathf.PI * frequencyHz;
        float xNow = amplitudePx * Mathf.Sin(omega * t + phaseOffset);

        Vector2 baseMove = new Vector2(0f, vy * dt);
        Vector2 newSine = new Vector2(xNow, 0f);
        Vector2 delta = baseMove + (newSine - sineOffset);

        rect.anchoredPosition += delta;
        sineOffset = newSine;
    }

    private void MoveFigureEight()
    {
        // 中心は開始位置。リサージュで8の字: x=A sin(w t + φ), y=B sin(2 w t + φ)
        float w = 2f * Mathf.PI * eightSpeedHz;
        float x = eightWidthPx * Mathf.Sin(w * t + eightPhase);
        float y = eightHeightPx * Mathf.Sin(2f * w * t + eightPhase);
        rect.anchoredPosition = startPos + new Vector2(x, y);
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
        if (relativeWaypoints) target = startPos + target; // 開始位置を基準に相対指定

        Vector2 pos = rect.anchoredPosition;
        Vector2 to = target - pos;
        float dist = to.magnitude;

        if (dist <= arriveEps)
        {
            // 次の点へ
            wpIndex++;
            if (wpIndex >= waypoints.Count)
            {
                if (loopWaypoints) wpIndex = 0;
                else { wpIndex = waypoints.Count - 1; return; }
            }
            waitRemain = waitAtPointSec;
            return;
        }

        Vector2 step = to.normalized * speedPxPerSec * dt;
        if (step.magnitude > dist) step = to; // オーバーシュート防止
        rect.anchoredPosition = pos + step;
    }
}
