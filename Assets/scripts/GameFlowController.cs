using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

public class GameFlowController : MonoBehaviour
{
    public enum GameState { Title, Playing, GameOver, GameClear }

    [Header("�Q��")]
    [SerializeField] private PlayerRespawn playerRespawn;
    [SerializeField] private RectTransform bulletLayer;    // �e�̐e�i�C�ӁF�J�n���ɑ|���j
    [SerializeField] private MonoBehaviour[] moverComponents; // �ړ��n�iPlayerMoverUI / FromM5�j
    [SerializeField] private MonoBehaviour[] fireComponents;  // �ˌ��n�iAllyBulletController / PlayerFireController / M5FireBridge���j
    [SerializeField] private MonoBehaviour[] enemySystems;    // �G�̒e��/�o������iEnemyDanmakuController���j

    [Header("UI�p�l��")]
    [SerializeField] private GameObject titlePanel;    // �^�C�g��/�X�^�[�g�p�l��
    [SerializeField] private GameObject gameOverPanel; // �Q�[���I�[�o�[�p�l��

    [Header("�J�n�ݒ�")]
    [SerializeField, Min(0)] private int startLives = 3;            // �J�n���̎c�@
    [SerializeField] private Vector2 startSpawnPosition = Vector2.zero; // �J�n�ʒu

    [Header("�C�x���g�i�C�Ӂj")]
    public UnityEvent onGameStart;
    public UnityEvent onGameOverShown;

    [Header("hud���Z�b�g")]
    [SerializeField] private Transform enemyRoot;
    [SerializeField] private ScoringManager scoring;
    [SerializeField] private HUDPresenter hudPresenter;

    [Header("Game Clear UI")]
    [SerializeField] private GameObject clearPanel;            // �� GameClearCanvas �����蓖��
    [SerializeField] private CanvasGroup clearCanvasGroup;     // �� GameClearCanvas �� CanvasGroup
    [SerializeField] private TMP_Text clearScoreText;              // �� SCORE �e�L�X�g
    [SerializeField] private TMP_Text clearTimeText;               // �� TIME �e�L�X�g
    [SerializeField, Min(0f)] private float gameClearFadeSec = 0.25f;

    [Header("Score & Timer")]


    private GameState state = GameState.Title;

    private float runTimeSec = 0f;
    private bool runTimer = false;


    private void Awake()
    {
        GoTitle(); // �N�����̓^�C�g�����
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
        if (runTimer) runTimeSec += Time.unscaledDeltaTime;

        // �L�[�{�[�h�̊ȈՃX�^�[�g/���g���C
        if ((state == GameState.Title || state == GameState.GameOver)
            && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            StartOrRetry();
        }
    }

    // �� �O�������FUI�{�^���^M5�{�^�������������Ăׂ�OK
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
        // �K�v�Ȃ炱���ŃX�R�A/�^�C�}�[�������̃C�x���g�𓊂���
    }

    private void StartGame()
    {
        // ���
        state = GameState.Playing;
        SetActiveSafe(titlePanel, false);
        SetActiveSafe(gameOverPanel, false);
        
        
        // �t�B�[���h������
        ClearBullets();

        // �v���C���[�������iHP�S�����ʒu���A���c�@�Z�b�g�j
        if (playerRespawn != null)
            playerRespawn.ResetLivesAndRespawnNow(startLives, startSpawnPosition);

        Reset();

        runTimeSec = 0f;
        runTimer = true;  

        if (clearCanvasGroup) clearCanvasGroup.alpha = 0f;
        SetActiveSafe(clearPanel, false);

        state = GameState.Playing;

        // �V�X�e���L����
        SetMoversEnabled(true);
        SetFireEnabled(true);
        SetEnemySystemsEnabled(true);

        onGameStart?.Invoke();
    }

    // GameFlowController.cs �̃N���X���ɒǉ�
    private void HandleGameOver()
    {
        // �i�s��~
        SetMoversEnabled(false);
        SetFireEnabled(false);
        SetEnemySystemsEnabled(false);
        runTimer = false;

        // ��ʕ\����ؑ�
        state = GameState.GameOver;
        SetActiveSafe(gameOverPanel, true);
        SetActiveSafe(titlePanel, false);

        onGameOverShown?.Invoke();
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


    private void Reset(){

        if (scoring) scoring.ResetScore();
        if (hudPresenter) hudPresenter.SetScore(0);

        if (enemyRoot){

            var healths = enemyRoot.GetComponentsInChildren<UIHealth>(true);
            foreach ( var h in healths )
                h.ResetHP();

            var motions = enemyRoot.GetComponentsInChildren<EnemyMotionUI>(true);
            foreach ( var m in motions )
                m.ResetToStartForRetry();
            
        }

    }

    // ===== ���[�e�B���e�B =====

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
            // �v�[���^�p�Ȃ� ReturnToPool ���ĂԁB������� Destroy
            var poolable = child.GetComponent<Poolable>();
            if (poolable != null) poolable.TryRelease();
            else Destroy(child);
        }
    }
}
