using UnityEngine;
using UnityEngine.Events;

public class GameFlowController : MonoBehaviour
{
    public enum GameState { Title, Playing, GameOver }

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

    private GameState state = GameState.Title;

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

        // �V�X�e���L����
        SetMoversEnabled(true);
        SetFireEnabled(true);
        SetEnemySystemsEnabled(true);

        onGameStart?.Invoke();
    }

    private void HandleGameOver()
    {
        // �V�X�e����~
        SetMoversEnabled(false);
        SetFireEnabled(false);
        SetEnemySystemsEnabled(false);

        // ��ʕ\��
        state = GameState.GameOver;
        SetActiveSafe(gameOverPanel, true);
        SetActiveSafe(titlePanel, false);

        onGameOverShown?.Invoke();
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
