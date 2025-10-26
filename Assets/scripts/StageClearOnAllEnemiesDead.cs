using UnityEngine;

/// <summary>
/// �w�肵���G(UIHealth)���S���|�ꂽ��u�N���A�v�����ɂ��āA
/// ScoringManager �̃N���A�^�C���{�[�i�X�����Z����B
/// </summary>
public class StageClearOnAllEnemiesDead : MonoBehaviour
{
    [SerializeField] private UIHealth[] enemies;
    private int remaining;
    private bool awarded;

    void OnEnable()
    {
        remaining = 0;
        if (enemies == null) return;

        foreach (var e in enemies)
        {
            if (e == null) continue;
            remaining++;
            e.onDead.AddListener(OnEnemyDead);
        }
    }

    void OnDisable()
    {
        if (enemies == null) return;
        foreach (var e in enemies)
        {
            if (e == null) continue;
            e.onDead.RemoveListener(OnEnemyDead);
        }
    }

    private void OnEnemyDead()
    {
        if (awarded) return;
        remaining = Mathf.Max(0, remaining - 1);
        if (remaining == 0)
        {
            awarded = true;
            int bonus = ScoringManager.Instance?.ComputeAndAddClearTimeBonus() ?? 0;
            Debug.Log($"Stage Clear! Time bonus: +{bonus}");
        }
    }
}
