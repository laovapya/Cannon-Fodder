using System.Collections.Generic;
using UnityEngine;

public static class ObjectPooler
{
    private static Dictionary<GameObject, Queue<GameObject>> pool = new();

    public static void CreatePool(GameObject prefab, int count)
    {
        if (!pool.ContainsKey(prefab))
            pool[prefab] = new Queue<GameObject>();

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Object.Instantiate(prefab, PrefabReference.instance.folderObjectPooling);
            obj.SetActive(false);
            pool[prefab].Enqueue(obj);
        }
    }
    public static GameObject ActivateObject(GameObject prefab)
    {
        return ActivateObject(prefab, Vector3.zero);
    }
    public static GameObject ActivateObject(GameObject prefab, Vector3 data)
    {
        if (!pool.ContainsKey(prefab))
            pool[prefab] = new Queue<GameObject>();

        GameObject obj;
        if (pool[prefab].Count > 0)
            obj = pool[prefab].Dequeue();
        else
            obj = Object.Instantiate(prefab, PrefabReference.instance.folderObjectPooling);

        obj.SetActive(true);
        if (obj.TryGetComponent<IPoolable>(out IPoolable iPoolable))
            iPoolable.OnActivateFromPool(prefab, data);
        return obj;
    }
    public static GameObject ActivateObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        return ActivateObject(prefab, position, rotation, Vector2.zero);
    }
    public static GameObject ActivateObject(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 data)
    {
        if (!pool.ContainsKey(prefab))
            pool[prefab] = new Queue<GameObject>();

        GameObject obj;
        if (pool[prefab].Count > 0)
            obj = pool[prefab].Dequeue();
        else
            obj = Object.Instantiate(prefab, PrefabReference.instance.folderObjectPooling);
        

        obj.transform.position = position;
        obj.transform.rotation = rotation;
    
        obj.SetActive(true);
        if (obj.TryGetComponent<IPoolable>(out IPoolable iPoolable))
            iPoolable.OnActivateFromPool(prefab, data);
        
        return obj;
    }
   
    public static void DeactivateObject(GameObject prefab, GameObject obj)
    {
        obj.SetActive(false);
        pool[prefab].Enqueue(obj);
    }

    public static void ClearPool(GameObject prefab)
    {
        if (pool.ContainsKey(prefab))
        {
            foreach (GameObject obj in pool[prefab])
                Object.Destroy(obj);
            pool[prefab].Clear();
        }
    }
    public static void ClearAll()
    {
        foreach (var kvp in pool)
        {
            foreach (GameObject obj in kvp.Value)
            {
                Object.Destroy(obj);
            }
        }
        pool.Clear();
    }
}

public interface IPoolable
{
    void OnActivateFromPool(GameObject dictKey, Vector3 data);
    //void OnDeactivateToPool();
}