using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

public class GameFlowController : MonoBehaviour
{
    public enum GameState { Title, Playing, GameOver, GameClear }

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

    [Header("hudリセット")]
    [SerializeField] private Transform enemyRoot;
    [SerializeField] private ScoringManager scoring;
    [SerializeField] private HUDPresenter hudPresenter;

    [SerializeField] private GameObject initialEnemiesPrefab;


    [Header("Game Clear UI")]
    [SerializeField] private GameObject clearPanel;            // ← GameClearCanvas を割り当て
    [SerializeField] private CanvasGroup clearCanvasGroup;     // ← GameClearCanvas の CanvasGroup
    [SerializeField] private TMP_Text clearScoreText;              // ← SCORE テキスト
    [SerializeField] private TMP_Text clearTimeText;               // ← TIME テキスト
    [SerializeField, Min(0f)] private float gameClearFadeSec = 0.25f;

    [SerializeField] private StageClear stageClear;  // インスペクタで割り当てる    
    [SerializeField] private M5FireBridge m5Bridge;




    private GameState state = GameState.Title;

    private float runTimeSec = 0f;
    private bool runTimer = false;


    private void Awake()
    {
        GoTitle(); // 起動時はタイトル状態
    }

    private void OnEnable()
    {
        if (playerRespawn != null)
            playerRespawn.onGameOver.AddListener(HandleGameOver);

        if (m5Bridge != null)
        {
            // M5からの"FIRE"通知イベントを購読する
            m5Bridge.GetStartRetryEvent().AddListener(OnM5ButtonPressed);
        }
    }

    private void OnDisable()
    {
        if (playerRespawn != null)
            playerRespawn.onGameOver.RemoveListener(HandleGameOver);

        if (m5Bridge != null)
        {
            // 購読解除
            m5Bridge.GetStartRetryEvent().RemoveListener(OnM5ButtonPressed);
        }
    }


    private void Update()
    {
        if (runTimer) runTimeSec += Time.unscaledDeltaTime;

        bool pressedRestart =
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return);

        bool canRestart =
            (state == GameState.Title ||
             state == GameState.GameOver ||
             state == GameState.GameClear);

