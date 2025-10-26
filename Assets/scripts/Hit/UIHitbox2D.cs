
using UnityEngine;

public enum UIFaction { Player, Enemy }
public enum UIRole { Bullet, Hurtbox }

[RequireComponent(typeof(RectTransform))]
public class UIHitbox2D : MonoBehaviour
{
    [Header("�����Ɛw�c")]
    public UIRole role = UIRole.Bullet;
    public UIFaction faction = UIFaction.Player;

    [Header("�]���i+�ōL����^-�ŋ��߂�j")]
    public Vector2 padding = Vector2.zero; // px

    private RectTransform rect;
    private static readonly Vector3[] C = new Vector3[4];

    void Awake() { rect = GetComponent<RectTransform>(); }
    void OnEnable() { UICollisionManager.Register(this); }
    void OnDisable() { UICollisionManager.Unregister(this); }

    public Rect GetWorldAABB()
    {
        rect.GetWorldCorners(C);
        float minX = Mathf.Min(C[0].x, C[1].x, C[2].x, C[3].x) - padding.x;
        float maxX = Mathf.Max(C[0].x, C[1].x, C[2].x, C[3].x) + padding.x;
        float minY = Mathf.Min(C[0].y, C[1].y, C[2].y, C[3].y) - padding.y;
        float maxY = Mathf.Max(C[0].y, C[1].y, C[2].y, C[3].y) + padding.y;

        // �� �������C���|�C���g�iRectTransform.MinMaxRect �ł͂Ȃ� Rect.MinMaxRect�j
        return new Rect(minX, minY, maxX - minX, maxY - minY);
        // ���邢�́Freturn new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    public RectTransform Rect => rect;
}
