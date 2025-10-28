using System.Collections;
using UnityEngine;
using UnityEngine.Events;

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
    [SerializeField] private MonoBehaviour[] moverComponents; // PlayerMoverUI, PlayerMoverFromM5 �Ȃǂ�����


    [Header("�����ؑ�")]
    [SerializeField] private bool noRespawnMode = true;        // �� ����� true �ɂ���Ɓg�������Ȃ��h���[�h
    [SerializeField] private bool restoreHPOnDeath = true;     // �� ���S����HP�S�񕜂��邩�i�_�Œ��ɔ�e�����Ȃ�����true�j

    [Header("���G���̎ˌ�����")]
    [SerializeField] private MonoBehaviour[] fireComponents;   // �� ���G���Ɂuenabled=false�v�ɂ������ˌ��n�X�N���v�g�Q
    // ��FAllyBulletController / PlayerFireController / M5FireBridge�i���ˑ��j�Ȃ�

    [Header("�C�x���g")] 
    public UnityEvent onGameOver;





    // ���G���Ԃ̈ꎞ�㏑���i�g��Ȃ����� null�j
    private float? nextInvincibleSecondsOverride = null;
    private Vector2 lastDamagePosition;           // ���߂̔�e���ʒu�i�����S�t���[�����܂ށj
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

        if (lives > 0)
        {
            lives--;
            if (hud) hud.SetLives(lives);

            if (noRespawnMode)
            {
                // �� �������Ȃ��t���[��
                StartCoroutine(CoNoRespawn());
            }
            else
            {
                // �� �����̕����t���[�i�c���j
                //StartCoroutine(CoRespawn());
            }
        }
        else
        {
            // �c�@�[�� �� �Q�[���I�[�o�[�i�K�v�Ȃ炱���ɏ����j
            onGameOver?.Invoke();
        }
    }


    private IEnumerator CoNoRespawn()
    {
        // �ʒu�͈�؂�����Ȃ��i�e���|�����j

        // HP�����̏�ŉ񕜁i�����F���G���ɍĎ��S���Ȃ����߁j
        if (restoreHPOnDeath && playerHealth)
            playerHealth.ResetHP();

        // ���G���ԁi�ώw��ɑΉ��j
        float useInvSec = nextInvincibleSecondsOverride ?? defaultInvincibleSeconds;
        nextInvincibleSecondsOverride = null;

        // ���G���͎ˌ����~�i�ړ��͉j
        SetFiringEnabled(false);

        // ���G�J�n�i�_�ŁE�����薳���͊����̃C�x���g�A�g�ō쓮�j
        if (invincibility)
            invincibility.Begin(useInvSec);

        // ���G���Ԃ����҂�
        yield return new WaitForSeconds(useInvSec);

        // ���G�I����A�ˌ����ĊJ
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





    // �X�e�[�W�J�n���ɌĂԏ������̂Ƃ�
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



