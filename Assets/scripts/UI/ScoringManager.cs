using UnityEngine;
using UnityEngine.Events;

public class ScoringManager : MonoBehaviour
{
    // -------- シングルトン（どこからでも呼べる） --------
    public static ScoringManager Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        levelStartTime = Time.time;
    }

    [Header("表示先（右パネル）")]
    [SerializeField] private HUDPresenter hud; // HUDPresenter

    [Header("加点倍率（あなたが調整できます）")]
    [Tooltip("与えたダメージ1につき何点か")]
    public int pointsPerDamage = 5;          //1ダメージ=5点
    [Tooltip("敵を1体撃破したときのボーナス")]
    public int pointsPerKill = 200;        //撃破=200点

    [Header("クリアタイムの換算（次ステップで使用）")]
    [Tooltip("クリア時に必ず乗るフラットボーナス")]
    public int clearFlatBonus = 1000;        //+1000点
    [Tooltip("パータイム（これより速いほど加点）")]
    public int timeParSeconds = 60;          //60秒
    [Tooltip("パータイムより1秒速いごとに加算する点")]
    public int timeBonusPerSecondUnderPar = 50; //1秒=+50点
    [Tooltip("タイムボーナスの上限（フラット分を含まない）")]
    public int timeBonusMax = 3000;          //最大+3000点

    [SerializeField] private int currentScore = 0;
    public int CurrentScore => currentScore ;

    public UnityEvent<int> onScoreChanged;

    //内部カウンタ
    public int TotalScore { get; private set; }
    public int TotalKills { get; private set; }
    public int DamageDealt { get; private set; }
    private float levelStartTime;

    //ダメージ/撃破の報告口（UIHealth から呼ぶ）
    public void ReportDamage(UIHitbox2D bulletHitbox, UIHealth target, int damage, bool killedNow)
    {
        if (bulletHitbox == null || target == null) return;

        // プレイヤー⇒敵 のダメージだけカウント
        if (bulletHitbox.faction == UIFaction.Player &&
            target.TryGetComponent<UIHitbox2D>(out var thb) &&
            thb.faction == UIFaction.Enemy)
        {
            int delta = 0;

            // 与ダメ
            if (damage > 0)
            {
                DamageDealt += damage;
                delta += damage * Mathf.Max(0, pointsPerDamage);
            }

            // 撃破ボーナス
            if (killedNow)
            {
                TotalKills++;
                delta += Mathf.Max(0, pointsPerKill);
            }

            if (delta != 0)
            {
                TotalScore += delta;

                // ★ HUD加算（今のまま維持してOK）
                if (hud) hud.AddScore(delta);

                // ★ ゲーム全体の最終スコア用 currentScore も更新する
                AddScore(delta);
                // これで currentScore も一緒に伸びる
            }
        }
    }

    //クリア時の加点
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

            // ★最終スコアにも反映
            AddScore(totalBonus);
        }
        return totalBonus;
    }


    // レベル開始時に呼ぶ
    public void StartNewLevel()
    {
        levelStartTime = Time.time;
        DamageDealt = 0;
        TotalKills = 0;
        //スコアは継続ならそのまま、区切るなら TotalScore=0 にする
    }

    public void ResetScore(){
        currentScore = 0;                 // 実際のスコア変数名に合わせてください
        onScoreChanged?.Invoke(currentScore);  // HUD更新イベントがあるなら発火
    }

    public void AddScore(int add)
    {
        currentScore += add;
        onScoreChanged?.Invoke(currentScore);
    }

}
