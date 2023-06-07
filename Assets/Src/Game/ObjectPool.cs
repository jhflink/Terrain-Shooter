using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using System.Linq;

/// <summary>
/// 
/// </summary>
public interface ISpawnPool
{
    /// <summary>
    /// Can be called without knowing the type of object pool to spawn a object on the terrain
    /// </summary>
    /// <param name="position"></param>
    public void Spawn(Vector3 position);
}

/// <summary>
/// Interface for all objects that can be used by the ObjectPool
/// </summary>
public interface IPoolableObject
{
    /// <summary>
    /// Check if object is available to be distributed by the pool
    /// </summary>
    /// <returns></returns>
    public bool IsAvailable();

    /// <summary>
    /// Callback that will reset the state of the object between being distributed
    /// </summary>
    public void OnResetCallback();

    /// <summary>
    /// Callback that will be called when a SpawnPoint is allocating new objects
    /// </summary>
    public void OnSpawnCallback(Vector3 position);
}

/// <summary>
/// Generic object pool constraining type T to implement IPoolableObject
/// </summary>
/// <typeparam name="T"></typeparam>
public class ObjectPool<T> : ISpawnPool where T : IPoolableObject
{
    // List storing the actual pooled objects
    private readonly List<IPoolableObject> _currentPool;

    // function callback to create the object
    private readonly System.Func<IPoolableObject> _createObjectMethod;

    // how much to extend the pool count with when needed
    private readonly int _extendPoolCount;

    public ObjectPool(System.Func<IPoolableObject> createObjectMethod,
                      int prewarmCount,
                      int extendPoolCount)
    {
        _createObjectMethod = createObjectMethod;
        _extendPoolCount = extendPoolCount;

        _currentPool = new List<IPoolableObject>(prewarmCount);

        InstantiateObjectsToPool(prewarmCount);
    }

    /// <summary>
    /// Instantiate new objects to the pool
    /// </summary>
    /// <param name="count"></param>
    private void InstantiateObjectsToPool(int count)
    {
        for (int i = 0; i < count; i++) {
            IPoolableObject newObject = _createObjectMethod();
            _currentPool.Add(newObject);
        }
    }

    /// <summary>
    /// Extend the pool capacity by count
    /// </summary>
    /// <param name="count"></param>
    private IPoolableObject ExtendPool(int count)
    {
        // incerase capacity by the count
        _currentPool.Capacity += count;

        // instantiate the new objects
        InstantiateObjectsToPool(count);

        // return the first new instantiated object
        return _currentPool[_currentPool.Count-1];
    }

    /// <summary>
    /// Get an available object from the pool
    /// </summary>
    /// <returns></returns>
    public T GetObject()
    {
        // find the first available object
        int index = _currentPool.FindIndex(pooledObject => pooledObject.IsAvailable());
        IPoolableObject availableObject = index >= 0 ? _currentPool[index] : null;

        // if we cant extend the pool anymore return default T
        if (availableObject == null && _extendPoolCount < 1)
            return default(T);
        // if we can't find a available object, extend the pool
        else if (availableObject == null)
            availableObject =  ExtendPool(_extendPoolCount);

        // enable new object
        availableObject.OnResetCallback();

        // cast the interface to the T type
        // note: we can do this without type checking
        // because we know that T inherits from the IPoolableObject interface due to the generics constrain
        return (T)availableObject;
    }

    /// <summary>
    /// Spawn a object at position
    /// </summary>
    /// <param name="position"></param>
    public void Spawn(Vector3 position)
    {
        IPoolableObject spawnedObject = GetObject();

        spawnedObject.OnSpawnCallback(position);
    }
}
