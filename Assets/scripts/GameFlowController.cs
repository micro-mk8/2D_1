using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

public class GameFlowController : MonoBehaviour
{
    public enum GameState { Title, Playing, GameOver, GameClear }

    [Header("参照")]
    [SerializeField] private PlayerRespawn playerRespawn;
    [SerializeField] private RectTransform bulletLayer;          // 弾をぶら下げている親
    [SerializeField] private MonoBehaviour[] moverComponents;    // PlayerMoverFromM5 / PlayerMoverUI など
    [SerializeField] private MonoBehaviour[] fireComponents;     // AllyBulletController / M5FireBridge など
    [SerializeField] private MonoBehaviour[] enemySystems;       // 敵AIや弾幕系など

    [Header("UIパネル")]
    [SerializeField] private GameObject titlePanel;              // Title/PressStart系
    [SerializeField] private GameObject gameOverPanel;           // GameOverパネル

    [Header("開始設定")]
    [SerializeField, Min(0)] private int startLives = 3;
    [SerializeField] private Vector2 startSpawnPosition = Vector2.zero;

    [Header("イベント（任意）")]
    public UnityEvent onGameStart;
    public UnityEvent onGameOverShown;

    [Header("hudリセット")]
    [SerializeField] private Transform enemyRoot;                // 既存の敵たちがぶら下がってる親
    [SerializeField] private ScoringManager scoring;
    [SerializeField] private HUDPresenter hudPresenter;

    // 初期敵構成のPrefab（敵を完全に作り直したいとき用。今は未使用でもOK）
    [SerializeField] private GameObject initialEnemiesPrefab;

    [Header("Game Clear UI")]
    [SerializeField] private GameObject clearPanel;              // GameClearCanvas
    [SerializeField] private CanvasGroup clearCanvasGroup;       // そのCanvasGroup
    [SerializeField] private TMP_Text clearScoreText;
    [SerializeField] private TMP_Text clearTimeText;
    [SerializeField, Min(0f)] private float gameClearFadeSec = 0.25f;

    [SerializeField] private StageClear stageClear;              // 全滅監視用
    [SerializeField] private M5FireBridge m5Bridge;              // M5入力ブリッジ

    private GameState state = GameState.Title;

    // クリアタイム計測
    private float runTimeSec = 0f;
    private bool runTimer = false;

    // ===== Unity標準イベント =====

    private void Awake()
    {
        // 起動時はタイトル状態にしておく
        GoTitle();
    }

    private void OnEnable()
    {
        // プレイヤー死亡→GameOverの通知
        if (playerRespawn != null)
            playerRespawn.onGameOver.AddListener(HandleGameOver);
    }

    private void OnDisable()
    {
        if (playerRespawn != null)
            playerRespawn.onGameOver.RemoveListener(HandleGameOver);
    }

    private void Start()
    {
        // m5Bridgeの "スタート/リトライ押したよ" イベントを購読しておく
        // （m5Bridgeが有効な限り、ここから OnM5ButtonPressed が呼ばれる）
        if (m5Bridge != null)
        {
            var ev = m5Bridge.GetStartRetryEvent();
            ev.RemoveListener(OnM5ButtonPressed); // 二重登録防止
            ev.AddListener(OnM5ButtonPressed);
        }
    }

    private void Update()
    {
        // --- 1) Spaceキーで Start / Retry ---
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnM5ButtonPressed(); // Spaceも最終的に同じ入口を通す
        }

        // --- 2) M5の「FIRE」生信号でも Start / Retry ---
        // ここは毎フレーム直接チェック（m5BridgeのMonoBehaviourが無効でもudp.latestRawは読める想定）
        if (m5Bridge != null && m5Bridge.udp != null)
        {
            string raw = m5Bridge.udp.latestRaw;
            if (!string.IsNullOrEmpty(raw) && raw.StartsWith("FIRE"))
            {
                OnM5ButtonPressed();
            }
        }

