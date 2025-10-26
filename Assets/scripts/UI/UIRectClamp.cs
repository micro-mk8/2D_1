using UnityEngine;

/// <summary>
/// �Ώ�(RectTransform)�́u�l��(���[���h)�v�� container �̓����ɓ���悤��
/// ���t���[���ʒu��␳���܂��B�A���J�[/�s�{�b�g�ݒ�Ɉˑ����܂���B
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIRectClamp : MonoBehaviour
{
    [SerializeField] private RectTransform container;   // ��: PlayAreaFrame
    [SerializeField] private Vector2 padding = new Vector2(8f, 8f);

    private RectTransform rect;

    // �g���񂵃o�b�t�@�iGC�팸�j
    static readonly Vector3[] C = new Vector3[4];
    static readonly Vector3[] T = new Vector3[4];

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        if (!container) Debug.LogWarning("[UIRectClamp] Container���ݒ�ł��B");
    }

    void LateUpdate()
    {
        if (!container) return;

        // �l��(���[���h)���擾
        container.GetWorldCorners(C);
        rect.GetWorldCorners(T);

        // container �̓������E�i�p�f�B���O�K�p�j
        float cMinX = Mathf.Min(C[0].x, C[1].x, C[2].x, C[3].x) + padding.x;
        float cMaxX = Mathf.Max(C[0].x, C[1].x, C[2].x, C[3].x) - padding.x;
        float cMinY = Mathf.Min(C[0].y, C[1].y, C[2].y, C[3].y) + padding.y;
        float cMaxY = Mathf.Max(C[0].y, C[1].y, C[2].y, C[3].y) - padding.y;

        // �Ώۂ̌��݋��E
        float tMinX = Mathf.Min(T[0].x, T[1].x, T[2].x, T[3].x);
        float tMaxX = Mathf.Max(T[0].x, T[1].x, T[2].x, T[3].x);
        float tMinY = Mathf.Min(T[0].y, T[1].y, T[2].y, T[3].y);
        float tMaxY = Mathf.Max(T[0].y, T[1].y, T[2].y, T[3].y);

        // �K�v�ȕ␳�ʁi���[���h�j���v�Z
        float dx = 0f, dy = 0f;

        // ��
        float contW = cMaxX - cMinX;
        float targW = tMaxX - tMinX;
        if (targW <= contW)
        {
            if (tMinX < cMinX) dx = cMinX - tMinX;
            else if (tMaxX > cMaxX) dx = cMaxX - tMaxX;
        }
        else
        {
            // �Ώۂ� container ��蕝�L���ꍇ�͒��S�����킹��
            float cMidX = 0.5f * (cMinX + cMaxX);
            float tMidX = 0.5f * (tMinX + tMaxX);
            dx = cMidX - tMidX;
        }

        // �c
        float contH = cMaxY - cMinY;
        float targH = tMaxY - tMinY;
        if (targH <= contH)
        {
            if (tMinY < cMinY) dy = cMinY - tMinY;         // ���ɂ͂ݏo���������グ
            else if (tMaxY > cMaxY) dy = cMaxY - tMaxY;    // ��ɂ͂ݏo������������
        }
        else
        {
            // �Ώۂ� container ���w�������ꍇ�͒��S�����킹��
            float cMidY = 0.5f * (cMinY + cMaxY);
            float tMidY = 0.5f * (tMinY + tMaxY);
            dy = cMidY - tMidY;
        }

        if (dx != 0f || dy != 0f)
        {
            // ���[���h��ԂŔ������i�A���J�[/�s�{�b�g�Ɉˑ����Ȃ��j
            rect.position += new Vector3(dx, dy, 0f);
        }
    }
}
