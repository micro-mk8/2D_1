using System.Collections;
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [Header("�Q��")]
    [SerializeField] private UIHealth playerHealth;          // PlayerRoot �� UIHealth
    [SerializeField] private RectTransform playerRoot;       // PlayerRoot�i���W�j
    [SerializeField] private HUDPresenter hud;               // �E�p�l���\��

    [Header("���X�n")]
    [SerializeField] private bool respawnAtLastPosition = true;
    private Vector2 lastAlivePosition;
    private bool hasLastAlivePosition = false;

    [Header("�ݒ�")]
    [SerializeField, Min(0)] private int initialLives = 3;   // �����c�@
    [SerializeField, Min(0f)] private float respawnDelay = 1.0f; // ���X�|�[���҂��b
    [SerializeField] private Vector2 spawnPosition = Vector2.zero; // �����ʒu�ianchoredPosition�j
    [SerializeField] private InvincibilityController invincibility; // �� �V�K�F����PlayerRoot�ɕt����
    [SerializeField, Min(0f)] private float defaultInvincibleSeconds = 2.0f; // �� �V�K�F�ȗ����̊���b

    private float? nextInvincibleSecondsOverride = null; // �� �V�K�F���񂾂��g���b���inull�Ȃ����j
    private Vector2 lastDamagePosition;           // ���߂̔�e���ʒu�i�����S�t���[�����܂ށj
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
            playerHealth.onDamaged.AddListener(OnPlayerDamaged);   // �� �ǉ�
        }
    }

    void OnDisable()
    {
        if (playerHealth)
        {
            playerHealth.onDead.RemoveListener(OnPlayerDead);
            playerHealth.onDamaged.RemoveListener(OnPlayerDamaged); // �� �ǉ�
        }
    }

    private void OnPlayerDamaged(int damage, int currentHP)
    {
        if (playerRoot)
        {
            // �g��e�����u�ԁh�̍��W���L�^�i���S�t���[���܂ށj
            lastDamagePosition = playerRoot.anchoredPosition;
            hasLastDamagePosition = true;
        }
    }


    private void OnPlayerDead()
    {
        if (waitingRespawn) return;

        // �O�̂��߁g���h�̈ʒu���Ō�ɍX�V�i�ی��j
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
            // �c�@�[�� �� �Q�[���I�[�o�[�i�����̂܂܁j
        }
    }

    private IEnumerator CoRespawn()
    {
        waitingRespawn = true;

        yield return new WaitForSeconds(respawnDelay);

        if (playerRoot)
        {
            Vector2 respawnPos = spawnPosition;

            // �� �D�揇�ʁF��e�ʒu �� �Ō�̐����ʒu �� ����X�|�[��
            if (respawnAtLastPosition)
            {
                if (hasLastDamagePosition) respawnPos = lastDamagePosition;
                else if (hasLastAlivePosition) respawnPos = lastAlivePosition;
            }

            playerRoot.anchoredPosition = respawnPos;
        }

        if (playerHealth) playerHealth.ResetHP();

        // �i�����j���G���Ԃ̊J�n�͂��̂܂�
        float useInvSec = nextInvincibleSecondsOverride ?? defaultInvincibleSeconds;
        nextInvincibleSecondsOverride = null;
        if (invincibility != null) invincibility.Begin(useInvSec);

        waitingRespawn = false;
    }

    // �X�e�[�W�J�n���ɌĂԏ������̂Ƃ�
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


