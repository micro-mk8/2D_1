using UnityEngine;

public class Poolable : MonoBehaviour
{
    private GameObjectPool pool;
    public void BindPool(GameObjectPool p) => pool = p;

    public bool TryRelease()
    {
        if (pool == null) return false;
        pool.Release(gameObject);
        return true;
    }
}
