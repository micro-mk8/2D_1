using UnityEngine;

/// <summary>
/// 敵の小弾：与えられた速度ベクトル( px/s )で直進するだけ。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class EnemyBulletMoverUI : MonoBehaviour
{
    [SerializeField] private Vector2 velocityPxPerSec = Vector2.zero;
    private RectTransform rect;

    void Awake() => rect = GetComponent<RectTransform>();

    public void SetVelocity(Vector2 v) => velocityPxPerSec = v;

    void Update()
    {
        rect.anchoredPosition += velocityPxPerSec * Time.deltaTime;
    }
}
