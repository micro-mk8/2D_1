using UnityEngine;
using UnityEngine.Events;

public class StageClear : MonoBehaviour
{
    [SerializeField] private Transform enemyRoot;   // Enemy ���Ԃ牺���Ă��郋�[�g
    [SerializeField] private float pollInterval = 0.25f;
    public UnityEvent onCleared;

    private float nextCheck;

    void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + pollInterval;

        if (!enemyRoot) return;
        var healths = enemyRoot.GetComponentsInChildren<UIHealth>(true);
        if (healths.Length == 0) return;

        // 1�̂ł� HP>0 ������Ζ��N���A
        foreach (var h in healths)
        {
            if (h && h.CurrentHP > 0) return;
        }
        // �S�� 0 �� �N���A
        onCleared?.Invoke();
        enabled = false; // 1�񂫂�
    }
}
