using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class EnemySplitBulletUI : MonoBehaviour
{
    private RectTransform rect;
    private RectTransform space;
    private RectTransform container;

    private Vector2 velocity;
    private float splitAfterSec;
    private float timer;

    private GameObject shardPrefab;
    private int shardCount;
    private float shardSpeed;

    //破片を借りるためのプール参照
    private GameObjectPool shardPool;

    void Awake() => rect = GetComponent<RectTransform>();

    //pool を受け取れるように引数を追加（既定は null）
    public void Setup(
        RectTransform spaceRT,
        RectTransform containerRT,
        Vector2 initialVelocity,
        float splitAfterSeconds,
        GameObject shardPrefab,
        int shardCount,
        float shardSpeed,
        GameObjectPool shardPool = null
    )
    {
        this.space = spaceRT;
        this.container = containerRT;
        this.velocity = initialVelocity;
        this.splitAfterSec = Mathf.Max(0.05f, splitAfterSeconds);
        this.shardPrefab = shardPrefab;
        this.shardCount = Mathf.Max(1, shardCount);
        this.shardSpeed = shardSpeed;
        this.shardPool = shardPool;   // ★保持
    }

    void Update()
    {
        rect.anchoredPosition += velocity * Time.deltaTime;
        timer += Time.deltaTime;

        if (timer >= splitAfterSec)
            Explode();
    }

    private void Explode()
    {
        if (!space || !container || !shardPrefab) { Destroy(gameObject); return; }

        Vector2 pos = rect.anchoredPosition;
        float step = 360f / shardCount;

        for (int i = 0; i < shardCount; i++)
        {
            float deg = i * step;
            float rad = deg * Mathf.Deg2Rad;
            Vector2 v = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * shardSpeed;

            //プールがあれば Get、無ければ Instantiate にフォールバック
            GameObject go = shardPool ? shardPool.Get()
                                      : Object.Instantiate(shardPrefab, space);

            // フォールバック時のみ親を合わせる
            if (!shardPool && go.transform.parent != space)
                go.transform.SetParent(space, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;

            var baseLife = go.GetComponent<AllyBulletBaseUI>();
            if (baseLife) baseLife.Init(container);

            var mover = go.GetComponent<EnemyBulletMoverUI>();
            if (mover) mover.SetVelocity(v);

            var hb = go.GetComponent<UIHitbox2D>();
            if (hb) { hb.role = UIRole.Bullet; hb.faction = UIFaction.Enemy; }
        }

        Destroy(gameObject); // 本体は消える
    }
}
