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
        // TimeScaleが0でもM5入力は通す
        if (udp == null) return;

        string raw = udp.latestRaw;
        if (string.IsNullOrEmpty(raw)) return;

        if (raw == lastRaw) return;
        lastRaw = raw;

        if (raw.StartsWith("FIRE"))
        {
            if (controller != null)
            {
                controller.enableM5Fire = true;
                controller.FireStraightOnce_FromM5();
            }

            // --- 追加: Pause中でもイベントを送る ---
            onFirePressedForStartRetry?.Invoke();
        }
    }


    // GameFlowController から AddListener しやすいように公開
    public UnityEvent GetStartRetryEvent()
    {
        return onFirePressedForStartRetry;
    }
}
