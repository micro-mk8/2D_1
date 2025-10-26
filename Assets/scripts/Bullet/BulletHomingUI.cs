using UnityEngine;

/// <summary>
/// �ȈՃz�[�~���O�e�B�^�[�Q�b�g��UI���W�n�Œǔ��B���񓪑��x�ŏ��X�Ɍ��������킹��B
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class BulletHomingUI : MonoBehaviour
{
    [SerializeField] private RectTransform target;         // ��FEnemyRoot
    [SerializeField] private RectTransform space;          // BulletLayer�i�������[�J����ԂŌv�Z�j
    [SerializeField] private float speedPxPerSec = 700f;
    [SerializeField] private float turnDegPerSec = 360f;   // �񓪑��x�i�x/�b�j

    private RectTransform rect;
    private Vector2 dir = Vector2.up; // �������� ��

    public void Setup(RectTransform targetRT, RectTransform spaceRT)
    {
        target = targetRT;
        space = spaceRT;
    }

    void Awake() => rect = GetComponent<RectTransform>();

    void Update()
    {
        if (!space || !rect) return;

        // �ʒu�� space�iBulletLayer�j���W�n�Ŏ擾
        Vector2 pos = rect.anchoredPosition;
        if (target)
        {
            Vector2 targetLocal = WorldToLocal(target.position, space);
            Vector2 to = targetLocal - pos;
            if (to.sqrMagnitude > 0.001f)
            {
                Vector2 desired = to.normalized;
                // ����dir�� desired �Ɍ����� turnDegPerSec ��������
                float maxRad = Mathf.Deg2Rad * turnDegPerSec * Time.deltaTime;
                dir = Vector2RotateTowards(dir, desired, maxRad).normalized;
            }
        }

        rect.anchoredPosition = pos + dir * speedPxPerSec * Time.deltaTime;
    }

    private static Vector2 WorldToLocal(Vector3 worldPos, RectTransform space)
    {
        Vector3 local3 = space.InverseTransformPoint(worldPos);
        return new Vector2(local3.x, local3.y);
    }

    private static Vector2 Vector2RotateTowards(Vector2 current, Vector2 target, float maxRadiansDelta)
    {
        float cur = Mathf.Atan2(current.y, current.x);
        float tar = Mathf.Atan2(target.y, target.x);
        float delta = Mathf.DeltaAngle(cur * Mathf.Rad2Deg, tar * Mathf.Rad2Deg) * Mathf.Deg2Rad;
        float step = Mathf.Clamp(delta, -maxRadiansDelta, +maxRadiansDelta);
        float ang = cur + step;
        return new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
    }
}
