using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵の弾幕コントローラ。原案の3パターンを再現：
/// 0: プレイヤー狙いツイン / 1: 半円ばら撒き / 2: 進行→分裂
/// ・BulletLayer/PlayAreaFrame/EnemyRoot/PlayerRoot を同一UI空間に置くこと
/// ・弾Prefabには UIHitbox2D(Role=Bullet,Faction=Enemy) と UIBulletOnHitDestroy を付ける
/// ・寿命/画面外消滅は AllyBulletBaseUI をそのまま流用OK
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class EnemyDanmakuController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private RectTransform bulletLayer;    // 弾の親（PlayAreaFrameの子）
    [SerializeField] private RectTransform playAreaFrame;  // 画面境界
    [SerializeField] private RectTransform enemyRoot;      // 自分自身（省略時はこのObj）
    [SerializeField] private RectTransform playerRoot;     // プレイヤー

    [Header("弾プレハブ")]
    [SerializeField] private GameObject enemyBulletPrefab;     // 小弾（直進用）
    [SerializeField] private GameObject enemySplitBulletPrefab;// 分裂弾（分裂本体）

    [Header("パターン有効化")]
    public bool enablePattern0_AimedTwin = true;
    public bool enablePattern1_Semicircle = true;
    public bool enablePattern2_Split = true;

    [Header("発射タイミング")]
    [Tooltip("弾幕の発射間隔(秒)")]
    [SerializeField] private float intervalSec = 0.50f;
    [Tooltip("1回の発射で何回パターンを実行するか（原案の for(i<5) 相当）")]
    [SerializeField] private int burstCount = 5;

    [Header("Pattern 0: プレイヤー狙いツイン")]
    [SerializeField] private float aimedSpeed = 420f;     // px/s
    [SerializeField] private float twinOffsetX = 12f;     // 左右オフセット(px)

    [Header("Pattern 1: 半円ばら撒き")]
    [SerializeField] private float radialSpeed = 350f;    // px/s
    [SerializeField] private float radialStartDeg = 0f;   // 0=右, 90=上
    [SerializeField] private float radialEndDeg = 180f;   // 半円
    [SerializeField] private float radialStepDeg = 15f;

    [Header("Pattern 2: 分裂弾（本体 → ばら撒き）")]
    [SerializeField] private float splitMainSpeed = 260f; // 本体の移動速度
    [SerializeField] private float splitAfterSec = 0.8f;  // 何秒後に分裂するか
    [SerializeField] private int splitShards = 12;        // 破片の個数
    [SerializeField] private float shardSpeed = 320f;     // 破片速度

    [SerializeField] private GameObjectPool enemyBulletPool;     // 小弾用
    [SerializeField] private GameObjectPool enemySplitMainPool;  // 分裂本体（任意：大量でなければ未指定でもOK）


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

        // 1回のトリガで複数回パターン実行（原案の burst）
        for (int i = 0; i < Mathf.Max(1, burstCount); i++)
            FireRandomEnabledPattern();
    }

    // ---- パターン選択 ----
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

    // ---- Pattern 0: プレイヤー狙いツイン ----
    private void Pattern0_AimedTwin()
    {
        if (!enemyBulletPrefab || !bulletLayer || !playAreaFrame) return;

        Vector2 ec = GetCenterLocal(enemyRoot, bulletLayer);
        Vector2 pc = playerRoot ? GetCenterLocal(playerRoot, bulletLayer) : ec + Vector2.down * 100f;

        Vector2 dir = (pc - ec).normalized;
        Vector2 v = dir * aimedSpeed;

        // 左右にオフセットして2発
        SpawnBullet(enemyBulletPrefab, ec + new Vector2(-twinOffsetX, 0f), v);
        SpawnBullet(enemyBulletPrefab, ec + new Vector2(+twinOffsetX, 0f), v);
    }

    // ---- Pattern 1: 半円ばら撒き ----
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
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)); // 0°=右, 90°=上
            Vector2 v = dir.normalized * radialSpeed;
            SpawnBullet(enemyBulletPrefab, ec, v);
        }
    }

    // ---- Pattern 2: 分裂弾 ----
    private void Pattern2_Split()
    {
        if (!enemySplitBulletPrefab || !bulletLayer || !playAreaFrame) return;

        Vector2 ec = GetCenterLocal(enemyRoot, bulletLayer);
        Vector2 pc = playerRoot ? GetCenterLocal(playerRoot, bulletLayer) : ec + Vector2.down * 120f;
        Vector2 dir = (pc - ec).sqrMagnitude > 0.001f ? (pc - ec).normalized : Vector2.down;

        var go = Instantiate(enemySplitBulletPrefab, bulletLayer);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = ec;

        var baseLife = go.GetComponent<AllyBulletBaseUI>(); // 寿命/境界消滅の流用
        if (baseLife) baseLife.Init(playAreaFrame);

        var split = go.GetComponent<EnemySplitBulletUI>();
        if (split)
        {
            // 変更前：pool 引数なし
            split.Setup(
                spaceRT: bulletLayer,
                containerRT: playAreaFrame,
                initialVelocity: dir * splitMainSpeed,
                splitAfterSeconds: splitAfterSec,
                shardPrefab: enemyBulletPrefab,
                shardCount: Mathf.Max(1, splitShards),
                shardSpeed: shardSpeed
            );

            // 変更後：最後に pool を渡す
            split.Setup(
                spaceRT: bulletLayer,
                containerRT: playAreaFrame,
                initialVelocity: dir * splitMainSpeed,
                splitAfterSeconds: splitAfterSec,
                shardPrefab: enemyBulletPrefab,
                shardCount: Mathf.Max(1, splitShards),
                shardSpeed: shardSpeed,
                shardPool: enemyBulletPool        // ★ここを追加
            );
        }

        // 当たり判定（分裂本体も当たるなら Hitbox を付けたプレハブを使う）
        var hb = go.GetComponent<UIHitbox2D>();
        if (hb) { hb.role = UIRole.Bullet; hb.faction = UIFaction.Enemy; }
    }

    // ---- 共通：弾生成 ----
    private void SpawnBullet(GameObject prefab, Vector2 localPos, Vector2 velocity)
    {
        GameObject go = null;
        if (enemyBulletPool != null) go = enemyBulletPool.Get();
        else go = Instantiate(prefab, bulletLayer);

        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = localPos;

        // 初期化（従来通り）
        var baseLife = go.GetComponent<AllyBulletBaseUI>();
        if (baseLife) baseLife.Init(playAreaFrame);

        var mover = go.GetComponent<EnemyBulletMoverUI>();
        if (mover) mover.SetVelocity(velocity);

        var hb = go.GetComponent<UIHitbox2D>();
        if (hb) { hb.role = UIRole.Bullet; hb.faction = UIFaction.Enemy; }
    }

    // ---- 座標ユーティリティ ----
    private static Vector2 GetCenterLocal(RectTransform rt, RectTransform space)
    {
        if (!rt || !space) return Vector2.zero;
        Vector3 world = rt.TransformPoint(rt.rect.center);
        Vector3 local = space.InverseTransformPoint(world);
        return new Vector2(local.x, local.y);
    }
}
