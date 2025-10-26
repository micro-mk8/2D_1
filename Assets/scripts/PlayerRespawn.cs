using System.Collections;
using UnityEngine;

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

    private float? nextInvincibleSecondsOverride = null; // ← 新規：次回だけ使う秒数（nullなら既定）
    private Vector2 lastDamagePosition;           // 直近の被弾時位置（＝死亡フレームも含む）
    private bool hasLastDamagePosition = false;

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

        // 念のため“今”の位置も最後に更新（保険）
        if (playerRoot)
        {
            lastAlivePosition = playerRoot.anchoredPosition;
            hasLastAlivePosition = true;
        }

        if (lives > 0)
        {
            lives--;
            if (hud) hud.SetLives(lives);
            StartCoroutine(CoRespawn());
        }
        else
        {
            // 残機ゼロ → ゲームオーバー（既存のまま）
        }
    }

    private IEnumerator CoRespawn()
    {
        waitingRespawn = true;

        yield return new WaitForSeconds(respawnDelay);

        if (playerRoot)
        {
            Vector2 respawnPos = spawnPosition;

            // ★ 優先順位：被弾位置 → 最後の生存位置 → 既定スポーン
            if (respawnAtLastPosition)
            {
                if (hasLastDamagePosition) respawnPos = lastDamagePosition;
                else if (hasLastAlivePosition) respawnPos = lastAlivePosition;
            }

            playerRoot.anchoredPosition = respawnPos;
        }

        if (playerHealth) playerHealth.ResetHP();

        // （既存）無敵時間の開始はそのまま
        float useInvSec = nextInvincibleSecondsOverride ?? defaultInvincibleSeconds;
        nextInvincibleSecondsOverride = null;
        if (invincibility != null) invincibility.Begin(useInvSec);

        waitingRespawn = false;
    }

    // ステージ開始時に呼ぶ初期化のとこ
    public void ResetLivesAndRespawnNow(int setLives, Vector2 setSpawn)
    {
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

}


