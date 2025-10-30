using UnityEngine;
using UnityEngine.Events;

public class StageClear : MonoBehaviour
{
    [SerializeField] private Transform enemyRoot;        // EnemyRoot (�G�������Ԃ牺���Ă���e)
    [SerializeField] private float pollInterval = 0.25f; // ���b�����Ƀ`�F�b�N���邩
    [SerializeField] private GameFlowController flow;    // GameFlowController (GameManager)

    public UnityEvent onCleared; // Inspector��HandleGameClear���Ă�ł邪�ی��Ŏc��

    private float nextCheck;

    void Update()
    {
        // pollInterval�b���Ƃɂ������肵�Ȃ�
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + pollInterval;

        // �G�̐e���w�肳��ĂȂ��Ȃ牽�����Ȃ�
        if (!enemyRoot) return;

        // EnemyRoot�z���̑SUIHealth(�G��HP�R���|�[�l���g)���擾
        var healths = enemyRoot.GetComponentsInChildren<UIHealth>(true);

        // ����������ԏd�v��
        // �u�N�����Ȃ� = �����G�͂��Ȃ� = �S�� = �N���A�v
        if (healths.Length == 0)
        {
            TriggerClear();
            return;
        }

        // �u��l�ł��܂�HP���c���Ă�Ȃ�܂��퓬���v
        foreach (var h in healths)
        {
            if (h && h.CurrentHP > 0)
            {
                return; // �܂��|�������ĂȂ�
            }
        }

        // �����܂ŗ��� = �G�͂܂�Hierarchy��ɂ��邯�ǑS��HP0����
        TriggerClear();
    }

    private void TriggerClear()
    {
        Debug.Log("[StageClear] all enemies dead. triggering clear.");

        // ����GameFlowController�ɃN���A�������Ă�
        if (flow != null)
        {
            flow.HandleGameClear();
        }
        else
        {
            Debug.LogWarning("[StageClear] flow is null, cannot call HandleGameClear");
        }

        // UnityEvent�o�R�ł��ꉞ�ĂԁiInspector�ł�onCleared�ݒ�����s����j
        onCleared?.Invoke();

        // ����ȏ㖈�t���[���Ă΂Ȃ��悤�Ɏ������~�߂�
        enabled = false;
    }
}
