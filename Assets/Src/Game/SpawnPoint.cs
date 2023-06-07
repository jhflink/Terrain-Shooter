using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawn point class that connect a pool and spawning logic
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    // all spawn points
    public static List<SpawnPoint> AllSpawnPoints = new List<SpawnPoint>();

    // callbacks for init a spawn point position
    public static Dictionary<string, System.Func<SpawnPoint, Vector3>> SpawnPointInitCallbacks = new Dictionary<string, System.Func<SpawnPoint, Vector3>>();

    // callbacks for actual spawning of object
    public static Dictionary<string, System.Action<SpawnPoint, ISpawnPool>> SpawnCallbacks = new Dictionary<string, System.Action<SpawnPoint, ISpawnPool>>();

    // the connected spawn pool / object pool
    private ISpawnPool _spawnPool = null;

    // id of callback to be called on spawn
    public string SpawnCallbackID = "";

    // the type of object to be spawned (needs to alight with typeof(myType)
    public string SpawnTypeID = "";

    // the callback to init a position with
    public string InitCallback = "";

    private void Start()
    {
        AllSpawnPoints.Add(this);

        // try to get the actual type information for the type id
        // then get the corresponding spawn pool for the type
        System.Type spawnType = System.Type.GetType(SpawnTypeID);
        if (GameManager.Instance.SpawnPools.ContainsKey(spawnType))
            _spawnPool = GameManager.Instance.SpawnPools[spawnType];

        // error handling
        if (_spawnPool == null)
            Debug.LogError("Can't find spawn pool of type " + SpawnTypeID);

        // get the spawn point from callback
        if (SpawnPointInitCallbacks.ContainsKey(InitCallback))
            transform.position = SpawnPointInitCallbacks[InitCallback](this);
    }

    private void Update()
    {
        // call the callback for spawn point logic
        if (_spawnPool !=null && SpawnCallbacks.ContainsKey(SpawnCallbackID))
            SpawnCallbacks[SpawnCallbackID](this, _spawnPool);
    }
}
