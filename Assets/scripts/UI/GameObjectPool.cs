using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;

public class GameObjectPool : MonoBehaviour
{
    [Header("プール対象")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private Transform defaultParent; // 例：BulletLayer
    [Header("サイズ")]
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
                // 生成直後に自分のプール参照を埋める
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

        // 事前生成
        for (int i = 0; i < prewarmCount; i++)
        {
            var obj = pool.Get();
            pool.Release(obj);
        }
    }

    public GameObject Get() => pool.Get();
    public void Release(GameObject go) { if (go) pool.Release(go); }

    // 外から Parent や Prefab を差し替えたい場合のSetter
    public void SetDefaultParent(Transform t) => defaultParent = t;
}
