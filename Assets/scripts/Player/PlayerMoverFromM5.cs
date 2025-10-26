using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class PlayerMoverFromM5 : MonoBehaviour
{
    [Header("感度（px/秒 per g）")]
    [SerializeField] private float pixelsPerG = 600f;

    [Header("デッドゾーン（g）※閾値以下は無視")]
    [SerializeField, Range(0f, 0.5f)] private float deadZoneG = 0.10f;

    [Header("入力の単位調整（例：5→0.05g にするなら 0.01）")]
    [SerializeField] private float inputScale = 1f;

    [Header("軸の調整")]
    [SerializeField] private bool invertX = false;
    [SerializeField] private bool invertY = false;
    [SerializeField] private bool swapXY = false;

    [Header("ゼロ点補正（机に置いた姿勢をゼロに）")]
    [SerializeField] private Vector2 bias = Vector2.zero;
    [SerializeField] private bool autoCalibrateOnStart = false;
    [SerializeField] private KeyCode calibrateKey = KeyCode.C;

    private RectTransform rect;

    void Awake() => rect = GetComponent<RectTransform>();

    void Start()
    {
        if (autoCalibrateOnStart) CaptureBias();
    }

    void Update()
    {
        if (Input.GetKeyDown(calibrateKey)) CaptureBias();

        var r = UdpReceiver.Instance;
        if (r == null) return;

        // 受信値（M5のxy）取得
        Vector2 a = new Vector2(r.latestAccel.x, r.latestAccel.y);

        // 単位スケール（5→0.05g など）
        a *= inputScale;

        // 軸調整
        if (swapXY) a = new Vector2(a.y, a.x);
        if (invertX) a.x = -a.x;
        if (invertY) a.y = -a.y;

        // ゼロ点補正（机に置いた時のオフセットを引く）
        a -= bias;

        // デッドゾーン（閾値以下は0、超過分は再マップ）
        a.x = ApplySoftDeadzone(a.x, deadZoneG);
        a.y = ApplySoftDeadzone(a.y, deadZoneG);

        rect.anchoredPosition += a * pixelsPerG * Time.deltaTime;
    }

    [ContextMenu("Calibrate Now (use current M5 values as zero)")]
    public void CaptureBias()
    {
        var r = UdpReceiver.Instance;
        if (r == null) return;

        Vector2 a = new Vector2(r.latestAccel.x, r.latestAccel.y) * inputScale;
        if (swapXY) a = new Vector2(a.y, a.x);
        if (invertX) a.x = -a.x;
        if (invertY) a.y = -a.y;

        bias = a; // この姿勢を「ゼロ」とする
    }

    private static float ApplySoftDeadzone(float v, float dz)
    {
        float av = Mathf.Abs(v);
        if (av <= dz) return 0f;
        float remapped = (av - dz) / (1f - dz);
        return Mathf.Sign(v) * remapped;
    }
}
