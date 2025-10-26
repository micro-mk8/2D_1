using UnityEngine;

/// <summary>
/// UI(RectTransform)�e�̋��ʃx�[�X�F�����Ƌ��E�O�Ŏ������ŁB�����ڂ�Image��OK�B
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class AllyBulletBaseUI : MonoBehaviour
{
    [Header("����(�b)")]
    [SerializeField] private float lifeTime = 4f;

    [Header("���E(�C��) - PlayAreaFrame �������Ă�")]
    [SerializeField] private RectTransform container;

    private RectTransform rect;
    private float timer;

    private static readonly Vector3[] C = new Vector3[4];
    private static readonly Vector3[] T = new Vector3[4];

    public void Init(RectTransform containerRT, float life = -1f)
    {
        container = containerRT;
        if (life > 0f) lifeTime = life;
        timer = 0f;
    }

    void Awake() => rect = GetComponent<RectTransform>();

    void Update()
    {
        timer += Time.deltaTime;
        if (lifeTime > 0f && timer >= lifeTime)
        {
            RecycleOrDestroy(); return;
        }

        if (!container) return;

        // �l���ŋ��E�O�`�F�b�N
        container.GetWorldCorners(C);
        rect.GetWorldCorners(T);
        float cMinX = Mathf.Min(C[0].x, C[1].x, C[2].x, C[3].x);
        float cMaxX = Mathf.Max(C[0].x, C[1].x, C[2].x, C[3].x);
        float cMinY = Mathf.Min(C[0].y, C[1].y, C[2].y, C[3].y);
        float cMaxY = Mathf.Max(C[0].y, C[1].y, C[2].y, C[3].y);

        float tMinX = Mathf.Min(T[0].x, T[1].x, T[2].x, T[3].x);
        float tMaxX = Mathf.Max(T[0].x, T[1].x, T[2].x, T[3].x);
        float tMinY = Mathf.Min(T[0].y, T[1].y, T[2].y, T[3].y);
        float tMaxY = Mathf.Max(T[0].y, T[1].y, T[2].y, T[3].y);

        bool outside = (tMaxX < cMinX) || (tMinX > cMaxX) || (tMaxY < cMinY) || (tMinY > cMaxY);
        if (outside)
        {

            if (outside) { RecycleOrDestroy(); }
        }
    }

    private void RecycleOrDestroy()
    {
        var pb = GetComponent<Poolable>();

        if (pb && pb.TryRelease()) return;
        Destroy(gameObject);
    }

}

