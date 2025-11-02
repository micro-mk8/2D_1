using UnityEngine;
using UnityEngine.Events;

public class M5FireBridge : MonoBehaviour
{
    [Header("プレイヤー弾を撃つコントローラ")]
    [SerializeField] private AllyBulletController controller;

    [Header("UDP受信 (latestRaw を読む)")]
    [SerializeField] private UdpReceiver udp;

    [Header("Start/Retry用イベント (GameFlowControllerが購読する)")]
    [SerializeField] private UnityEvent onFirePressedForStartRetry;

    // 直前に読んだraw。連打で同じ文字列を何度も処理しないためのフィルタ
    private string lastRaw = null;

    void Update()
    {
        // UDPがなければ何もできない
        if (udp == null) return;

        // M5から来た生の文字列（例: "FIRE", "ACCEL..." みたいな想定）
        string raw = udp.latestRaw;
        if (string.IsNullOrEmpty(raw)) return;

        // 同じrawを連続で処理しない
        if (raw == lastRaw) return;
        lastRaw = raw;

        // ボタン押し合図かどうかを判定
        if (raw.StartsWith("FIRE"))
        {
            // --- 1. プレイ中想定の弾発射 ----------------
            if (controller != null)
            {
                // フラグや1発撃ちメソッドはあなたのAllyBulletControllerに合わせてください
                controller.enableM5Fire = true;
                controller.FireStraightOnce_FromM5();
            }

            // --- 2. Start/Retry シグナル ----------------
            // GameFlowController側が購読していれば、タイトル・リザルト中はここからスタート/リスタートできる
            if (onFirePressedForStartRetry != null)
            {
                onFirePressedForStartRetry.Invoke();
            }
        }
    }

    // GameFlowController から AddListener しやすいように公開
    public UnityEvent GetStartRetryEvent()
    {
        return onFirePressedForStartRetry;
    }
}
