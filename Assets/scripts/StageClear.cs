using UnityEngine;
using UnityEngine.Events;

public class StageClear : MonoBehaviour
{
    [SerializeField] private Transform enemyRoot;        // EnemyRoot (敵たちをぶら下げている親)
    [SerializeField] private float pollInterval = 0.25f; // 何秒おきにチェックするか
    [SerializeField] private GameFlowController flow;    // GameFlowController (GameManager)

    public UnityEvent onCleared; // InspectorでHandleGameClearも呼んでるが保険で残す

    private float nextCheck;

    void Update()
    {
        // pollInterval秒ごとにしか判定しない
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + pollInterval;

        // 敵の親が指定されてないなら何もしない
        if (!enemyRoot) return;

        // EnemyRoot配下の全UIHealth(敵のHPコンポーネント)を取得
        var healths = enemyRoot.GetComponentsInChildren<UIHealth>(true);

        // ★ここが一番重要★
        // 「誰もいない = もう敵はいない = 全滅 = クリア」
        if (healths.Length == 0)
        {
            TriggerClear();
            return;
        }

        // 「一人でもまだHPが残ってるならまだ戦闘中」
        foreach (var h in healths)
        {
            if (h && h.CurrentHP > 0)
            {
                return; // まだ倒しきってない
            }
        }

        // ここまで来た = 敵はまだHierarchy上にいるけど全員HP0扱い
        TriggerClear();
    }

    private void TriggerClear()
    {
        Debug.Log("[StageClear] all enemies dead. triggering clear.");

        // 直接GameFlowControllerにクリア処理を呼ぶ
        if (flow != null)
        {
            flow.HandleGameClear();
        }
        else
        {
            Debug.LogWarning("[StageClear] flow is null, cannot call HandleGameClear");
        }

        // UnityEvent経由でも一応呼ぶ（InspectorでのonCleared設定も実行する）
        onCleared?.Invoke();

        // これ以上毎フレーム呼ばないように自分を止める
        enabled = false;
    }
}
