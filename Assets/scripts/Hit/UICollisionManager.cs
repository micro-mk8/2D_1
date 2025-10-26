using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(1000)] // �ړ��̌�ɑ��点��
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
        // �����e vs �GHurtbox
        TestPairs(bulletsPlayer, hurtEnemy);
        // �G�e vs ����Hurtbox�i�����p�A���͓G�e���������Ȃ��̂܂܁j
        TestPairs(bulletsEnemy, hurtPlayer);
    }

    void TestPairs(List<UIHitbox2D> bullets, List<UIHitbox2D> hurts)
    {
        if (bullets.Count == 0 || hurts.Count == 0) return;

        // �X�i�b�v�V���b�g�i���[�v����Destroy����Ă����S�j
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
                    // �e��
                    var bulletHandler = b.GetComponent<IUIBullet>();
                    bulletHandler?.OnHit(h);

                    // ��e���i�����̃��C�t/�X�R�A�X�V�Ɂj
                    var hurtHandler = h.GetComponent<IUIHurtTarget>();
                    hurtHandler?.OnHitBy(b);

                    consumed = true;
                    break; // 1�q�b�g�Œe�͏���
                }
            }

            if (consumed) continue;
        }
    }
}
