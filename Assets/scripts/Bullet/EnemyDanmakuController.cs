using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �G�̒e���R���g���[���B���Ă�3�p�^�[�����Č��F
/// 0: �v���C���[�_���c�C�� / 1: ���~�΂�T�� / 2: �i�s������
/// �EBulletLayer/PlayAreaFrame/EnemyRoot/PlayerRoot �𓯈�UI��Ԃɒu������
/// �E�ePrefab�ɂ� UIHitbox2D(Role=Bullet,Faction=Enemy) �� UIBulletOnHitDestroy ��t����
/// �E����/��ʊO���ł� AllyBulletBaseUI �����̂܂ܗ��pOK
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class EnemyDanmakuController : MonoBehaviour
{
    [Header("�Q��")]
    [SerializeField] private RectTransform bulletLayer;    // �e�̐e�iPlayAreaFrame�̎q�j
    [SerializeField] private RectTransform playAreaFrame;  // ��ʋ��E
    [SerializeField] private RectTransform enemyRoot;      // �������g�i�ȗ����͂���Obj�j
    [SerializeField] private RectTransform playerRoot;     // �v���C���[

    [Header("�e�v���n�u")]
    [SerializeField] private GameObject enemyBulletPrefab;     // ���e�i���i�p�j
    [SerializeField] private GameObject enemySplitBulletPrefab;// �����e�i�����{�́j

    [Header("�p�^�[���L����")]
    public bool enablePattern0_AimedTwin = true;
    public bool enablePattern1_Semicircle = true;
    public bool enablePattern2_Split = true;

    [Header("���˃^�C�~���O")]
    [Tooltip("�e���̔��ˊԊu(�b)")]
    [SerializeField] private float intervalSec = 0.50f;
    [Tooltip("1��̔��˂ŉ���p�^�[�������s���邩�i���Ă� for(i<5) �����j")]
    [SerializeField] private int burstCount = 5;

    [Header("Pattern 0: �v���C���[�_���c�C��")]
    [SerializeField] private float aimedSpeed = 420f;     // px/s
    [SerializeField] private float twinOffsetX = 12f;     // ���E�I�t�Z�b�g(px)

    [Header("Pattern 1: ���~�΂�T��")]
    [SerializeField] private float radialSpeed = 350f;    // px/s
    [SerializeField] private float radialStartDeg = 0f;   // 0=�E, 90=��
    [SerializeField] private float radialEndDeg = 180f;   // ���~
    [SerializeField] private float radialStepDeg = 15f;

    [Header("Pattern 2: �����e�i�{�� �� �΂�T���j")]
    [SerializeField] private float splitMainSpeed = 260f; // �{�̂̈ړ����x
    [SerializeField] private float splitAfterSec = 0.8f;  // ���b��ɕ��􂷂邩
    [SerializeField] private int splitShards = 12;        // �j�Ђ̌�
    [SerializeField] private float shardSpeed = 320f;     // �j�Б��x

    [SerializeField] private GameObjectPool enemyBulletPool;     // ���e�p
    [SerializeField] private GameObjectPool enemySplitMainPool;  // �����{�́i�C�ӁF��ʂłȂ���Ζ��w��ł�OK�j


    private float timer;

    void Reset()
    {
        enemyRoot = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (!enemyRoot) enemyRoot = GetComponent<RectTransform>();
        timer += Time.deltaTime;
        if (timer < intervalSec) return;
        timer -= intervalSec;

        // 1��̃g���K�ŕ�����p�^�[�����s�i���Ă� burst�j
        for (int i = 0; i < Mathf.Max(1, burstCount); i++)
            FireRandomEnabledPattern();
    }

    // ---- �p�^�[���I�� ----
    private void FireRandomEnabledPattern()
    {
        var enabled = new List<System.Action>();
        if (enablePattern0_AimedTwin) enabled.Add(Pattern0_AimedTwin);
        if (enablePattern1_Semicircle) enabled.Add(Pattern1_Semicircle);
        if (enablePattern2_Split) enabled.Add(Pattern2_Split);

        if (enabled.Count == 0) return;
        int idx = Random.Range(0, enabled.Count);
        enabled[idx].Invoke();
    }

    // ---- Pattern 0: �v���C���[�_���c�C�� ----
    private void Pattern0_AimedTwin()
    {
        if (!enemyBulletPrefab || !bulletLayer || !playAreaFrame) return;

        Vector2 ec = GetCenterLocal(enemyRoot, bulletLayer);
        Vector2 pc = playerRoot ? GetCenterLocal(playerRoot, bulletLayer) : ec + Vector2.down * 100f;

        Vector2 dir = (pc - ec).normalized;
        Vector2 v = dir * aimedSpeed;

        // ���E�ɃI�t�Z�b�g����2��
        SpawnBullet(enemyBulletPrefab, ec + new Vector2(-twinOffsetX, 0f), v);
        SpawnBullet(enemyBulletPrefab, ec + new Vector2(+twinOffsetX, 0f), v);
    }

    // ---- Pattern 1: ���~�΂�T�� ----
    private void Pattern1_Semicircle()
    {
        if (!enemyBulletPrefab || !bulletLayer || !playAreaFrame) return;

        Vector2 ec = GetCenterLocal(enemyRoot, bulletLayer);

        float start = radialStartDeg;
        float end = radialEndDeg;
        float step = Mathf.Max(1f, radialStepDeg);

        if (end < start) (start, end) = (end, start);

        for (float a = start; a <= end + 0.01f; a += step)
        {
            float rad = a * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)); // 0��=�E, 90��=��
            Vector2 v = dir.normalized * radialSpeed;
            SpawnBullet(enemyBulletPrefab, ec, v);
        }
    }

    // ---- Pattern 2: �����e ----
    private void Pattern2_Split()
    {
        if (!enemySplitBulletPrefab || !bulletLayer || !playAreaFrame) return;

        Vector2 ec = GetCenterLocal(enemyRoot, bulletLayer);
        Vector2 pc = playerRoot ? GetCenterLocal(playerRoot, bulletLayer) : ec + Vector2.down * 120f;
        Vector2 dir = (pc - ec).sqrMagnitude > 0.001f ? (pc - ec).normalized : Vector2.down;

        var go = Instantiate(enemySplitBulletPrefab, bulletLayer);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = ec;

        var baseLife = go.GetComponent<AllyBulletBaseUI>(); // ����/���E���ł̗��p
        if (baseLife) baseLife.Init(playAreaFrame);

        var split = go.GetComponent<EnemySplitBulletUI>();
        if (split)
        {
            // �ύX�O�Fpool �����Ȃ�
            split.Setup(
                spaceRT: bulletLayer,
                containerRT: playAreaFrame,
                initialVelocity: dir * splitMainSpeed,
                splitAfterSeconds: splitAfterSec,
                shardPrefab: enemyBulletPrefab,
                shardCount: Mathf.Max(1, splitShards),
                shardSpeed: shardSpeed
            );

            // �ύX��F�Ō�� pool ��n��
            split.Setup(
                spaceRT: bulletLayer,
                containerRT: playAreaFrame,
                initialVelocity: dir * splitMainSpeed,
                splitAfterSeconds: splitAfterSec,
                shardPrefab: enemyBulletPrefab,
                shardCount: Mathf.Max(1, splitShards),
                shardSpeed: shardSpeed,
                shardPool: enemyBulletPool        // ��������ǉ�
            );
        }

        // �����蔻��i�����{�̂�������Ȃ� Hitbox ��t�����v���n�u���g���j
        var hb = go.GetComponent<UIHitbox2D>();
        if (hb) { hb.role = UIRole.Bullet; hb.faction = UIFaction.Enemy; }
    }

    // ---- ���ʁF�e���� ----
    private void SpawnBullet(GameObject prefab, Vector2 localPos, Vector2 velocity)
    {
        GameObject go = null;
        if (enemyBulletPool != null) go = enemyBulletPool.Get();
        else go = Instantiate(prefab, bulletLayer);

        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = localPos;

        // �������i�]���ʂ�j
        var baseLife = go.GetComponent<AllyBulletBaseUI>();
        if (baseLife) baseLife.Init(playAreaFrame);

        var mover = go.GetComponent<EnemyBulletMoverUI>();
        if (mover) mover.SetVelocity(velocity);

        var hb = go.GetComponent<UIHitbox2D>();
        if (hb) { hb.role = UIRole.Bullet; hb.faction = UIFaction.Enemy; }
    }

    // ---- ���W���[�e�B���e�B ----
    private static Vector2 GetCenterLocal(RectTransform rt, RectTransform space)
    {
        if (!rt || !space) return Vector2.zero;
        Vector3 world = rt.TransformPoint(rt.rect.center);
        Vector3 local = space.InverseTransformPoint(world);
        return new Vector2(local.x, local.y);
    }
}
