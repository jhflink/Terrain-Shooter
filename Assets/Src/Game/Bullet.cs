using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour, IPoolableObject
{
    // is bullet available for pooling or not
    public bool IsAvailable() => !gameObject.activeSelf;

    // called when being reused in object pool
    public void OnResetCallback() { }

    // called when being spawned by spawn pool
    public void OnSpawnCallback(Vector3 position) { }

    /// <summary>
    /// Instantiate bullet callback
    /// </summary>
    /// <returns></returns>
    public static IPoolableObject InstantiateBullet()
    {
        return Instantiate<Bullet>(GameManager.Instance.Prefabs.Bullet);
    }

    // hit collider
    public SphereCollider Collider;

    // moving direction
    private Vector3 _targetDirection = Vector3.negativeInfinity;

    // moving speed
    private float _speed;

    // current closest cell
    private TerrainInstanceCellDataContainer _currentCell;

    // callback for when hitting a cell
    private System.Action<TerrainInstanceCellDataContainer> _hitCallback = null;

    // material property block
    private MaterialPropertyBlock _materialPropertyBlock = null;

    // renderer component
    private Renderer _renderer = null;

    // material color
    private Color _color = Color.white;

    /// <summary>
    /// shot bullet in direction of target position
    /// </summary>
    /// <param name="startPosition"></param>
    /// <param name="targetPosition"></param>
    /// <param name="speed"></param>
    /// <param name="hitCallback"></param>
    public void Shoot(Vector3 startPosition, Vector3 targetPosition, float speed, System.Action<TerrainInstanceCellDataContainer> hitCallback)
    {
        _hitCallback = hitCallback;
        transform.position = startPosition;
        _targetDirection = (targetPosition - startPosition).normalized;
        _speed = speed;

        gameObject.SetActive(true);
    }

    private void Update()
    {
        // check if bullet hit something
        (bool didHit, int vertexIndex) result = DidHit();

        // if it did invoke hit callback
        if(result.didHit)
        {
            gameObject.SetActive(false);

            if (result.vertexIndex > -1)
            {
                _hitCallback!.Invoke(_currentCell);
            }
        }
        // else keep moving in target direction
        else
            transform.position += _targetDirection * (_speed * Time.deltaTime);
    }

    /// <summary>
    /// Check if bullet hit anything
    /// </summary>
    /// <returns></returns>
    private (bool,int) DidHit()
    {
        // make sure it's withing terrain bounds
        if (!GameManager.Instance.TerrainManager.WithinMainBounds(transform.position))
            return (true,-1);

        // get the cell that we are current above or on
        _currentCell = GameManager.Instance.TerrainManager.FetchCellFromWorldPositionCallback(transform.position);

        // if no cell or same cell as laste frame, return
        if (_currentCell == null || GameManager.Instance.PlayerData.Player.LocationData.Cell.Index==_currentCell.Index)
            return (false,-1);

        // get height on cell triangle
        float cellHeight = _currentCell.HeightOnCell(transform.position);

        // calculate difference between height on cell and actual height
        float hightDifference = Mathf.Abs(cellHeight - transform.position.y);

        // if height difference is either below cell or withing collider radius it's a hit!
        if (transform.position.y < cellHeight || hightDifference < Collider.radius * transform.localScale.x)
        {
            // return result bool and index of closest vertex point
            return (true,_currentCell.ClosestVertexPointToPosition(transform.position));
        }

        return (false,-1);
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

        _color = color;
        _materialPropertyBlock.SetColor("_Color", color);
        _renderer.SetPropertyBlock(_materialPropertyBlock);
    }
}