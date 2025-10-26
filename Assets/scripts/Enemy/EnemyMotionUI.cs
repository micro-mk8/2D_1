using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Canvas/UI( RectTransform ) �p�̃G�l�~�[�ړ��R���|�[�l���g�i�e�ⓖ���蔻��͖����j�B
/// ���Ắu�����v�����݂̂��Č����₷����\�p�^�[���� Inspector ����I�ׂ܂��B
/// �v���C�G���A�� UI ��� anchoredPosition ��p���Ĉړ����܂��B
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class EnemyMotionUI : MonoBehaviour
{
    public enum MovePattern
    {
        Straight,       // �������֒��i
        HorizontalSine, // X�ɐi�݂�Y���T�C���g�ŗh�炷�i���ړ��{�c�h��j
        VerticalSine,   // Y�ɐi�݂�X���T�C���g�ŗh�炷�i�c�ړ��{���h��j
        FigureEight,    // ���̏��8�̎��i���T�[�W���j
        Waypoints       // �����_�����Ɉړ��i���[�v�j
    }

    [Header("��{")]
    [SerializeField] private MovePattern pattern = MovePattern.HorizontalSine;
    [SerializeField] private float speedPxPerSec = 260f; // ��{�ړ����x�ipx/s�j
    [Tooltip("Straight �̐i�s�����BHorizontalSine/VerticalSine�ł͎厲�̐i�s�����Ɏg�p�B")]
    [SerializeField] private Vector2 direction = new Vector2(-1f, 0f); // ����͍���

    [Header("�T�C���g�iHorizontal/Vertical �p�j")]
    [SerializeField] private float amplitudePx = 120f;   // �h�ꕝ�i�s�[�N�j
    [SerializeField] private float frequencyHz = 0.9f;   // �h����g���i1�b������j
    [SerializeField] private float phaseOffset = 0f;     // �����ʑ��i���W�A���j

    [Header("FigureEight�i8�̎��j")]
    [SerializeField] private float eightWidthPx = 140f;   // ����
    [SerializeField] private float eightHeightPx = 90f;   // �c��
    [SerializeField] private float eightSpeedHz = 0.6f;   // ��鑬���i����/�b�j
    [SerializeField] private float eightPhase = 0f;       // �����ʑ�

    [Header("Waypoints�i���[�J�����W�BRelative �̏ꍇ�͏����ʒu����̑��΁j")]
    [SerializeField] private bool relativeWaypoints = true;
    [SerializeField] private List<Vector2> waypoints = new List<Vector2>(); // PlayAreaFrame ��̃��[�J���_
    [SerializeField] private bool loopWaypoints = true;
    [SerializeField] private float arriveEps = 2f; // ���B���肵�����l(px)
    [SerializeField] private float waitAtPointSec = 0f;

    private RectTransform rect;
    private Vector2 startPos;     // �J�n���� anchoredPosition
    private Vector2 sineOffset;   // �T�C�����̃I�t�Z�b�g�������K�p���邽�ߕێ�
    private float t;              // �o�ߎ���
    private int wpIndex;          // ���݂̃E�F�C�|�C���g
    private float waitRemain;     // �E�F�C�|�C���g�ł̑ҋ@�c��

    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        startPos = rect.anchoredPosition;
        sineOffset = Vector2.zero;
        t = 0f;
        wpIndex = 0;
        waitRemain = 0f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;

        switch (pattern)
        {
            case MovePattern.Straight:
                MoveStraight(dt);
                break;

            case MovePattern.HorizontalSine:
                MoveHorizontalSine(dt);
                break;

            case MovePattern.VerticalSine:
                MoveVerticalSine(dt);
                break;

            case MovePattern.FigureEight:
                MoveFigureEight();
                break;

            case MovePattern.Waypoints:
                MoveWaypoints(dt);
                break;
        }
    }

    // ===== �p�^�[������ =====

    private void MoveStraight(float dt)
    {
        Vector2 dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.left;
        rect.anchoredPosition += dir * speedPxPerSec * dt;
    }

    private void MoveHorizontalSine(float dt)
    {
        // �厲��X�Bdir�̕����ō��E�ǂ���֐i�ނ������߂�
        float vx = (direction.x >= 0f ? 1f : -1f) * speedPxPerSec;
        // �T�C����Y�ɂ�����i�����K�p�Ńh���t�g��h���j
        float omega = 2f * Mathf.PI * frequencyHz;
        float yNow = amplitudePx * Mathf.Sin(omega * t + phaseOffset);

        // ���O�̃I�t�Z�b�g�Ƃ̍�����������
        Vector2 baseMove = new Vector2(vx * dt, 0f);
        Vector2 newSine = new Vector2(0f, yNow);
        Vector2 delta = baseMove + (newSine - sineOffset);

        rect.anchoredPosition += delta;
        sineOffset = newSine;
    }

    private void MoveVerticalSine(float dt)
    {
        // �厲��Y�Bdir�̕����ŏ㉺�ǂ���֐i�ނ������߂�
        float vy = (direction.y >= 0f ? 1f : -1f) * speedPxPerSec;
        float omega = 2f * Mathf.PI * frequencyHz;
        float xNow = amplitudePx * Mathf.Sin(omega * t + phaseOffset);

        Vector2 baseMove = new Vector2(0f, vy * dt);
        Vector2 newSine = new Vector2(xNow, 0f);
        Vector2 delta = baseMove + (newSine - sineOffset);

        rect.anchoredPosition += delta;
        sineOffset = newSine;
    }

    private void MoveFigureEight()
    {
        // ���S�͊J�n�ʒu�B���T�[�W����8�̎�: x=A sin(w t + ��), y=B sin(2 w t + ��)
        float w = 2f * Mathf.PI * eightSpeedHz;
        float x = eightWidthPx * Mathf.Sin(w * t + eightPhase);
        float y = eightHeightPx * Mathf.Sin(2f * w * t + eightPhase);
        rect.anchoredPosition = startPos + new Vector2(x, y);
    }

    private void MoveWaypoints(float dt)
    {
        if (waypoints == null || waypoints.Count == 0) return;

        if (waitRemain > 0f)
        {
            waitRemain -= dt;
            return;
        }

        Vector2 target = waypoints[wpIndex];
        if (relativeWaypoints) target = startPos + target; // �J�n�ʒu����ɑ��Ύw��

        Vector2 pos = rect.anchoredPosition;
        Vector2 to = target - pos;
        float dist = to.magnitude;

        if (dist <= arriveEps)
        {
            // ���̓_��
            wpIndex++;
            if (wpIndex >= waypoints.Count)
            {
                if (loopWaypoints) wpIndex = 0;
                else { wpIndex = waypoints.Count - 1; return; }
            }
            waitRemain = waitAtPointSec;
            return;
        }

        Vector2 step = to.normalized * speedPxPerSec * dt;
        if (step.magnitude > dist) step = to; // �I�[�o�[�V���[�g�h�~
        rect.anchoredPosition = pos + step;
    }
}
