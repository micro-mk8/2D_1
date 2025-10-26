using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIBulletOnHitRecycle : MonoBehaviour, IUIBullet
{
    public void OnHit(UIHitbox2D target)
    {
        var pb = GetComponent<Poolable>();
        if (pb && pb.TryRelease()) return; // プールへ返却
        if (this && gameObject) Destroy(gameObject); // プール外なら従来通り破棄
    }
}
