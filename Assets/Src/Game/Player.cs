using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class handling player action and movement
/// </summary>
public class Player : BaseActor
{
    // pool of bullets
    public ObjectPool<Bullet> BulletPool = null;

    // container with player data
    [HideInInspector]
    public PlayerDataContainer DataContainer = null;

    // list to map a input key to a direction
    private List<(KeyCode, Vector3)> _keys = null;

    // how fast we're slowing down
    public float dampingCoefficient = 5.0f;

    // how fast we are accelerating
    public float acceleration = 200.0f;

    // multiplier for accelerating
    public float accSprintMultiplier = 4.0f;

    // current velocity, direction, and height
    private Vector3 _velocity;
    private Vector3 _currentDirecton;
    private Vector3 _height;

    // returns a normalized velocity vector
    public float NormalizedVelocity => _velocity.magnitude;

    // our types of bullets
    public enum BulletTypes
    {
        Increase = 0,
        Decrease = 1,
    }

    private BulletTypes _currentBulletType = BulletTypes.Increase;

    // callbacks for hitting the terrain with a bullet
    private Dictionary<BulletTypes, System.Action<TerrainInstanceCellDataContainer>> _bulletHitCallbacks = null;

    // caching how many types of bullets we have
    // used for toggling through bullet enum
    private int _numBulletTypes = -1;

    // list of neighbour tiles
    private (int,int)[] _neighbourTiles = new (int,int)[8];

    // bullet speed
    private const float _bulletSpeed = 10.0f;

    // terrain increase/decrease amount
    private const float _increaseDecreaseTerrainAmount = 0.5f;

    // material property block
    private MaterialPropertyBlock _materialPropertyBlock = null;

    // renderer component
    private Renderer _renderer = null;

    // material color
    private Color _bulletColor = Color.white;

    // weapons colors
    private Dictionary<BulletTypes, (Color color, string displayString)> _bulletTypeInfo;

    // get current bullet type display string
    public string CurrentBulletTypeDisplayString => _bulletTypeInfo[_currentBulletType].displayString;

    private const float _bumpTimer = 2.0f;
    private float _bumpTimeCounter = -1;

    public LineRenderer LineRenderer = null;

    // Start is called before the first frame update
    void Start()
    {
        // set up bullet hit callbacks
        _bulletHitCallbacks = new Dictionary<BulletTypes, System.Action<TerrainInstanceCellDataContainer>>() {
            { BulletTypes.Increase,BulletCallbackIncreaseTerrain },
            { BulletTypes.Decrease,BulletCallbackDecreaseTerrain },
        };

        // get number of bullet types and cache it
        _numBulletTypes = System.Enum.GetNames(typeof(BulletTypes)).Length;

        // set up bullet colors
        _bulletTypeInfo = new Dictionary<BulletTypes, (Color color, string displayString)>(_numBulletTypes) { { BulletTypes.Increase, (Color.white, "Increase Terrain") },
                                                                                                              { BulletTypes.Decrease, (Color.blue, "Decrease Terrain") } };

        // set up movement controls
        _keys = new List<(KeyCode, Vector3)>() {
            (KeyCode.W, Vector3.forward),
            (KeyCode.A, Vector3.left),
            (KeyCode.S, Vector3.back),
            (KeyCode.D, Vector3.right),
        };

        // set height
        _height = new Vector3(0.0f, (Collider.height*transform.localScale.x) * 0.5f, 0.0f);
    }

    // Update is called once per frame
    void Update()
    {
        // get any new acceleration
        _velocity += GetAccelerationVector() * Time.deltaTime;

        // apply velocity
        _velocity = Vector3.Lerp(_velocity, Vector3.zero, dampingCoefficient * Time.deltaTime);
        transform.position += _velocity * Time.deltaTime;

        // correct position if we're out of bounds
        Vector3 correctedPosition = GameManager.Instance.TerrainManager.CorrectPositionToBeWithinBounds(transform.position);
        transform.position = new Vector3(correctedPosition.x, transform.position.y, correctedPosition.z);

        // keep the camera look at point slightly infront of the player direction
        Vector3 smoothFollow = Vector3.Lerp(DataContainer.CameraFollow.transform.position + _currentDirecton.normalized*0.01f, DataContainer.Player.transform.position, 2.0f * Time.deltaTime);
        DataContainer.CameraFollow.transform.position = smoothFollow;

        // fire bullets
        if(Input.GetMouseButtonDown(0))
        {
            // get cell from mouse position and use as direction for bullet
            (TerrainInstanceCellDataContainer target, Vector3 hitPoint) = GameManager.Instance.TerrainManager.CellFromMousePosition(rayDistance:500.0f);
            if(target!=null) {
                Shoot(hitPoint);
            }
        }
        /*else
        {
            (TerrainInstanceCellDataContainer target, Vector3 hitPoint) = GameManager.Instance.TerrainManager.CellFromMousePosition(rayDistance: 500.0f);
            if (target != null)
            {
                RaycastHit hit;
                //if (Physics.Linecast(transform.position, hitPoint, out hit, LayerMask.NameToLayer("Actors")))
                if(Physics.Raycast(transform.position, (hitPoint-transform.position).normalized, out hit, 500.0f))
                {
                    LineRenderer.SetPosition(0, transform.position);
                    LineRenderer.SetPosition(1, hit.point);
                }
            }
        }*/

        // bump
        if(_bumpTimeCounter>=0.0f)
        {
            _bumpTimeCounter += Time.deltaTime;
            if(_bumpTimeCounter>=_bumpTimer)
            {
                SetColor(Color.white);
                _bumpTimeCounter = -1.0f;
            }
        }

        // change bullet type
#if UNITY_EDITOR
        if(Input.GetKeyDown(KeyCode.Q))
#else
        if(Input.GetKeyDown(KeyCode.Space))
#endif
        {
            int nextBulletType = (int)_currentBulletType + 1;

            if (nextBulletType >= _numBulletTypes)
                nextBulletType = 0;

            _currentBulletType = (BulletTypes)nextBulletType;

            _bulletColor = _bulletTypeInfo[_currentBulletType].color;
            SetColor(_bulletColor);
        }
    }

