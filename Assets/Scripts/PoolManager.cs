using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    //单例脚本
    public static PoolManager Instance { get; private set; }
    //通过传入的类型找到对应的池子
    private Dictionary<Object, Queue<Object>> poolDic = new Dictionary<Object, Queue<Object>>();

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 初始化某个类型的对象池
    /// </summary>
    /// <param name="prefab">指定预制体的类型</param>
    /// <param name="size">当前类型对象池的大小</param>
    public void InitPool(Object prefab,int size)
    {
        if (poolDic.ContainsKey(prefab))
        {
            return;
        }
        Queue<Object> queue = new Queue<Object>();
        for (int i = 0; i < size; i++)
        {
            Object go = Instantiate(prefab);
            CreateGameObjectAndSetActive(go,false);
            queue.Enqueue(go);
        }
        poolDic[prefab] = queue;
    }

    /// <summary>
    /// 创建游戏物体并设置激活状态
    /// </summary>
    private void CreateGameObjectAndSetActive(Object obj,bool active)
    {
        GameObject itemGo;
        if (obj is Component) // 如果是组件
        {
            itemGo = (obj as Component).gameObject;
        }
        else // 如果传进来的就是一个游戏物体
        {
            itemGo = obj as GameObject;
        }
        itemGo.transform.SetParent(transform);
        itemGo.SetActive(active);
    }

    public T GetInstance<T>(Object prefab) where T : Object
    {
        Queue<Object> queue;
        if (poolDic.TryGetValue(prefab,out queue))
        {
            Object obj;
            if (queue.Count > 0)
            {
                obj = queue.Dequeue();
            }
            else
            {
                obj = Instantiate(prefab);
            }
            CreateGameObjectAndSetActive(obj, true);
            queue.Enqueue(obj);
            return obj as T;
        }

        Debug.LogError("还没有当前类型的资源池被实例化");
        return null;
    }
}