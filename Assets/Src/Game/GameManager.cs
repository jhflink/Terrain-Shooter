using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main entry point for the game logic
/// Class Index:
/// GameManager.cs: Main entry point tying all the game logic together
/// 
/// ObjectPool.cs:                  Generic ObjectPool class for IPoolableObjects.
/// BaseActor.cs                    A base class that every actor that moves around the terrain should inherit from.
/// Creature.cs                     Logic for the creature / enemy.
/// Bullet.cs                       Logic for shooting bullets, checking if it hits something, etc.
/// ParticleEmitter,cs:             A wrapper around the particle system to be pooled and copy particle settings to.
/// SpawnPoint.cs                   Lightweight logic for updating a spawner.
/// 
/// Player.cs:                      Player input / logic.
/// PlayerCamerea.cs:               Logic for handling the player camera.
/// PlayerCameraSettings.cs         Data container for camera settings that can be applied to the player camera.
/// PlayerDataContainer.cs:         Data container for some player stats, as well as the GUI code to display them.
/// 
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>
    /// Set up a simple singleton pattern
    /// </summary>
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // Difficult level
    public enum DifficultLevel
    {
        Easy = 1,
        Normal = 2,
        Hard = 3,
    }

    // Initial size of actor list
    private const int _actorsAllocationSize = 100;
    private const int _activeParticleSize = 20;

    // Container of all available perfabs
    public PrefabsContainer Prefabs = null;

    // Terrain manager
    public TerrainEditorMain TerrainManager = null;

    // Container for various player data
    public PlayerDataContainer PlayerData = null;

    // List of available settings/prefabs that can be used to copy settings from 
    public List<ParticleSystem> ParticleSystemPrefabTypes = new List<ParticleSystem>();

    // Pool of particle emitters
    private ObjectPool<ParticleEmitter> _particlePool = null;
    private const int _particlePoolPrewarmCount = 20;
    private const int _particlePoolExpandCount = 10;

    // Creature pool constants
    private const int _creaturePoolPrewarmCount = 50;
    private const int _creaturePoolExpandCount = 25;

    // Bullet pool constants
    private const int _bulletPoolPrewarmCount = 100;
    private const int _bulletPoolExpandCount = 50;

    // Current active particles
    private List<ParticleEmitter> _activeParticles = new List<ParticleEmitter>(_activeParticleSize);

    // Dictionary with spawn pools associated to a specific type
    public Dictionary<System.Type, ISpawnPool> SpawnPools = new Dictionary<System.Type, ISpawnPool>();

    // Time left variables
    private const int _maxSecondsLeft = 180;

    [HideInInspector]
    private float secondsLeft = _maxSecondsLeft;

    public float TimeLeft => secondsLeft;

    // paused variables
    private bool _paused = false;
    public bool IsPaused { private set { } get { return _paused; } }

    // timer for displaying a brief text when difficulty has changed
    private const float _difficultyChangeTimer = 2.0f;
    private float _difficultyChangeCounter = -1.0f;

    public bool HasDifficultyChange => _difficultyChangeCounter >= 0.0f;

    /// <summary>
    /// A list holding all active actors on the terrain
    /// </summary>
    private List<BaseActor> _activeActors = new List<BaseActor>(_actorsAllocationSize);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="actor"></param>
    /// <param name="shouldSpawn"></param>
    public void AddActiveActor(BaseActor actor, bool shouldSpawn)
    {
        _activeActors.Add(actor);

        if(shouldSpawn && !Vector3.positiveInfinity.Equals(actor.SpawnPosition))
        {
            actor.transform.position = actor.SpawnPosition;
        }

        actor.gameObject.SetActive(true);
    }

    /// <summary>
    /// Spawn a particle emitter at position
    /// </summary>
    /// <param name="position"></param>
    public void SpawnEmitter(string settingsID, Vector3 position)
    {
        ParticleEmitter emitter = _particlePool.GetObject();

        if (ParticleEmitter.PrefabSettings.ContainsKey(settingsID))
        {
            ParticleEmitter.CopyParticleSettings(properties:ParticleEmitter.PrefabSettings[settingsID].propertyList,
                                                 fromSystem:ParticleEmitter.PrefabSettings[settingsID].system,
                                                 toSystem:emitter.System);
        }

        emitter.gameObject.SetActive(true);
        emitter.Fire(position);

        _activeParticles.Add(emitter);
    }

    // Start is called before the first frame update
    void Start()
    {
        // Set up callbacks for spawn point inits
        int spawnAreaSize = TerrainManager.terrainSize.x / 4;
        int centerIndex = TerrainManager.terrainSize.x / 2;

        SpawnPoint.SpawnPointInitCallbacks.Add("TopLeftPosition",       (SpawnPoint) => { return GetAvailableSpawnPointWithinCellIndexRange(spawnAreaSize, TerrainManager.terrainSize.y - spawnAreaSize*2, spawnAreaSize); });
        SpawnPoint.SpawnPointInitCallbacks.Add("TopRightPosition",      (SpawnPoint) => { return GetAvailableSpawnPointWithinCellIndexRange(TerrainManager.terrainSize.x - spawnAreaSize*2, TerrainManager.terrainSize.y - spawnAreaSize*2, spawnAreaSize); });
        SpawnPoint.SpawnPointInitCallbacks.Add("BottomLeftPosition",    (SpawnPoint) => { return GetAvailableSpawnPointWithinCellIndexRange(spawnAreaSize, spawnAreaSize, spawnAreaSize); });
        SpawnPoint.SpawnPointInitCallbacks.Add("BottomRightPosition",   (SpawnPoint) => { return GetAvailableSpawnPointWithinCellIndexRange(TerrainManager.terrainSize.x - spawnAreaSize*2, spawnAreaSize, spawnAreaSize); });
        SpawnPoint.SpawnPointInitCallbacks.Add("CenterPosition",        (SpawnPoint) => { return GetAvailableSpawnPointWithinCellIndexRange(centerIndex - (spawnAreaSize/2),
                                                                                                                                            centerIndex - (spawnAreaSize/2),
                                                                                                                                            spawnAreaSize); });

        // Save properties from particle settings prefab
        foreach (ParticleSystem ps in ParticleSystemPrefabTypes)
            ParticleEmitter.PrefabSettings.Add(ps.name, (typeof(ParticleSystem).GetProperties(), ps));

        // Set up the particle emitter pool
        _particlePool = new ObjectPool<ParticleEmitter>(ParticleEmitter.InstantiateEmitter, _particlePoolPrewarmCount, _particlePoolExpandCount);

        // Set up Creature spawn pool
        SpawnPoint.SpawnCallbacks.Add("CreatureSpawn", Creature.SpawnPoolUpdate);
        Creature.CreaturePool = new ObjectPool<Creature>(Creature.InstantiateCreature, _creaturePoolPrewarmCount, _creaturePoolExpandCount);
        SpawnPools.Add(typeof(Creature), Creature.CreaturePool);

        // Set up player bullet pool
        PlayerData.Player.BulletPool = Creature.BulletPool = new ObjectPool<Bullet>(Bullet.InstantiateBullet, _bulletPoolPrewarmCount, _bulletPoolExpandCount);
         
        // Spawn player and add as actor
        TerrainInstanceCellDataContainer playerSpawnCell = TerrainManager.FetchCellFromIndexCallback((TerrainManager.terrainSize.x / 2, TerrainManager.terrainSize.y / 2));
        PlayerData.Player.SpawnPosition = TerrainInstanceCellDataContainer.CalculateCellWorldPosition(playerSpawnCell);
        AddActiveActor(PlayerData.Player, true);
    }

    /// <summary>
    /// Pause/unpause the game
    /// </summary>
    /// <param name="pause"></param>
    private void Pause(bool pause)
    {
        _paused = pause;

        foreach (BaseActor ba in _activeActors)
            ba.enabled = !pause;

        foreach (SpawnPoint sp in SpawnPoint.AllSpawnPoints)
            sp.enabled = !pause;
    }

    /// <summary>
    /// Restart the game
    /// </summary>
    private void Restart()
    {
        // reset player score and health
        PlayerData.Score = 0;
        PlayerData.Health = 100;

        // reset time
        secondsLeft = _maxSecondsLeft;

        // reset particles
        for (int i = _activeParticles.Count - 1; i > 0; i--)
        {
            _activeParticles[i].System.Stop();
            _activeParticles[i].gameObject.SetActive(false);
        }
        _activeParticles.Clear();

        // reset all actors
        for (int i = _activeActors.Count - 1; i > -1; i--)
        {
            _activeActors[i].gameObject.SetActive(false);
        }
        _activeActors.Clear();

        // clear all active creatures
        Creature.ActiveCreatures.Clear();

        // generate a new level for us
        TerrainManager.ReGenerateTerrain();

        // respawn player
        TerrainInstanceCellDataContainer playerSpawnCell = TerrainManager.FetchCellFromIndexCallback((TerrainManager.terrainSize.x / 2, TerrainManager.terrainSize.y / 2));
        PlayerData.Player.SpawnPosition = TerrainInstanceCellDataContainer.CalculateCellWorldPosition(playerSpawnCell);
        AddActiveActor(PlayerData.Player, true);

        // reanable spawn points
        foreach (SpawnPoint sp in SpawnPoint.AllSpawnPoints)
            sp.enabled = true;

        // unpause
        Pause(false);
    }

    // Update is called once per frame
    void Update()
    {
        // switch between game and terrain editor
        if (Input.GetKeyDown(KeyCode.F1))
        {
            TerrainManager.RenderGUI = !TerrainManager.RenderGUI;
            TerrainManager.TerrainEditorCamera.enabled = TerrainManager.RenderGUI;
            TerrainManager.HideTerrainFeedbackAndSelection(true);
            PlayerData.Camera.enabled = !TerrainManager.TerrainEditorCamera.enabled;
            Pause(TerrainManager.RenderGUI);
        }

        // Update all actors location data
        for (int i = _activeActors.Count - 1; i > -1; i--)
        {
            TerrainInstanceCellDataContainer newCell = TerrainManager.FetchCellFromWorldPositionCallback(_activeActors[i].transform.position);

            bool shouldRemoveActor = BaseActor.UpdateLocationAndApply(_activeActors[i], newCell);
            if (shouldRemoveActor)
                _activeActors.RemoveAt(i);
        }

        if (_paused)
        {
            if ((secondsLeft <= 0.0f || PlayerData.Health < 1) && Input.GetKeyDown(KeyCode.Return))
            {
                Restart();
            }

            return;
        }

        // reduce time left
        secondsLeft -= Time.deltaTime;

        // if time or health run out, pause the game
        if (secondsLeft <= 0.0f || PlayerData.Health < 1)
            Pause(true);

        if(_difficultyChangeCounter>=0.0f)
        {
            _difficultyChangeCounter += Time.deltaTime;
            if(_difficultyChangeCounter >= _difficultyChangeTimer)
            {
                _difficultyChangeCounter = -1.0f;
            }
        }

        // Update active particle emitters
        for (int i = _activeParticles.Count-1; i > 0; i--)
        {
            if (_activeParticles[i].IsDone())
            {
                _activeParticles[i].gameObject.SetActive(false);
                _activeParticles.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Get current difficult level
    /// </summary>
    /// <returns></returns>
    public DifficultLevel CurrentDifficultLevel()
    {
        if (PlayerData.Score < 10)
            return DifficultLevel.Easy;
        else if (PlayerData.Score < 25)
            return DifficultLevel.Normal;
        else
            return DifficultLevel.Hard;
    }

    /// <summary>
    /// Convert hits to points and add them to core
    /// </summary>
    /// <param name="hits"></param>
    /// <returns></returns>
    public int AddPointsFromHits(int hits)
    {
        DifficultLevel difficultyBeforeScore = CurrentDifficultLevel();

        // multiple the hits by itself to give extra combo points
        int points = hits * hits;

        Debug.Log("Add score " + points + " from " + hits + " hits");

        // set the player gui score
        PlayerData.Score += points;

        if(CurrentDifficultLevel()!=difficultyBeforeScore)
        {
            _difficultyChangeCounter = 0.0f;
        }

        return points;
    }

    /// <summary>
    /// Get a random cell position within a certain area 
    /// </summary>
    /// <param name="startX"></param>
    /// <param name="startY"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public Vector3 GetAvailableSpawnPointWithinCellIndexRange(int startX, int startY, int size)
    {
        // get a random cell withing the range
        TerrainInstanceCellDataContainer cell = TerrainManager.FetchCellFromIndexCallback((Random.Range(startX, startX + size), Random.Range(startY, startY + size)));

        // return world position of the cell
        return TerrainInstanceCellDataContainer.CalculateCellWorldPosition(cell);
    }
}
