using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerRespawn : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private UIHealth playerHealth;          // PlayerRoot の UIHealth
    [SerializeField] private RectTransform playerRoot;       // PlayerRoot（座標）
    [SerializeField] private HUDPresenter hud;               // 右パネル表示

    [Header("リス地")]
    [SerializeField] private bool respawnAtLastPosition = true;
    private Vector2 lastAlivePosition;
    private bool hasLastAlivePosition = false;

    [Header("設定")]
    [SerializeField, Min(0)] private int initialLives = 3;   // 初期残機
    [SerializeField, Min(0f)] private float respawnDelay = 1.0f; // リスポーン待ち秒
    [SerializeField] private Vector2 spawnPosition = Vector2.zero; // 復活位置（anchoredPosition）
    [SerializeField] private InvincibilityController invincibility; // ← 新規：同じPlayerRootに付ける
    [SerializeField, Min(0f)] private float defaultInvincibleSeconds = 2.0f; // ← 新規：省略時の既定秒
    [SerializeField] private MonoBehaviour[] moverComponents; // PlayerMoverUI, PlayerMoverFromM5 などを割当


    [Header("挙動切替")]
    [SerializeField] private bool noRespawnMode = true;        // ★ これを true にすると“復活しない”モード
    [SerializeField] private bool restoreHPOnDeath = true;     // ★ 死亡時にHP全回復するか（点滅中に被弾させないためtrue）

    [Header("無敵中の射撃制御")]
    [SerializeField] private MonoBehaviour[] fireComponents;   // ★ 無敵中に「enabled=false」にしたい射撃系スクリプト群
    // 例：AllyBulletController / PlayerFireController / M5FireBridge（発射側）など

    [Header("イベント")] 
    public UnityEvent onGameOver;





    // 無敵時間の一時上書き（使わない時は null）
    private float? nextInvincibleSecondsOverride = null;
    private Vector2 lastDamagePosition;           // 直近の被弾時位置（＝死亡フレームも含む）
    private bool hasLastDamagePosition = false;
    private Vector2 deathPosition;          
    private bool hasDeathPosition      = false;
    
    private int lives;
    private bool waitingRespawn = false;

    //private void Update()
    //{
    //    if (!waitingRespawn && playerHealth && playerHealth.CurrentHP > 0 && playerRoot)
    //    {
    //        lastAlivePosition = playerRoot.anchoredPosition;
    //       hasLastAlivePosition = true;
    //    }
    //}

    void OnEnable()
    {
        lives = initialLives;
        if (hud) hud.SetLives(lives);

        if (playerHealth)
        {
            playerHealth.onDead.AddListener(OnPlayerDead);
            playerHealth.onDamaged.AddListener(OnPlayerDamaged);   // ★ 追加
        }
    }

    void OnDisable()
    {
        if (playerHealth)
        {
            playerHealth.onDead.RemoveListener(OnPlayerDead);
            playerHealth.onDamaged.RemoveListener(OnPlayerDamaged); // ★ 追加
        }
    }

    private void OnPlayerDamaged(int damage, int currentHP)
    {
        if (playerRoot)
        {
            // “被弾した瞬間”の座標を記録（死亡フレーム含む）
            lastDamagePosition = playerRoot.anchoredPosition;
            hasLastDamagePosition = true;
        }
    }


    private void OnPlayerDead()
    {
        if (waitingRespawn) return;

        if (lives > 0)
        {
            lives--;
            if (hud) hud.SetLives(lives);

            if (noRespawnMode)
            {
                // ★ 復活しないフローへ
                StartCoroutine(CoNoRespawn());
            }
            else
            {
                // ★ 既存の復活フロー（残す）
                //StartCoroutine(CoRespawn());
            }
        }
        else
        {
            // 残機ゼロ → ゲームオーバー（必要ならここに処理）
            onGameOver?.Invoke();
        }
    }


    private IEnumerator CoNoRespawn()
    {
        // 位置は一切いじらない（テレポ無し）

        // HPをその場で回復（推奨：無敵中に再死亡しないため）
        if (restoreHPOnDeath && playerHealth)
            playerHealth.ResetHP();

        // 無敵時間（可変指定に対応）
        float useInvSec = nextInvincibleSecondsOverride ?? defaultInvincibleSeconds;
        nextInvincibleSecondsOverride = null;

        // 無敵中は射撃を停止（移動は可）
        SetFiringEnabled(false);

        // 無敵開始（点滅・当たり無効は既存のイベント連携で作動）
        if (invincibility)
            invincibility.Begin(useInvSec);

        // 無敵時間だけ待つ
        yield return new WaitForSeconds(useInvSec);

        // 無敵終了後、射撃を再開
        SetFiringEnabled(true);
    }

    private void SetMoversEnabled(bool enabled)
    {
        if (moverComponents == null) return;
        foreach (var m in moverComponents)
            if (m) m.enabled = enabled;
    }



    private void SetFiringEnabled(bool enabled)
    {
        if (fireComponents == null) return;
        foreach (var c in fireComponents)
            if (c) c.enabled = enabled;
    }





    // ステージ開始時に呼ぶ初期化のとこ
    public void ResetLivesAndRespawnNow(int setLives, Vector2 setSpawn)
    {

        StopAllCoroutines();
        waitingRespawn = false;
        nextInvincibleSecondsOverride = null;

        initialLives = Mathf.Max(0, setLives);
        lives = initialLives;
        spawnPosition = setSpawn;
        if (hud) hud.SetLives(lives);

        if (playerRoot) playerRoot.anchoredPosition = spawnPosition;

        if (playerHealth) playerHealth.ResetHP();
    }

    public void SetNextInvincibility(float seconds)
    {
        nextInvincibleSecondsOverride = Mathf.Max(0f, seconds);
    }

     private void LateUpdate()
    {
        if (waitingRespawn) return;
        if (!playerHealth || playerHealth.CurrentHP <= 0) return;
        if (!playerRoot) return;

        lastAlivePosition = playerRoot.anchoredPosition;
        hasLastAlivePosition = true;
    }


}