    /// <summary>
    /// If player gets hit by bullet
    /// </summary>
    /// <param name="bulletType"></param>
    /// <returns></returns>
    public override int Hit(BulletTypes bulletType)
    {
        return 0;
    }

    public void Bump()
    {
        SetColor(Color.red);
        DataContainer.Health -= 5;
        _bumpTimeCounter = 0.0f;
    }

    /// <summary>
    /// Should be removed from actors
    /// </summary>
    /// <returns></returns>
    public override bool ShouldBeRemoved()
    {
        // never remove player
        return false;
    }

    /// <summary>
    /// Shoot a bullet towards target position
    /// </summary>
    /// <param name="targetPosition"></param>
    void Shoot(Vector3 targetPosition)
    {
        Bullet bullet = BulletPool.GetObject();
        bullet.SetColor(_bulletColor);
        bullet.Shoot(transform.position + _height,
                     targetPosition,
                     _bulletSpeed,
                     _bulletHitCallbacks[_currentBulletType]);
    }

    /// <summary>
    /// Callback when hitting cell with bullet type increase
    /// </summary>
    /// <param name="cell"></param>
    private void BulletCallbackIncreaseTerrain(TerrainInstanceCellDataContainer cell)
    {
        // increase cell height
        GameManager.Instance.TerrainManager.IncreaseHeightAndUpdate(cell, _increaseDecreaseTerrainAmount);

        // try to hit actors on the cell
        HitActorsOnNeighborCells(cell, BulletTypes.Increase);
    }

    /// <summary>
    /// Update the material property block with color
    /// </summary>
    /// <param name="color"></param>
    public void SetColor(Color color)
    {
        if (_materialPropertyBlock == null)
        {
            _renderer = GetComponent<Renderer>();
            _materialPropertyBlock = new MaterialPropertyBlock();
        }

        _materialPropertyBlock.SetColor("_Color", color);
        _renderer.SetPropertyBlock(_materialPropertyBlock);
    }

    /// <summary>
    /// Hit actors on a specific cell
    /// </summary>
    /// <param name="cell"></param>
    private void HitActorsOnNeighborCells(TerrainInstanceCellDataContainer cell, BulletTypes bulletType)
    {
        // number of hits
        int hits = 0;

        // hit actors on main cell
        for(int i=cell.Actors.Count-1; i>-1; i--)
            hits += cell.Actors[i].Hit(bulletType);

        // get neighbor cells
        _neighbourTiles[0] = (cell.Index.x + 1, cell.Index.y);
        _neighbourTiles[1] = (cell.Index.x + 1, cell.Index.y - 1);
        _neighbourTiles[2] = (cell.Index.x, cell.Index.y - 1);
        _neighbourTiles[3] = (cell.Index.x - 1, cell.Index.y - 1);
        _neighbourTiles[4] = (cell.Index.x - 1, cell.Index.y);
        _neighbourTiles[5] = (cell.Index.x - 1, cell.Index.y + 1);
        _neighbourTiles[6] = (cell.Index.x, cell.Index.y + 1);
        _neighbourTiles[7] = (cell.Index.x + 1, cell.Index.y + 1);

        // try to hit actors on neighbor cells
        foreach ((int, int) cellIndex in _neighbourTiles)
        {
            TerrainInstanceCellDataContainer neighborCell = GameManager.Instance.TerrainManager.FetchCellFromIndexCallback(cellIndex);

            if (neighborCell == null)
                continue;

            for (int i = neighborCell.Actors.Count-1; i > -1; i--)
                hits += neighborCell.Actors[i].Hit(bulletType);
        }

        // add points from number of hits
        GameManager.Instance.AddPointsFromHits(hits);
    }

    /// <summary>
    /// Callback for hitting a cell with a decrease bullet type
    /// </summary>
    /// <param name="cell"></param>
    private void BulletCallbackDecreaseTerrain(TerrainInstanceCellDataContainer cell)
    {
        GameManager.Instance.TerrainManager.IncreaseHeightAndUpdate(cell, -_increaseDecreaseTerrainAmount);

        HitActorsOnNeighborCells(cell, BulletTypes.Decrease);
    }

    /// <summary>
    /// Calculate velocity from input
    /// </summary>
    /// <returns></returns>
	Vector3 GetAccelerationVector()
    {
        Vector3 moveInput = default;

        // check if we had any input and apply direction vector
        foreach ((KeyCode keyCode, Vector3 direction) keyMap in _keys)
        {
            if (Input.GetKey(keyMap.keyCode))
            {
                moveInput += keyMap.direction;
            }
        }

        // transform vector from local to world space
        _currentDirecton = transform.TransformVector(moveInput.normalized);

        // return direction vector with multiplyer if we hold left shift aka run
        if (Input.GetKey(KeyCode.LeftShift))
            return _currentDirecton * (acceleration * accSprintMultiplier);

        // return riection vector 
        return _currentDirecton * acceleration;
    }
}
