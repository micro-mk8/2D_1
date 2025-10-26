using UnityEngine;

/// <summary>
/// �G�̏��e�F�^����ꂽ���x�x�N�g��( px/s )�Œ��i���邾���B
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
