using UnityEngine;

/// <summary>
/// 対象(RectTransform)の「四隅(ワールド)」が container の内側に入るように
/// 毎フレーム位置を補正します。アンカー/ピボット設定に依存しません。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIRectClamp : MonoBehaviour
{
    [SerializeField] private RectTransform container;   // 例: PlayAreaFrame
    [SerializeField] private Vector2 padding = new Vector2(8f, 8f);

    private RectTransform rect;

    // 使い回しバッファ（GC削減）
    static readonly Vector3[] C = new Vector3[4];
    static readonly Vector3[] T = new Vector3[4];

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        if (!container) Debug.LogWarning("[UIRectClamp] Container未設定です。");
    }

    void LateUpdate()
    {
        if (!container) return;

        // 四隅(ワールド)を取得
        container.GetWorldCorners(C);
        rect.GetWorldCorners(T);

        // container の内側境界（パディング適用）
        float cMinX = Mathf.Min(C[0].x, C[1].x, C[2].x, C[3].x) + padding.x;
        float cMaxX = Mathf.Max(C[0].x, C[1].x, C[2].x, C[3].x) - padding.x;
        float cMinY = Mathf.Min(C[0].y, C[1].y, C[2].y, C[3].y) + padding.y;
        float cMaxY = Mathf.Max(C[0].y, C[1].y, C[2].y, C[3].y) - padding.y;

        // 対象の現在境界
        float tMinX = Mathf.Min(T[0].x, T[1].x, T[2].x, T[3].x);
        float tMaxX = Mathf.Max(T[0].x, T[1].x, T[2].x, T[3].x);
        float tMinY = Mathf.Min(T[0].y, T[1].y, T[2].y, T[3].y);
        float tMaxY = Mathf.Max(T[0].y, T[1].y, T[2].y, T[3].y);

        // 必要な補正量（ワールド）を計算
        float dx = 0f, dy = 0f;

        // 横
        float contW = cMaxX - cMinX;
        float targW = tMaxX - tMinX;
        if (targW <= contW)
        {
            if (tMinX < cMinX) dx = cMinX - tMinX;
            else if (tMaxX > cMaxX) dx = cMaxX - tMaxX;
        }
        else
        {
            // 対象が container より幅広い場合は中心を合わせる
            float cMidX = 0.5f * (cMinX + cMaxX);
            float tMidX = 0.5f * (tMinX + tMaxX);
            dx = cMidX - tMidX;
        }

        // 縦
        float contH = cMaxY - cMinY;
        float targH = tMaxY - tMinY;
        if (targH <= contH)
        {
            if (tMinY < cMinY) dy = cMinY - tMinY;         // 下にはみ出しを押し上げ
            else if (tMaxY > cMaxY) dy = cMaxY - tMaxY;    // 上にはみ出しを押し下げ
        }
        else
        {
            // 対象が container より背が高い場合は中心を合わせる
            float cMidY = 0.5f * (cMinY + cMaxY);
            float tMidY = 0.5f * (tMinY + tMaxY);
            dy = cMidY - tMidY;
        }

        if (dx != 0f || dy != 0f)
        {
            // ワールド空間で微調整（アンカー/ピボットに依存しない）
            rect.position += new Vector3(dx, dy, 0f);
        }
    }
}
