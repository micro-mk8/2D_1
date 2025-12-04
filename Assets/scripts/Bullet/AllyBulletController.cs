using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// �����e�̔��ː���B�Đ��O��Inspector�̃`�F�b�N�ŗL��/������ؑ։\�B
/// </summary>
public class AllyBulletController : MonoBehaviour
{
    [Header("�Q��")]
    [SerializeField] private RectTransform bulletLayer;     // PlayAreaFrame �̎q�ɂ��� BulletLayer
    [SerializeField] private RectTransform playAreaFrame;   // ���E
    [SerializeField] private RectTransform enemyRoot;       // �z�[�~���O�p�^�[�Q�b�g
    [SerializeField] private AudioManager audioManager;     // �� �ǉ��F�T�E���h�Đ��p

    [Header("�v���n�u")]
    [SerializeField] private GameObject straightBulletPrefab;
    [SerializeField] private GameObject homingBulletPrefab;

    [Header("�L��/�����i�Đ��O�ɐؑցj")]
    public bool enableStraight = true;
    public bool enableHoming = false;
    public bool enableM5Fire = false; 

    [Header("�A�ːݒ�")]
    [SerializeField] private float straightFireRate = 6f; // 1�b������
    [SerializeField] private float homingFireRate = 2f;

    [Header("�e�p�����[�^�i����j")]
    [SerializeField] private float straightSpeed = 900f;
    [SerializeField] private bool straightUpwards = true;

    [SerializeField] private float homingSpeed = 700f;
    [SerializeField] private float homingTurnDegPerSec = 360f;

    private float straightTimer, homingTimer;
    private RectTransform rect; // �v���C���[�i�����j

    void Awake() => rect = GetComponent<RectTransform>();

    void Update()
    {
        float dt = Time.deltaTime;

        if (enableStraight && straightBulletPrefab)
        {
            straightTimer += dt;
            float interval = Mathf.Max(0.01f, 1f / straightFireRate);
            if (straightTimer >= interval)
            {
                straightTimer -= interval;
                SpawnStraight();
            }
        }

        if (enableHoming && homingBulletPrefab)
        {
            homingTimer += dt;
            float interval = Mathf.Max(0.01f, 1f / homingFireRate);
            if (homingTimer >= interval)
            {
                homingTimer -= interval;
                SpawnHoming();
            }
        }
    }

    //3�ځFM5�{�^���Ō��p�̌��J�t�b�N
    public void FireStraightOnce_FromM5()
    {
        if (!enableM5Fire || straightBulletPrefab == null) return;
        SpawnStraight();
    }

    // ---- �������� ----
    private void SpawnStraight()
    {
        var go = Instantiate(straightBulletPrefab, bulletLayer);
        var b = go.GetComponent<AllyBulletBaseUI>();
        var m = go.GetComponent<BulletStraightUI>();

        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = rect.anchoredPosition; // �v���C���[�ʒu����

        if (b != null) b.Init(playAreaFrame);
        if (m != null)
        {
            m.SetSpeed(straightSpeed);
            m.SetUpwards(straightUpwards);
        }

            // �� �����ŃV���b�g����炷
        if (audioManager != null)
        {
            audioManager.PlayShoot();
        }
    }

    private void SpawnHoming()
    {
        var go = Instantiate(homingBulletPrefab, bulletLayer);
        var b = go.GetComponent<AllyBulletBaseUI>();
        var m = go.GetComponent<BulletHomingUI>();

        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = rect.anchoredPosition;

        if (b != null) b.Init(playAreaFrame);
        if (m != null)
        {
            m.Setup(enemyRoot, bulletLayer);
            // ���x�E�񓪂̏㏑��
            var so = m.GetComponent<BulletHomingUI>();
            so.GetType().GetField("speedPxPerSec", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.SetValue(so, homingSpeed);
            so.GetType().GetField("turnDegPerSec", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.SetValue(so, homingTurnDegPerSec);
        }

            // �� �����ŃV���b�g����炷
        if (audioManager != null)
        {
            audioManager.PlayShoot();
        }
    }
}
