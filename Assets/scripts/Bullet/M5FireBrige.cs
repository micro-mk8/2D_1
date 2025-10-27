using UnityEngine;




public class M5FireBridge : MonoBehaviour
{
    [SerializeField] private AllyBulletController controller;
    private string lastRaw;

    [SerializeField] private GameFlowController gameFlowController; // ← 追加：インスペクタで割り当て
    [SerializeField] private bool triggerStartRetryOnFire = true;   // ← 追加：FIREでStart/Retryを呼ぶか


    void Reset() { controller = GetComponent<AllyBulletController>(); }

    void Update()
    {
        var r = UdpReceiver.Instance;
        if (r == null || controller == null) return;

        string raw = r.latestRaw;        // UdpReceiver が最後に受けた生文字列
        if (string.IsNullOrEmpty(raw) || raw == lastRaw) return; // 同一パケットの多重発火防止
        lastRaw = raw;

        if (raw.StartsWith("FIRE"))
        {
            controller.enableM5Fire = true;           // 事前トグルをON
            controller.FireStraightOnce_FromM5();     // 1発だけ発射
        }

        // M5FireBridge 側：発射ボタンを検知した箇所で
        if (triggerStartRetryOnFire && gameFlowController != null)
            gameFlowController.StartOrRetry();

    }
}
