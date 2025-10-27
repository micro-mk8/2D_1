using UnityEngine;
using UnityEngine.Events;

public class GameFlowController : MonoBehaviour
{
    public enum GameState { Title, Playing, GameOver }

    [Header("参照")]
    [SerializeField] private PlayerRespawn playerRespawn;
    [SerializeField] private RectTransform bulletLayer;    // 弾の親（任意：開始時に掃除）
    [SerializeField] private MonoBehaviour[] moverComponents; // 移動系（PlayerMoverUI / FromM5）
    [SerializeField] private MonoBehaviour[] fireComponents;  // 射撃系（AllyBulletController / PlayerFireController / M5FireBridge等）
    [SerializeField] private MonoBehaviour[] enemySystems;    // 敵の弾幕/出現制御（EnemyDanmakuController等）

    [Header("UIパネル")]
    [SerializeField] private GameObject titlePanel;    // タイトル/スタートパネル
    [SerializeField] private GameObject gameOverPanel; // ゲームオーバーパネル

    [Header("開始設定")]
    [SerializeField, Min(0)] private int startLives = 3;            // 開始時の残機
    [SerializeField] private Vector2 startSpawnPosition = Vector2.zero; // 開始位置

    [Header("イベント（任意）")]
    public UnityEvent onGameStart;
    public UnityEvent onGameOverShown;

    private GameState state = GameState.Title;

    private void Awake()
    {
        GoTitle(); // 起動時はタイトル状態
    }

    private void OnEnable()
    {
        if (playerRespawn != null)
            playerRespawn.onGameOver.AddListener(HandleGameOver);
    }

    private void OnDisable()
    {
        if (playerRespawn != null)
            playerRespawn.onGameOver.RemoveListener(HandleGameOver);
    }

    private void Update()
    {
        // キーボードの簡易スタート/リトライ
        if ((state == GameState.Title || state == GameState.GameOver)
            && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            StartOrRetry();
        }
    }

    // ▼ 外部入口：UIボタン／M5ボタンからもこれを呼べばOK
    public void StartOrRetry()
    {
        if (state == GameState.Title || state == GameState.GameOver)
            StartGame();
    }

    public void GoTitle()
    {
        state = GameState.Title;
        SetActiveSafe(titlePanel, true);
        SetActiveSafe(gameOverPanel, false);

        SetMoversEnabled(false);
        SetFireEnabled(false);
        SetEnemySystemsEnabled(false);
        // 必要ならここでスコア/タイマー初期化のイベントを投げる
    }

    private void StartGame()
    {
        // 画面
        state = GameState.Playing;
        SetActiveSafe(titlePanel, false);
        SetActiveSafe(gameOverPanel, false);

        // フィールド初期化
        ClearBullets();

        // プレイヤー初期化（HP全快＆位置復帰＆残機セット）
        if (playerRespawn != null)
            playerRespawn.ResetLivesAndRespawnNow(startLives, startSpawnPosition);

        // システム有効化
        SetMoversEnabled(true);
        SetFireEnabled(true);
        SetEnemySystemsEnabled(true);

        onGameStart?.Invoke();
    }

    private void HandleGameOver()
    {
        // システム停止
        SetMoversEnabled(false);
        SetFireEnabled(false);
        SetEnemySystemsEnabled(false);

        // 画面表示
        state = GameState.GameOver;
        SetActiveSafe(gameOverPanel, true);
        SetActiveSafe(titlePanel, false);

        onGameOverShown?.Invoke();
    }

    // ===== ユーティリティ =====

    private void SetActiveSafe(GameObject go, bool v)
    {
        if (go && go.activeSelf != v) go.SetActive(v);
    }

    private void SetMoversEnabled(bool enabled)
    {
        ToggleArray(moverComponents, enabled);
    }

    private void SetFireEnabled(bool enabled)
    {
        ToggleArray(fireComponents, enabled);
    }

    private void SetEnemySystemsEnabled(bool enabled)
    {
        ToggleArray(enemySystems, enabled);
    }

    private void ToggleArray(MonoBehaviour[] arr, bool enabled)
    {
        if (arr == null) return;
        foreach (var m in arr) if (m) m.enabled = enabled;
    }

    private void ClearBullets()
    {
        if (!bulletLayer) return;
        for (int i = bulletLayer.childCount - 1; i >= 0; i--)
        {
            var child = bulletLayer.GetChild(i).gameObject;
            // プール運用なら ReturnToPool を呼ぶ。無ければ Destroy
            var poolable = child.GetComponent<Poolable>();
            if (poolable != null) poolable.TryRelease();
            else Destroy(child);
        }
    }
}
