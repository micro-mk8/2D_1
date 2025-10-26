using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(1000)] // 移動の後に走らせる
public class UICollisionManager : MonoBehaviour
{
    static readonly List<UIHitbox2D> bulletsPlayer = new();
    static readonly List<UIHitbox2D> bulletsEnemy = new();
    static readonly List<UIHitbox2D> hurtPlayer = new();
    static readonly List<UIHitbox2D> hurtEnemy = new();

    public static void Register(UIHitbox2D hb)
    {
        if (hb == null) return;
        var list = GetList(hb);
        if (!list.Contains(hb)) list.Add(hb);
    }
    public static void Unregister(UIHitbox2D hb)
    {
        if (hb == null) return;
        GetList(hb).Remove(hb);
    }

    static List<UIHitbox2D> GetList(UIHitbox2D hb)
    {
        if (hb.role == UIRole.Bullet)
            return hb.faction == UIFaction.Player ? bulletsPlayer : bulletsEnemy;
        else
            return hb.faction == UIFaction.Player ? hurtPlayer : hurtEnemy;
    }

    void Update()
    {
        // 味方弾 vs 敵Hurtbox
        TestPairs(bulletsPlayer, hurtEnemy);
        // 敵弾 vs 味方Hurtbox（将来用、今は敵弾が未実装なら空のまま）
        TestPairs(bulletsEnemy, hurtPlayer);
    }

    void TestPairs(List<UIHitbox2D> bullets, List<UIHitbox2D> hurts)
    {
        if (bullets.Count == 0 || hurts.Count == 0) return;

        // スナップショット（ループ中にDestroyされても安全）
        var bArr = bullets.ToArray();
        var hArr = hurts.ToArray();

        foreach (var b in bArr)
        {
            if (b == null) continue;
            Rect br = b.GetWorldAABB();

            bool consumed = false;
            foreach (var h in hArr)
            {
                if (h == null) continue;
                Rect hr = h.GetWorldAABB();
                if (br.Overlaps(hr))
                {
                    // 弾側
                    var bulletHandler = b.GetComponent<IUIBullet>();
                    bulletHandler?.OnHit(h);

                    // 被弾側（将来のライフ/スコア更新に）
                    var hurtHandler = h.GetComponent<IUIHurtTarget>();
                    hurtHandler?.OnHitBy(b);

                    consumed = true;
                    break; // 1ヒットで弾は消費
                }
            }

            if (consumed) continue;
        }
    }
}
