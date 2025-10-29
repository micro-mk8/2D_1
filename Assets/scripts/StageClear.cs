using UnityEngine;
using UnityEngine.Events;

public class StageClear : MonoBehaviour
{
    [SerializeField] private Transform enemyRoot;   // Enemy をぶら下げているルート
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

        // 1体でも HP>0 がいれば未クリア
        foreach (var h in healths)
        {
            if (h && h.CurrentHP > 0) return;
        }
        // 全員 0 → クリア
        onCleared?.Invoke();
        enabled = false; // 1回きり
    }
}
