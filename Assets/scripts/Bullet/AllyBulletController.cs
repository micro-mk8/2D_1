using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 味方弾の発射制御。再生前にInspectorのチェックで有効/無効を切替可能。
/// </summary>
public class AllyBulletController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private RectTransform bulletLayer;     // PlayAreaFrame の子にある BulletLayer
    [SerializeField] private RectTransform playAreaFrame;   // 境界
    [SerializeField] private RectTransform enemyRoot;       // ホーミング用ターゲット

    [Header("プレハブ")]
    [SerializeField] private GameObject straightBulletPrefab;
    [SerializeField] private GameObject homingBulletPrefab;

    [Header("有効/無効（再生前に切替）")]
    public bool enableStraight = true;
    public bool enableHoming = false;
    public bool enableM5Fire = false; 

    [Header("連射設定")]
    [SerializeField] private float straightFireRate = 6f; // 1秒あたり
    [SerializeField] private float homingFireRate = 2f;

    [Header("弾パラメータ（既定）")]
    [SerializeField] private float straightSpeed = 900f;
    [SerializeField] private bool straightUpwards = true;

    [SerializeField] private float homingSpeed = 700f;
    [SerializeField] private float homingTurnDegPerSec = 360f;

    private float straightTimer, homingTimer;
    private RectTransform rect; // プレイヤー（自分）

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

    //3つ目：M5ボタンで撃つ用の公開フック
    public void FireStraightOnce_FromM5()
    {
        if (!enableM5Fire || straightBulletPrefab == null) return;
        SpawnStraight();
    }

    // ---- 生成処理 ----
    private void SpawnStraight()
    {
        var go = Instantiate(straightBulletPrefab, bulletLayer);
        var b = go.GetComponent<AllyBulletBaseUI>();
        var m = go.GetComponent<BulletStraightUI>();

        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = rect.anchoredPosition; // プレイヤー位置から

        if (b != null) b.Init(playAreaFrame);
        if (m != null)
        {
            m.SetSpeed(straightSpeed);
            m.SetUpwards(straightUpwards);
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
            // 速度・回頭の上書き
            var so = m.GetComponent<BulletHomingUI>();
            so.GetType().GetField("speedPxPerSec", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.SetValue(so, homingSpeed);
            so.GetType().GetField("turnDegPerSec", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.SetValue(so, homingTurnDegPerSec);
        }
    }
}
