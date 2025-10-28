using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(UIHitbox2D))]
public class UIHealth : MonoBehaviour, IUIHurtTarget
{
    [Header("HP")]
    [Min(1)] public int maxHP = 3;
    [SerializeField] private int currentHP = -1; // 初期化で maxHP に揃える
    public int CurrentHP => currentHP;            // 参照用（読み取り専用）

    [Header("死亡時の挙動")]
    [Tooltip("死亡した瞬間にこの GameObject を破棄する")]
    public bool destroyOnDeath = false;
    [Tooltip("死亡した瞬間に Hitbox を無効化（多重ヒット防止）")]
    public bool disableHitboxOnDeath = true;

    [Header("イベント（任意でUIや演出へ接続）")]
    public UnityEvent<int, int> onDamaged; // (currentHP, maxHP)
    public UnityEvent onDead;

    private UIHitbox2D myHitbox;
    private bool isDead;

    void Awake()
    {
        myHitbox = GetComponent<UIHitbox2D>();
    }

    void OnEnable()
    {
        // 初期化（再開時も復活）
        isDead = false;
        currentHP = Mathf.Clamp(currentHP < 0 ? maxHP : currentHP, 0, maxHP);
        if (myHitbox) myHitbox.enabled = true;


        //Hud通知を発火していないからHud側の表記がリセットされていない
        onDamaged?.Invoke(currentHP, maxHP);

    }

    // ==== 被弾コールバック（UICollisionManager から呼ばれる） ====
    public void OnHitBy(UIHitbox2D bulletHitbox)
    {
        if (isDead || bulletHitbox == null || myHitbox == null) return;

        // 陣営が同じなら無視（フレンドリーファイア無し）
        if (bulletHitbox.faction == myHitbox.faction) return;

        // ダメージ値（デフォルト1）
        int dmg = 1;
        var dmgComp = bulletHitbox.GetComponent<UIBulletDamage>();
        if (dmgComp) dmg = Mathf.Max(1, dmgComp.damage);

        // HP を減らす
        currentHP = Mathf.Max(0, currentHP - dmg);
        onDamaged?.Invoke(currentHP, maxHP);

        // ★追加：スコアへ報告（与ダメージと撃破）★
        ScoringManager.Instance?.ReportDamage(bulletHitbox, this, dmg, currentHP <= 0);


        // 死亡判定
        if (currentHP <= 0)
        {
            isDead = true;
            if (disableHitboxOnDeath && myHitbox) myHitbox.enabled = false;
            onDead?.Invoke();

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }
    }

    // 任意：外部から回復/リセットしたい時に
    public void ResetHP() {
        
    currentHP = maxHP; isDead = false; if (myHitbox) myHitbox.enabled = true; 
    
    onDamaged?.Invoke(currentHP, maxHP);

    }
}

