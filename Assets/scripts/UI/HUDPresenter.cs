using UnityEngine;
using TMPro;

public class HUDPresenter : MonoBehaviour
{
    [Header("UI Texts")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text scoreText;

    [Header("Sources")]
    [SerializeField] private UIHealth playerHealth;

    [Header("Initial Values")]
    [SerializeField] private int lives = 3;
    [SerializeField] private int score = 0;

    void OnEnable()
    {
        if (playerHealth != null)
        {
            // HPが減った/回復した/初期化された時にHP表記を更新
            playerHealth.onDamaged.AddListener(OnPlayerHPChanged);
            //playerHealth.onDead.AddListener(OnPlayerDead);
        }
        RefreshAll();
    }

    void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.onDamaged.RemoveListener(OnPlayerHPChanged);
            //playerHealth.onDead.RemoveListener(OnPlayerDead);
        }
    }

    // --- イベント受信 ---
    private void OnPlayerHPChanged(int cur, int max) => RefreshHP(cur, max);

    //private void OnPlayerDead()
    //{
    //    // とりあえず残機だけ減らす
    //    lives = Mathf.Max(0, lives - 1);
    //    if (livesText) livesText.text = $"LIVES {lives}";
    //}

    // --- 外部からスコア/残機を更新したい時用---
    public void AddScore(int delta)
    {
        // もう自前で += しない
        // ScoringManager が正しい currentScore を持っているので、
        // そっちを使ってスコア全体を表示に反映させる。
        if (ScoringManager.Instance != null)
        {
            int now = ScoringManager.Instance.CurrentScore;
            SetScore(now);
        }
    }


    public void SetLives(int value)
    {
        lives = Mathf.Max(0, value);
        if (livesText) livesText.text = $"LIVES {lives}";
    }

    // --- 表示更新 ---
    private void RefreshAll()
    {
        if (playerHealth != null)
            RefreshHP(playerHealth.CurrentHP, playerHealth.maxHP);
        else if (hpText) hpText.text = "HP --/--";

        if (livesText) livesText.text = $"LIVES {lives}";
        if (scoreText) scoreText.text = $"SCORE {score}";
    }

    private void RefreshHP(int cur, int max)
    {
        if (hpText) hpText.text = $"HP {cur}/{max}";
    }

    public void SetScore(int v){

        if (scoreText) scoreText.text = v.ToString();
    
    }   

}
