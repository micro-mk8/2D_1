using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;

public class GameObjectPool : MonoBehaviour
{
    [Header("�v�[���Ώ�")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private Transform defaultParent; // ��FBulletLayer
    [Header("�T�C�Y")]
    [SerializeField] private int prewarmCount = 100;
    [SerializeField] private int maxSize = 500;
    [SerializeField] private bool collectionChecks = false;

    private ObjectPool<GameObject> pool;

    void Awake()
    {
        pool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                var go = Instantiate(prefab, defaultParent);
                go.SetActive(false);
                // ��������Ɏ����̃v�[���Q�Ƃ𖄂߂�
                var pb = go.GetComponent<Poolable>();
                if (!pb) pb = go.AddComponent<Poolable>();
                pb.BindPool(this);
                return go;
            },
            actionOnGet: go => { if (defaultParent && go.transform.parent != defaultParent) go.transform.SetParent(defaultParent, false); go.SetActive(true); },
            actionOnRelease: go => { go.SetActive(false); },
            actionOnDestroy: go => { if (go) Destroy(go); },
            collectionCheck: collectionChecks,
            defaultCapacity: prewarmCount,
            maxSize: maxSize
        );

        // ���O����
        for (int i = 0; i < prewarmCount; i++)
        {
            var obj = pool.Get();
            pool.Release(obj);
        }
    }

    public GameObject Get() => pool.Get();
    public void Release(GameObject go) { if (go) pool.Release(go); }

    // �O���� Parent �� Prefab �������ւ������ꍇ��Setter
    public void SetDefaultParent(Transform t) => defaultParent = t;
}
