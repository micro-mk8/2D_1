using UnityEngine;
using UnityEngine.Events;

public class StageClear : MonoBehaviour
{
    [SerializeField] private Transform enemyRoot;
    [SerializeField] private float pollInterval = 0.25f;
    [SerializeField] private GameFlowController flow; // �� �������������Ŋ���������Ȃ�OK

    public UnityEvent onCleared;

    private float nextCheck;

    void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + pollInterval;

        if (!enemyRoot) return;
        var healths = enemyRoot.GetComponentsInChildren<UIHealth>(true);
        if (healths.Length == 0) return;

        foreach (var h in healths)
        {
            if (h && h.CurrentHP > 0) return;
        }

        onCleared?.Invoke();
        // �܂��́Aflow?.HandleGameClear(); ���Ē��ڌĂԏꍇ������
        enabled = false;
    }
}
