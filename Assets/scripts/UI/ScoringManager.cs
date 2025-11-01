using UnityEngine;
using UnityEngine.Events;

public class ScoringManager : MonoBehaviour
{
    // -------- �V���O���g���i�ǂ�����ł��Ăׂ�j --------
    public static ScoringManager Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        levelStartTime = Time.time;
    }

    [Header("�\����i�E�p�l���j")]
    [SerializeField] private HUDPresenter hud; // HUDPresenter

    [Header("���_�{���i���Ȃ��������ł��܂��j")]
    [Tooltip("�^�����_���[�W1�ɂ����_��")]
    public int pointsPerDamage = 5;          //1�_���[�W=5�_
    [Tooltip("�G��1�̌��j�����Ƃ��̃{�[�i�X")]
    public int pointsPerKill = 200;        //���j=200�_

    [Header("�N���A�^�C���̊��Z�i���X�e�b�v�Ŏg�p�j")]
    [Tooltip("�N���A���ɕK�����t���b�g�{�[�i�X")]
    public int clearFlatBonus = 1000;        //+1000�_
    [Tooltip("�p�[�^�C���i�����葬���قǉ��_�j")]
    public int timeParSeconds = 60;          //60�b
    [Tooltip("�p�[�^�C�����1�b�������Ƃɉ��Z����_")]
    public int timeBonusPerSecondUnderPar = 50; //1�b=+50�_
    [Tooltip("�^�C���{�[�i�X�̏���i�t���b�g�����܂܂Ȃ��j")]
    public int timeBonusMax = 3000;          //�ő�+3000�_

    [SerializeField] private int currentScore = 0;
    public int CurrentScore => currentScore ;

    public UnityEvent<int> onScoreChanged;

    //�����J�E���^
    public int TotalScore { get; private set; }
    public int TotalKills { get; private set; }
    public int DamageDealt { get; private set; }
    private float levelStartTime;

    //�_���[�W/���j�̕񍐌��iUIHealth ����Ăԁj
    public void ReportDamage(UIHitbox2D bulletHitbox, UIHealth target, int damage, bool killedNow)
    {
        if (bulletHitbox == null || target == null) return;

        // �v���C���[�˓G �̃_���[�W�����J�E���g
        if (bulletHitbox.faction == UIFaction.Player &&
            target.TryGetComponent<UIHitbox2D>(out var thb) &&
            thb.faction == UIFaction.Enemy)
        {
            int delta = 0;

            // �^�_��
            if (damage > 0)
            {
                DamageDealt += damage;
                delta += damage * Mathf.Max(0, pointsPerDamage);
            }

            // ���j�{�[�i�X
            if (killedNow)
            {
                TotalKills++;
                delta += Mathf.Max(0, pointsPerKill);
            }

            if (delta != 0)
            {
                TotalScore += delta;

                // �� HUD���Z�i���̂܂܈ێ�����OK�j
                if (hud) hud.AddScore(delta);

                // �� �Q�[���S�̂̍ŏI�X�R�A�p currentScore ���X�V����
                AddScore(delta);
                // ����� currentScore ���ꏏ�ɐL�т�
            }
        }
    }

    //�N���A���̉��_
    public int ComputeAndAddClearTimeBonus()
    {
        float elapsed = Time.time - levelStartTime;
        float under = Mathf.Max(0f, timeParSeconds - elapsed);
        int timeBonus = Mathf.RoundToInt(under * timeBonusPerSecondUnderPar);
        timeBonus = Mathf.Min(timeBonus, timeBonusMax);

        int totalBonus = clearFlatBonus + Mathf.Max(0, timeBonus);
        if (totalBonus > 0)
        {
            TotalScore += totalBonus;

            if (hud) hud.AddScore(totalBonus);

            // ���ŏI�X�R�A�ɂ����f
            AddScore(totalBonus);
        }
        return totalBonus;
    }


    // ���x���J�n���ɌĂ�
    public void StartNewLevel()
    {
        levelStartTime = Time.time;
        DamageDealt = 0;
        TotalKills = 0;
        //�X�R�A�͌p���Ȃ炻�̂܂܁A��؂�Ȃ� TotalScore=0 �ɂ���
    }

    public void ResetScore(){
        currentScore = 0;                 // ���ۂ̃X�R�A�ϐ����ɍ��킹�Ă�������
        onScoreChanged?.Invoke(currentScore);  // HUD�X�V�C�x���g������Ȃ甭��
    }

    public void AddScore(int add)
    {
        currentScore += add;
        onScoreChanged?.Invoke(currentScore);
    }

}
