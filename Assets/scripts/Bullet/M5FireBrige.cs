using UnityEngine;




public class M5FireBridge : MonoBehaviour
{
    [SerializeField] private AllyBulletController controller;
    private string lastRaw;

    [SerializeField] private GameFlowController gameFlowController; // �� �ǉ��F�C���X�y�N�^�Ŋ��蓖��
    [SerializeField] private bool triggerStartRetryOnFire = true;   // �� �ǉ��FFIRE��Start/Retry���ĂԂ�


    void Reset() { controller = GetComponent<AllyBulletController>(); }

    void Update()
    {
        var r = UdpReceiver.Instance;
        if (r == null || controller == null) return;

        string raw = r.latestRaw;        // UdpReceiver ���Ō�Ɏ󂯂���������
        if (string.IsNullOrEmpty(raw) || raw == lastRaw) return; // ����p�P�b�g�̑��d���Ζh�~
        lastRaw = raw;

        if (raw.StartsWith("FIRE"))
        {
            controller.enableM5Fire = true;           // ���O�g�O����ON
            controller.FireStraightOnce_FromM5();     // 1����������
        }

        // M5FireBridge ���F���˃{�^�������m�����ӏ���
        if (triggerStartRetryOnFire && gameFlowController != null)
            gameFlowController.StartOrRetry();

    }
}