        // --- 3) タイマー進行（プレイ中のみ） ---
        if (runTimer)
        {
            runTimeSec += Time.deltaTime;
        }
    }

    // ===== 外部・共通入口 =====
    // Spaceキー / M5ボタン / UIボタン から全てここを通す
    private void OnM5ButtonPressed()
    {
        switch (state)
        {
            case GameState.Title:
                // タイトル中はゲーム開始
                StartGame();
                break;

            case GameState.GameOver:
            case GameState.GameClear:
                // GameOver/GameClear中はリトライ
                StartOrRetry();
                break;

            case GameState.Playing:
                // プレイ中は弾発射ボタンなので、ここでは何もしない
                // （実際の発射はM5FireBridge / AllyBulletController側に任せる）
                break;
        }
    }

    // UIからも呼びたい場合があるのでpublicのまま残す
    public void StartOrRetry()
    {
        if (state == GameState.Title ||
            state == GameState.GameOver ||
            state == GameState.GameClear)
        {
            StartGame();
        }
    }

    // ===== 状態遷移 =====

    // タイトル状態に戻す
    public void GoTitle()
    {
        state = GameState.Title;

        SetActiveSafe(titlePanel, true);
        SetActiveSafe(gameOverPanel, false);

        SetActiveSafe(clearPanel, false);
        if (clearCanvasGroup) clearCanvasGroup.alpha = 0f;

        // 入力・射撃・敵システムは止めておく
        SetMoversEnabled(false);
        SetFireEnabled(false);
        SetEnemySystemsEnabled(false);

        // HUD・スコアの初期化（見た目上のリセットだけでOK）
        ResetScoreAndHudOnly();

        runTimer = false;
        runTimeSec = 0f;
        Time.timeScale = 1f;
    }

    // ゲーム開始 / リトライ時に呼ばれる本体
    private void StartGame()
    {
        // 時間を動かす
        Time.timeScale = 1f;

        // 状態切り替え
        state = GameState.Playing;

        // 各パネルOFF
        SetActiveSafe(titlePanel, false);
        SetActiveSafe(gameOverPanel, false);
        SetActiveSafe(clearPanel, false);
        if (clearCanvasGroup) clearCanvasGroup.alpha = 0f;

        // 全弾消し
        ClearBullets();

        // 敵を復活
        ReviveAllEnemies();

        // プレイヤー復活＆HP全快＆Livesリセット
        if (playerRespawn != null)
        {
            playerRespawn.ResetLivesAndRespawnNow(startLives, startSpawnPosition);

            var playerHealth = playerRespawn.GetComponentInChildren<UIHealth>(true);
            if (playerHealth != null)
            {
                playerHealth.gameObject.SetActive(true);
                playerHealth.ResetHP();
            }
        }

        // スコアやHUDの見た目をリセット
        ResetScoreAndHudOnly();

        // タイマー開始
        runTimeSec = 0f;
        runTimer = true;

        // ステージクリア監視を有効化（敵全滅→HandleGameClear呼ばせるため）
        if (stageClear != null)
            stageClear.enabled = true;

        // 入力・射撃・敵システムを有効化
        SetMoversEnabled(true);
        SetFireEnabled(true);
        SetEnemySystemsEnabled(true);

        // コールバック
        onGameStart?.Invoke();
    }

    // GameOver時に PlayerRespawn から呼ばれる
    private void HandleGameOver()
    {
        // プレイ停止
        SetMoversEnabled(false);
        SetFireEnabled(false);
        SetEnemySystemsEnabled(false);

        runTimer = false;

        state = GameState.GameOver;

        // GameOverパネルON, タイトルOFF, クリア画面OFF
        SetActiveSafe(gameOverPanel, true);
        SetActiveSafe(titlePanel, false);
        SetActiveSafe(clearPanel, false);

        // コールバック
        onGameOverShown?.Invoke();

        // 時間は止めない（止めるとUpdate自体も動くけどTime.timeScaleだけが0になるので
        // M5BridgeやこのUpdate内の処理はMonoBehaviour.Updateとしては動く。
        // もし完全停止したいなら Time.timeScale = 0f; にしてOK。
    }

    [ContextMenu("DEBUG/HandleGameClear()")]
    public void HandleGameClear()
    {
        Debug.Log("[GF] HandleGameClear called");

        // 停止
        SetMoversEnabled(false);
        SetFireEnabled(false);
        SetEnemySystemsEnabled(false);

        runTimer = false;
        state = GameState.GameClear;

        // クリア画面を表示
        SetActiveSafe(clearPanel, true);
        if (clearCanvasGroup != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroup(clearCanvasGroup, 0f, 1f, gameClearFadeSec));
        }

        // スコア最終確定（クリアボーナスなど）
        if (scoring != null)
        {
            scoring.ComputeAndAddClearTimeBonus();
        }

        // UIにタイムとスコア反映
        if (clearScoreText && scoring)
            clearScoreText.text = $"SCORE: {scoring.CurrentScore}";
        if (clearTimeText)
            clearTimeText.text = $"TIME: {FormatTime(runTimeSec)}";

        Debug.Log("[GF] HandleGameClear finished, state=" + state);

        // クリア画面中はゲーム内の時間を止める
        Time.timeScale = 0f;
    }

    private System.Collections.IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float dur)
    {
        if (!cg) yield break;
        cg.alpha = from;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
            yield return null;
        }
        cg.alpha = to;
    }

    // ===== 復活・リセット系 =====

    // スコアUIとHUDだけを初期状態に戻す
    private void ResetScoreAndHudOnly()
    {
        if (scoring) scoring.ResetScore();
        if (hudPresenter) hudPresenter.SetScore(0);
    }

    // 敵を「再使用」する：非アクティブにしていた敵をもう一回生き返らせる
    private void ReviveAllEnemies()
    {
        if (!enemyRoot) return;

        // 非アクティブも含めて全部とりたいので true を付ける
        var allHealth = enemyRoot.GetComponentsInChildren<UIHealth>(true);

        foreach (var h in allHealth)
        {
            if (!h) continue;

            // UIHealth に実装した Revive():
            //   - gameObject.SetActive(true)
            //   - currentHP = maxHP
            //   - isDead = false
            //   - onDamaged.Invoke() 等の見た目リフレッシュ
            h.Revive();

            // もし敵の移動スクリプト(EnemyMotionUIなど)があるなら初期位置に戻す
            var motion = h.GetComponent<EnemyMotionUI>();
            if (motion != null)
            {
                motion.ResetToStartForRetry();
            }
        }
    }

    // すべての弾を片付ける
    private void ClearBullets()
    {
        if (!bulletLayer) return;

        for (int i = bulletLayer.childCount - 1; i >= 0; i--)
        {
            var child = bulletLayer.GetChild(i).gameObject;

            // オブジェクトプール運用なら返却、それ以外ならDestroy
            var poolable = child.GetComponent<Poolable>();
            if (poolable != null)
            {
                poolable.TryRelease();
            }
            else
            {
                Destroy(child);
            }
        }
    }

    // ===== 小物ヘルパ =====

    private void SetActiveSafe(GameObject go, bool v)
    {
        if (!go) return;
        if (go.activeSelf != v) go.SetActive(v);
    }

    private void SetMoversEnabled(bool en)
    {
        ToggleArray(moverComponents, en);
    }

    private void SetFireEnabled(bool en)
    {
        ToggleArray(fireComponents, en);
    }

    private void SetEnemySystemsEnabled(bool en)
    {
        ToggleArray(enemySystems, en);
    }

    private void ToggleArray(MonoBehaviour[] arr, bool en)
    {
        if (arr == null) return;
        foreach (var m in arr)
        {
            if (m) m.enabled = en;
        }
    }

    private string FormatTime(float sec)
    {
        if (sec < 0f) sec = 0f;
        int m = (int)(sec / 60f);
        float s = sec - m * 60;
        return $"{m:00}:{s:00.000}";
    }
}
