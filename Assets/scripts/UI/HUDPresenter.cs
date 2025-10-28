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
            // HP��������/�񕜂���/���������ꂽ����HP�\�L���X�V
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

    // --- �C�x���g��M ---
    private void OnPlayerHPChanged(int cur, int max) => RefreshHP(cur, max);

    //private void OnPlayerDead()
    //{
    //    // �Ƃ肠�����c�@�������炷
    //    lives = Mathf.Max(0, lives - 1);
    //    if (livesText) livesText.text = $"LIVES {lives}";
    //}

    // --- �O������X�R�A/�c�@���X�V���������p---
    public void AddScore(int delta)
    {
        score = Mathf.Max(0, score + delta);
        if (scoreText) scoreText.text = $"SCORE {score}";
    }

    public void SetLives(int value)
    {
        lives = Mathf.Max(0, value);
        if (livesText) livesText.text = $"LIVES {lives}";
    }

    // --- �\���X�V ---
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
