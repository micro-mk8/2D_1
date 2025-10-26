using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class PlayerMoverFromM5 : MonoBehaviour
{
    [Header("���x�ipx/�b per g�j")]
    [SerializeField] private float pixelsPerG = 600f;

    [Header("�f�b�h�]�[���ig�j��臒l�ȉ��͖���")]
    [SerializeField, Range(0f, 0.5f)] private float deadZoneG = 0.10f;

    [Header("���͂̒P�ʒ����i��F5��0.05g �ɂ���Ȃ� 0.01�j")]
    [SerializeField] private float inputScale = 1f;

    [Header("���̒���")]
    [SerializeField] private bool invertX = false;
    [SerializeField] private bool invertY = false;
    [SerializeField] private bool swapXY = false;

    [Header("�[���_�␳�i���ɒu�����p�����[���Ɂj")]
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

        // ��M�l�iM5��xy�j�擾
        Vector2 a = new Vector2(r.latestAccel.x, r.latestAccel.y);

        // �P�ʃX�P�[���i5��0.05g �Ȃǁj
        a *= inputScale;

        // ������
        if (swapXY) a = new Vector2(a.y, a.x);
        if (invertX) a.x = -a.x;
        if (invertY) a.y = -a.y;

        // �[���_�␳�i���ɒu�������̃I�t�Z�b�g�������j
        a -= bias;

        // �f�b�h�]�[���i臒l�ȉ���0�A���ߕ��͍ă}�b�v�j
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

        bias = a; // ���̎p�����u�[���v�Ƃ���
    }

    private static float ApplySoftDeadzone(float v, float dz)
    {
        float av = Mathf.Abs(v);
        if (av <= dz) return 0f;
        float remapped = (av - dz) / (1f - dz);
        return Mathf.Sign(v) * remapped;
    }
}
