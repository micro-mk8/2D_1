using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIBulletOnHitRecycle : MonoBehaviour, IUIBullet
{
    public void OnHit(UIHitbox2D target)
    {
        var pb = GetComponent<Poolable>();
        if (pb && pb.TryRelease()) return; // �v�[���֕ԋp
        if (this && gameObject) Destroy(gameObject); // �v�[���O�Ȃ�]���ʂ�j��
    }
}
