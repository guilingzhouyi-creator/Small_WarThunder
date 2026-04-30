using System.Collections.Generic;
using UnityEngine;

//对象池
public class Objectpooler : MonoBehaviour
{
    [SerializeField] private GameObject _prefab; // 需要池化的预制体
    [SerializeField] private int _poolSize = 5; // 池中对象的数量
    // [SerializeField] private bool _allowExpansion = true; // 是否允许池扩展

    private List<GameObject> _pool; // 对象池列表

    private void Start()
    {
        _pool = new List<GameObject>();
        for (int i = 0; i < _poolSize; i++)
        {
            CreateNewObject();
        }
    }

    // public void InitializePool()
    // {
    //     if (_pool != null)
    //     {
    //         return; // 已经初始化过了
    //     }

    //     _pool = new List<GameObject>();
    //     for (int i = 0; i < _poolSize; i++)
    //     {
    //         CreateNewObject();
    //     }
    // }

    private GameObject CreateNewObject()
    {
        GameObject obj = Instantiate(_prefab, this.transform); // 将新对象作为 Objectpooler 的子对象，方便管理

        // 池化对象必须在预制体上就带有 PooledObject；运行时补件会晚于 Awake，无法保证炮弹初始化顺序。
        PooledObject pooledObject = obj.GetComponent<PooledObject>();
        if (pooledObject == null)
        {
            Debug.LogError($"对象池创建的实例 {obj.name} 缺少 PooledObject，说明炮弹预制体配置不完整。请在预制体上直接添加 PooledObject 组件。", obj);
            Destroy(obj);
            return null;
        }
        pooledObject.SetPool(this);

        obj.SetActive(false); // 初始状态为不激活

        _pool.Add(obj);
        return obj;
    }

    public GameObject GetPooledObject()
    {
        foreach (GameObject obj in _pool)
        {
            if (!obj.activeInHierarchy)
            {
                return obj; // 返回第一个未激活的对象
            }
        }


        //改进为通用的 TryGet 模式，支持按弹种获取对应的对象池
        // for (int i = 0; i < _pool.Count; i++)
        // {
        //     if (!_pool[i].activeInHierarchy)
        //     {
        //         return _pool[i];
        //     }
        // }

        // if (_allowExpansion)
        // {
        //     return CreateNewObject();
        // }

        return CreateNewObject(); // 没有可用对象
    }

    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false); // 将对象设置为不激活，返回池中
    }


}