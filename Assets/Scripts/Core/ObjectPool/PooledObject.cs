using UnityEngine;

/// <summary>
/// 池化对象的引用脚本，使对象能够自动返回到对象池
/// </summary>
public class PooledObject : MonoBehaviour
{
    private Objectpooler _pool; // 所属对象池

    public void SetPool(Objectpooler pool)
    {
        _pool = pool;
    }

    /// <summary>
    /// 对象返回到对象池
    /// </summary>
    public void ReturnToPool()
    {
        if (_pool != null)
        {
            _pool.ReturnToPool(gameObject);
        }
        else
        {
            Debug.LogWarning("PooledObject: 没有设置对象池引用，无法返回！", gameObject);
        }
    }
}
