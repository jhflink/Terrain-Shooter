using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Creature : BaseActor, IPoolableObject
{
    // pool of bullets
    public static ObjectPool<Bullet> BulletPool = null;

    // list of all active creatures
    public static List<Creature> ActiveCreatures = new List<Creature>();

    #region PoolableRegion

    // obeject pool for creatures
    public static ObjectPool<Creature> CreaturePool = null;

    /// <summary>
    /// Instantiate creature for pool callback
    /// </summary>
    /// <returns></returns>
    public static IPoolableObject InstantiateCreature()
    {
        return Instantiate<Creature>(GameManager.Instance.Prefabs.Creature);
    }

    /// <summary>
    /// Is available for pooling
    /// </summary>
    /// <returns></returns>
    public bool IsAvailable() => !gameObject.activeSelf;

    /// <summary>
    /// Called when being reused by pool
    /// </summary>
    public void OnResetCallback()
    {
        _activeState = ActiveState.Normal;
        SetColor(Color.white);
    }

    /// <summary>
    /// Called when being spawned by spawn point
    /// </summary>
    /// <param name="position"></param>
    public void OnSpawnCallback(Vector3 position)
    {
        GameManager.Instance.AddActiveActor(this, true);
        ActiveCreatures.Add(this);

        // get closest cell from position
        TerrainInstanceCellDataContainer startCell = GameManager.Instance.TerrainManager.FetchCellFromWorldPositionCallback(position);

        // spawn on cell
        Spawn(startCell);

        // spawn particle effect 
        GameManager.Instance.SpawnEmitter("SpawnCreature", position);

        enabled = true;
    }

    /// <summary>
    /// Logic for spawning creatures
    /// </summary>
    /// <param name="spawnPoint"></param>
    /// <param name="spawnPool"></param>
    public static void SpawnPoolUpdate(SpawnPoint spawnPoint, ISpawnPool spawnPool)
    {
        // easy mode
        if (GameManager.Instance.CurrentDifficultLevel() == GameManager.DifficultLevel.Easy)
        {
            if (ActiveCreatures.Count < 20 + GameManager.Instance.PlayerData.Score)
            {
                spawnPool.Spawn(SpawnPoint.SpawnPointInitCallbacks[spawnPoint.InitCallback](spawnPoint));
            }
        }
        // normal mode
        else if (GameManager.Instance.CurrentDifficultLevel() == GameManager.DifficultLevel.Normal)
        {
            if (ActiveCreatures.Count < 35 + GameManager.Instance.PlayerData.Score)
            {
                spawnPool.Spawn(SpawnPoint.SpawnPointInitCallbacks[spawnPoint.InitCallback](spawnPoint));
            }
        }
        // hard mode
        else if (GameManager.Instance.CurrentDifficultLevel() == GameManager.DifficultLevel.Hard)
        {
            if (ActiveCreatures.Count < 50 + GameManager.Instance.PlayerData.Score)
            {
                spawnPool.Spawn(SpawnPoint.SpawnPointInitCallbacks[spawnPoint.InitCallback](spawnPoint));
            }
        }
    }

    #endregion

    /// <summary>
    /// Spawn one creature on every cell in list
    /// </summary>
    /// <param name="SpawnCells"></param>
    public static void SpawnNext(List<TerrainInstanceCellDataContainer> SpawnCells, bool pause=false) {
        for (int i = 0; i < SpawnCells.Count; i++) {
            SpawnNext(SpawnCells[i], pause);
        }
    }

    /// <summary>
    /// Spawn creature on cell
    /// </summary>
    /// <param name="SpawnCell"></param>
    public static void SpawnNext(TerrainInstanceCellDataContainer SpawnCell, bool pause=false)
    {
        Creature newCreature = CreaturePool.GetObject();
        newCreature.Spawn(SpawnCell);
        GameManager.Instance.AddActiveActor(newCreature,true);
        ActiveCreatures.Add(newCreature);
        newCreature.enabled = !pause;
    }

    // logical state of create
    public enum ActiveState
    {
        Normal,     // normal behaviour
        InAir,      // when being flipped into the air
        Angry,      // angry behaviour attacking the player more aggressivley 
        CanBeHit,   // when flipped over and can be hurt
    }

    // current state
    private ActiveState _activeState = ActiveState.Normal;

    // collider
    public CapsuleCollider collider = null;

    // target cell that we're walking towards
    private TerrainInstanceCellDataContainer _targetCell = null;

    // array of neighbour cells
    private (int, int)[] _neighbourTiles = new (int, int)[8];

    // base moving speed
    private float _speed = 0.0f;

    // speed multiplier
    private float _speedMulti = 1.0f;

    // spline position when moving in the air
    private Vector3[] _airSplinePosition = new Vector3[3];

    // counter for in air movements
    private float _inAirCounter = 0.0f;

    // general counter for going in/out of different states
    private float _stateTimeCounter = 0.0f;

    // material property block
    private MaterialPropertyBlock _materialPropertyBlock = null;

    // renderer component
    private Renderer _renderer = null;

    // material color
    private Color _color = Color.white;

    // speed constants
    private const float _minSpeed = 0.3f;
    private const float _maxSpeed = 0.7f;

    // point constant
    private const int _killPoint = 1;

    // angry consts
    private const float _angrySpeedMultiplier = 2.0f;
    private const float _angryTimer = 5.0f;

    // the time window where we can be hit
    private const float _canBeHitTimer = 5.0f;

    // height of control point when flipping
    private const float _inAirHeight = 5.0f;

    // attack cooldowns and timers
    private const float _shootCooldownBump = 5.0f;
    private const float _shootCooldownBullet = 10.0f;

    private float _shootCooldown = 5.0f;
    private float _shootCooldownTimer = -1.0f;

    // color constants
    private readonly Color _normalColor = Color.black;
    private readonly Color _angryColor = Color.red;
    private readonly Color _canBeHitColor = Color.blue;

    private float _rotateAmount = 90.0f;

    public LineRenderer LineRenderer = null;

    // speed in air
    private float _inAirSpeed = 1.0f;

    /// <summary>
    /// Spawn creature
    /// </summary>
    /// <param name="StartCell"></param>
    public void Spawn(TerrainInstanceCellDataContainer StartCell)
    {
        // get world postion of cell
        Vector3 newPosition = SpawnPosition = TerrainInstanceCellDataContainer.CalculateCellWorldPosition(StartCell);

        // place creature on cell
        transform.position = new Vector3(newPosition.x, newPosition.y + ((collider.height*0.5f)*collider.transform.localScale.y), newPosition.z);
        gameObject.SetActive(true);

        // set new target cell and randomize speed
        LocationData.Cell = StartCell;
        _targetCell = GetRandomNeighborCell(StartCell);
        _speed = Random.Range(_minSpeed, _maxSpeed);
        SetColor(_normalColor);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bulletType"></param>
    /// <returns></returns>
    public override int Hit(Player.BulletTypes bulletType)
    {
        // if we're hit with an increase type of bullet
        if(bulletType== Player.BulletTypes.Increase && _activeState != ActiveState.InAir)
        {
            // flip creature to any neighbouring tile
            UpdateLocationAndApply(this, GameManager.Instance.TerrainManager.FetchCellFromWorldPositionCallback(transform.position));
            FlipToTile(GetRandomNeighborCell(LocationData.Cell));
        }
        // if bullet is of decrease type and creature can be hit
        else if(bulletType== Player.BulletTypes.Decrease && _activeState == ActiveState.CanBeHit)
        {
            // kill creature and return 1 point
            GameManager.Instance.SpawnEmitter("CreatureHit", transform.position);
            gameObject.SetActive(false);
            ActiveCreatures.Remove(this);
            transform.Rotate(Vector3.right, -_rotateAmount);
            return _killPoint;
        }
        // if we hit creture with decrease type of bullet in normal mode
        else if (bulletType == Player.BulletTypes.Decrease && _activeState == ActiveState.Normal)
        {
            // make angry
            SetAngry(true);
        }

        return 0;
    }

    /// <summary>
    /// Return true to automatically be removed from active actor list
    /// </summary>
    /// <returns></returns>
    public override bool ShouldBeRemoved()
    {
        return IsAvailable();
    }

    /// <summary>
    /// Set creature to angry state or not
    /// </summary>
    /// <param name="angry"></param>
    private void SetAngry(bool angry)
    {
        _activeState = angry ? ActiveState.Angry : ActiveState.Normal;
        _speedMulti = angry ? _angrySpeedMultiplier : 1.0f;
        SetColor(angry ? _angryColor : _normalColor);
    }


    /// <summary>
    /// Update the material property block with color
    /// </summary>
    /// <param name="color"></param>
    private void SetColor(Color color)
    {
        if (_materialPropertyBlock == null)
        {
            _renderer = GetComponent<Renderer>();
            _materialPropertyBlock = new MaterialPropertyBlock();
        }

        _color = color;
        _materialPropertyBlock.SetColor("_Color", color);
        _renderer.SetPropertyBlock(_materialPropertyBlock);
    }

    private float SpeedFromDifficulty()
    {
        return _speed + _speed * ((float)((int)GameManager.Instance.CurrentDifficultLevel())*0.5f);
    }

    private void Update()
    {
        // normal or angry state
        if (_activeState == ActiveState.Normal || _activeState == ActiveState.Angry)
        {
            // if angry, count down until its time to reset
            if(_activeState == ActiveState.Angry)
            {
                _stateTimeCounter += Time.deltaTime;
                if (_stateTimeCounter >= _angryTimer)
                {
                    SetAngry(false);
                    _stateTimeCounter = 0.0f;
                }
            }

            // update attack logic
            HandleAttacks();

            // move towards target cell
            MoveTowardsTarget();
        }
        // move the creature in air when being flipped
        else if(_activeState == ActiveState.InAir)
        {
            // while moving through the air
            if (_inAirCounter < 1.0f)
            {
                _inAirCounter += _inAirSpeed * Time.deltaTime;

                MoveAlongAirSpline(_inAirCounter);
            }
            // on landing
            else
            {
                Land();
            }
        }
        // logic for when in can be hit mode
        else if (_activeState == ActiveState.CanBeHit)
        {
            _stateTimeCounter += Time.deltaTime;

            // reset to normal mode if we exceed can be hit timer
            if(_stateTimeCounter >= _canBeHitTimer)
            {
                SetColor(_normalColor);
                transform.Rotate(Vector3.right, -_rotateAmount);
                _activeState = ActiveState.Normal;
                _stateTimeCounter = 0.0f;
            }
        }
    }

    /// <summary>
    /// Move towards target cell
    /// </summary>
    public void MoveTowardsTarget()
    {
        Vector2 currentPos = new Vector2(transform.position.x, transform.position.z);
        Vector2 distanceVector = (_targetCell.CenterPosition() - currentPos);

        // if we're close enough to target, get a new target
        if (distanceVector.magnitude < 0.01f)
        {
            if (GameManager.Instance.CurrentDifficultLevel() == GameManager.DifficultLevel.Easy)
                _targetCell = GetRandomNeighborCell(LocationData.Cell);
            else
                _targetCell = GetRandomNeighborCellTowardsPlayer(LocationData.Cell);

            return;
        }

        // make distance vector direction
        distanceVector.Normalize();

        // move in direction
        Vector2 newPosition = currentPos + distanceVector * Time.deltaTime * (SpeedFromDifficulty() * _speedMulti);
        transform.position = new Vector3(newPosition.x, transform.position.y, newPosition.y);
    }

    /// <summary>
    /// Handle the various attack patterns
    /// </summary>
    public void HandleAttacks()
    {
        // check if we can bump player
        if (_shootCooldownTimer < 0.0f && LocationData.Cell != null && PlayerIsOnNeighborCells(LocationData.Cell))
        {
            LineRenderer.SetPosition(0, transform.position);
            LineRenderer.SetPosition(1, GameManager.Instance.PlayerData.Player.transform.position);
            LineRenderer.enabled = true;

            GameManager.Instance.PlayerData.Player.Bump();
            _shootCooldownTimer = 0.0f;
            _shootCooldown = _shootCooldownBump;
        }
        else if (_shootCooldownTimer < 0.0f && GameManager.Instance.CurrentDifficultLevel() == GameManager.DifficultLevel.Hard && CanSeePlayer())
        {
            if (!GameManager.Instance.HasDifficultyChange)
            {
                Shoot(GameManager.Instance.PlayerData.Player.transform.position);
            }

            _shootCooldownTimer = 0.0f;
            _shootCooldown = Random.Range(_shootCooldownBump, _shootCooldownBullet);
        }
        // shot cooldown
        else if (_shootCooldownTimer >= 0.0f)
        {
            _shootCooldownTimer += Time.deltaTime;
            if (_shootCooldownTimer >= _shootCooldown)
            {
                _shootCooldownTimer = -1.0f;
            }
            else if (LineRenderer.enabled && _shootCooldownTimer >= 0.1f)
                LineRenderer.enabled = false;
        }
    }

    /// <summary>
    /// Land after flipped to tile
    /// </summary>
    public void Land()
    {
        _shouldAutoUpdateHeight = true;
        _stateTimeCounter = 0.0f;

        // if creature was angry before, keep being angry
        if (_speedMulti > 1.0f)
        {
            SetAngry(true);
        }
        // if creature was in can be hit mode before, reset to normal mode
        else if (_color == _canBeHitColor)
        {
            _activeState = ActiveState.Normal;
            SetColor(_normalColor);
        }
        // if creature was in normal mode before, activate can be hit mode
        else
        {
            _activeState = ActiveState.CanBeHit;
            transform.Rotate(Vector3.right, _rotateAmount);
            SetColor(_canBeHitColor);
        }
    }

    /// <summary>
    /// Move along air spline by progress
    /// </summary>
    /// <param name="progress"></param>
    public void MoveAlongAirSpline(float progress)
    {
        // lerp between start point and control point
        Vector3 frontLerp = Vector3.Lerp(_airSplinePosition[0], _airSplinePosition[1], progress);

        // lerp between control point and end point
        Vector3 backLerp = Vector3.Lerp(_airSplinePosition[1], _airSplinePosition[2], progress);

        // lerp between to front and back lerp position to move in an arch
        Vector3 newPosition = Vector3.Lerp(frontLerp, backLerp, progress);

        // dynamically get height on target cell
        float maxHeight = _targetCell.HeightOnCell(TerrainInstanceCellDataContainer.CalculateCellWorldPosition(_targetCell));

        // set new position
        transform.position = new Vector3(newPosition.x, Mathf.Clamp(newPosition.y, maxHeight + ColliderHeight, 2000.0f), newPosition.z);
    }

    /// <summary>
    /// Flipt the creature in air to a certain cell
    /// </summary>
    /// <param name="cell"></param>
    private void FlipToTile(TerrainInstanceCellDataContainer cell)
    {
        // reset rotation if we're already rotted
        if (_activeState == ActiveState.CanBeHit)
        {
            transform.Rotate(Vector3.right, -_rotateAmount);
        }

        // set state variables
        _activeState = ActiveState.InAir;
        _targetCell = cell;
        _shouldAutoUpdateHeight = false;
        _inAirCounter = 0.0f;

        // set spline points
        // start position
        _airSplinePosition[0] = gameObject.transform.position;

        // end point
        _airSplinePosition[2] = TerrainInstanceCellDataContainer.CalculateCellWorldPosition(_targetCell);

        // control point
        _airSplinePosition[1] = (_airSplinePosition[0] + (_airSplinePosition[2] - _airSplinePosition[0]) * 0.5f) + (Vector3.up * _inAirHeight);
    }

    /// <summary>
    /// Shoot a bullet towards target position
    /// </summary>
    /// <param name="targetPosition"></param>
    void Shoot(Vector3 targetPosition)
    {
        Bullet bullet = BulletPool.GetObject();
        bullet.SetColor(Color.white);
        bullet.Shoot(transform.position + Vector3.up*ColliderHeight,
                     targetPosition,
                     10.0f,
                     (TerrainInstanceCellDataContainer cell) =>
                     {
                         // increase cell height
                         GameManager.Instance.TerrainManager.IncreaseHeightAndUpdate(cell, 0.5f);

                         if (PlayerIsOnNeighborCells(cell))
                         {
                             GameManager.Instance.PlayerData.Player.Bump();
                         }
                     });
    }

    /// <summary>
    /// Is there a sight line between creature and player
    /// </summary>
    /// <returns></returns>
    private bool CanSeePlayer()
    {
        if(!Physics.Linecast(transform.position, GameManager.Instance.PlayerData.Player.transform.position, LayerMask.NameToLayer("Actors") ))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if player is on neughbor cells
    /// </summary>
    /// <param name="fromCell"></param>
    /// <returns></returns>
    private bool PlayerIsOnNeighborCells(TerrainInstanceCellDataContainer fromCell)
    {
        if (fromCell.Actors.FindIndex(x => x.IsPlayer())>-1)
            return true;

        GetNeighborCells(fromCell, ref _neighbourTiles);

        for (int i = 0; i < _neighbourTiles.Length; i++)
        {
            TerrainInstanceCellDataContainer cell = GameManager.Instance.TerrainManager.FetchCellFromIndexCallback(_neighbourTiles[i]);
            if (cell!=null && cell.Actors.FindIndex(x => x.IsPlayer()) > -1)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Assign neighbour cells to array
    /// </summary>
    /// <param name="fromCell"></param>
    /// <param name="arrayOutput"></param>
    private void GetNeighborCells(TerrainInstanceCellDataContainer fromCell, ref (int,int)[] arrayOutput)
    {
        arrayOutput[0] = (fromCell.Index.x - 1, fromCell.Index.y + 1);
        arrayOutput[1] = (fromCell.Index.x, fromCell.Index.y + 1);
        arrayOutput[2] = (fromCell.Index.x + 1, fromCell.Index.y + 1);
        arrayOutput[3] = (fromCell.Index.x + 1, fromCell.Index.y);
        arrayOutput[4] = (fromCell.Index.x + 1, fromCell.Index.y - 1);
        arrayOutput[5] = (fromCell.Index.x, fromCell.Index.y - 1);
        arrayOutput[6] = (fromCell.Index.x - 1, fromCell.Index.y - 1);
        arrayOutput[7] = (fromCell.Index.x - 1, fromCell.Index.y);
    }

    /// <summary>
    /// Get a random enighbouring cell
    /// </summary>
    /// <returns></returns>
    public TerrainInstanceCellDataContainer GetRandomNeighborCell(TerrainInstanceCellDataContainer fromCell)
    {
        // set up cell indexes
        GetNeighborCells(fromCell, ref _neighbourTiles);

        // init a new random seed
        Random.InitState(gameObject.GetInstanceID() + System.DateTime.Now.Millisecond);

        // fetch random cell 
        TerrainInstanceCellDataContainer newCell = null;
        do { newCell = GameManager.Instance.TerrainManager.FetchCellFromIndexCallback(_neighbourTiles[Random.Range(0, _neighbourTiles.Length)]); }
        while (newCell == null);

        return newCell;
    }

    /// <summary>
    /// Get a random enighbouring cell facing the player
    /// </summary>
    /// <returns></returns>
    public TerrainInstanceCellDataContainer GetRandomNeighborCellTowardsPlayer(TerrainInstanceCellDataContainer fromCell)
    {
        // set up cell indexes
        GetNeighborCells(fromCell, ref _neighbourTiles);

        // calculate angles 
        List<(float,int)> angles = new List<(float,int)>();
        for (int i = 0; i < _neighbourTiles.Length; i++)
        {
            TerrainInstanceCellDataContainer cell = GameManager.Instance.TerrainManager.FetchCellFromIndexCallback(_neighbourTiles[i]);

            if (cell == null)
                continue;

            angles.Add((CellAngleToPosition(cell, GameManager.Instance.PlayerData.Player.transform.position, transform.position), i));
        }
        // get 3 closest cells towards the player
        List<TerrainInstanceCellDataContainer> closestCells = new List<TerrainInstanceCellDataContainer>();
        for (int i = 0; i < 3; i++)
        {
            (float,int) closest = angles.Aggregate((x, y) => Mathf.Abs(x.Item1 - 0.0f) < Mathf.Abs(y.Item1 - 0.0f) ? x : y);
            closestCells.Add(GameManager.Instance.TerrainManager.FetchCellFromIndexCallback(_neighbourTiles[closest.Item2]));
            angles.RemoveAt(angles.IndexOf(closest));
        }

        // return one of the three cells
        Random.InitState(gameObject.GetInstanceID() + System.DateTime.Now.Millisecond);
        return closestCells[Random.Range(0, closestCells.Count)];
    }

    /// <summary>
    /// Calculate the angle between creature to cell and toPosition (the player for example)
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="toPosition"></param>
    /// <param name="fromPosition"></param>
    /// <returns></returns>
    private float CellAngleToPosition(TerrainInstanceCellDataContainer cell, Vector3 toPosition, Vector3 fromPosition)
    {
        Vector2 toTargetVector = new Vector2(toPosition.x - fromPosition.x, toPosition.z - fromPosition.z);
        Vector2 toCellVector = new Vector2(TerrainInstanceCellDataContainer.CalculateCellWorldPosition(cell).x - fromPosition.x, TerrainInstanceCellDataContainer.CalculateCellWorldPosition(cell).z - fromPosition.z);

        return Vector2.SignedAngle(toTargetVector, toCellVector);
    }
}