        if (canRestart && pressedRestart)
        {
            StartOrRetry();
        }
    }


    // ▼ 外部入口：UIボタン／M5ボタンからもこれを呼べばOK
    public void StartOrRetry()
    {
        if (state == GameState.Title ||
            state == GameState.GameOver ||
            state == GameState.GameClear)
        {
            StartGame();
        }
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
        Time.timeScale = 1f;

        state = GameState.Playing;
        SetActiveSafe(titlePanel, false);
        SetActiveSafe(gameOverPanel, false);

        SetActiveSafe(clearPanel, false);
        if (clearCanvasGroup) clearCanvasGroup.alpha = 0f;

        // まず弾を全部消す
        ClearBullets();

        // ★敵を復活させる（非アクティブ→アクティブ、HP満タン、初期位置へ）
        ReviveAllEnemies();

        // プレイヤー復帰（ライフと位置リセット）
        if (playerRespawn != null){
            playerRespawn.ResetLivesAndRespawnNow(startLives, startSpawnPosition);
                var playerHealth = playerRespawn.GetComponentInChildren<UIHealth>(true);
            if (playerHealth != null)
            {
                playerHealth.gameObject.SetActive(true);
                playerHealth.ResetHP();
            }
        }

        // スコアなどのリセット
        ResetScoreAndHudOnly();

        // タイマー初期化
        runTimeSec = 0f;
        runTimer = true;

            // ←これを追加
        if (stageClear != null)
            stageClear.enabled = true;

        state = GameState.Playing;

        // 入力・射撃・敵システム再有効化
        SetMoversEnabled(true);
        SetFireEnabled(true);
        SetEnemySystemsEnabled(true);

        onGameStart?.Invoke();
    }


        // EnemyRoot の中身を、初期編成プレハブから作り直す
    //private void ResetEnemiesFromPrefab()
    //{
    //    if (enemyRoot == null)
    //    {
    //        Debug.LogWarning("enemyRoot がセットされていません");
    //        return;
    //    }
    //
    //    // 1) 既存の敵を全消し
    //    for (int i = enemyRoot.childCount - 1; i >= 0; i--)
    //    {
    //        var child = enemyRoot.GetChild(i);
    //        Destroy(child.gameObject);
    //    }
    //
    //    // 2) 初期プレハブから新しく複製
    //    if (initialEnemiesPrefab != null)
    //    {
    //        var clone = Instantiate(initialEnemiesPrefab, enemyRoot);
    //        // 位置の基準を0にそろえる（UIなのでanchoredPositionを触るほうがいい場合もある）
    //        var rt = clone.transform as RectTransform;
    //        if (rt != null)
    //        {
    //            rt.anchoredPosition = Vector2.zero;
    //            rt.localRotation = Quaternion.identity;
    //            rt.localScale = Vector3.one;
    //        }
    //        else
    //        {
    //            clone.transform.localPosition = Vector3.zero;
    //            clone.transform.localRotation = Quaternion.identity;
    //            clone.transform.localScale = Vector3.one;
    //        }
    //    }
    //    else
    //    {
    //        Debug.LogWarning("initialEnemiesPrefab が割り当てられていません");
    //    }
    //}

        // 敵を再利用して復活させる
    private void ReviveAllEnemies()
    {
        if (!enemyRoot) return;

        // enemyRoot 配下の全敵の UIHealth を取得（非アクティブも含めて拾いたいので true）
        var allHealth = enemyRoot.GetComponentsInChildren<UIHealth>(true);

        foreach (var h in allHealth)
        {
            if (h == null) continue;

            // 1) ゲームオブジェクトを再アクティブ化し、HP満タンに戻す
            h.Revive();  // ← UIHealthに追加したやつ。SetActive(true), HP=maxHP, isDead=false, onDamaged発火

            // 2) 動きをリセット（もし動きスクリプトを持っているなら）
            var motion = h.GetComponent<EnemyMotionUI>();
            if (motion != null)
            {
                // あなたのプロジェクトには EnemyMotionUI.ResetToStartForRetry() があるので、それを呼ぶ
                motion.ResetToStartForRetry();
            }
        }
    }





    // GameFlowController.cs のクラス内に追加
    private void HandleGameOver()
    {
        // 進行停止
        SetMoversEnabled(false);
        SetFireEnabled(false);
        SetEnemySystemsEnabled(false);
        runTimer = false;

        // 画面表示を切替
        state = GameState.GameOver;
        SetActiveSafe(gameOverPanel, true);
        SetActiveSafe(titlePanel, false);

        onGameOverShown?.Invoke();
    }


    [ContextMenu("DEBUG/HandleGameClear()")]
    public void HandleGameClear()
    {
        Debug.Log("[GF] HandleGameClear called");

        
        // プレイを停止
        SetMoversEnabled(false);
        SetFireEnabled(false);
        SetEnemySystemsEnabled(false);
        runTimer = false;

        state = GameState.GameClear;

        // ★ GameClearCanvas を前面に出す
        SetActiveSafe(clearPanel, true);
        if (clearCanvasGroup != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroup(clearCanvasGroup, 0f, 1f, gameClearFadeSec));
        }

        // クリアボーナス加算（必要なら）
        if (scoring != null)
        {
            scoring.ComputeAndAddClearTimeBonus();
        }

        // 表示
        if (clearScoreText && scoring)
            clearScoreText.text = $"SCORE: {scoring.CurrentScore}";
        if (clearTimeText)
            clearTimeText.text = $"TIME: {FormatTime(runTimeSec)}";


        Debug.Log("[GF] HandleGameClear finished, state=" + state);
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


    // スコアだけ・HUDだけを初期化する
    private void ResetScoreAndHudOnly()
    {
        if (scoring) scoring.ResetScore();
        if (hudPresenter) hudPresenter.SetScore(0);
    }

    private void OnM5ButtonPressed()
    {
        // 状態に応じてSpaceキーと同じ動作を行う
        switch (state)
        {
            case GameState.Title:
                StartGame();
                break;

            case GameState.GameOver:
            case GameState.GameClear:
                StartOrRetry();
                break;

            default:
                // プレイ中は弾発射なので何もしない
                break;
        }
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

    private string FormatTime(float sec)
    {
        if (sec < 0f) sec = 0f;
        int m = (int)(sec / 60f);
        float s = sec - m * 60;
        return $"{m:00}:{s:00.000}";
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
