using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Container for location data
/// </summary>
public class ActorLocationData
{
    public float Height;
    public TerrainInstanceCellDataContainer Cell;
}

/// <summary>
/// 
/// </summary>
public abstract class BaseActor : MonoBehaviour
{
    /// <summary>
    /// Update base actors height position
    /// </summary>
    /// <param name="actor"></param>
    public static bool UpdateLocationAndApply(BaseActor actor, TerrainInstanceCellDataContainer cell)
    {
        // if actor is flagged to be remove, remove it from cell actors
        if(actor.ShouldBeRemoved())
        {
            actor.LocationData.Cell.Actors.Remove(actor);
            actor.LocationData.Cell = null;
            return true;
        }

        // add actor to cell
        if(actor.LocationData.Cell==null)
        {
            cell.Actors.Add(actor);
        }
        // add actor to new cell, remove from old
        else if (actor.LocationData.Cell.Index != cell.Index)
        {
            actor.LocationData.Cell.Actors.Remove(actor);
            cell.Actors.Add(actor);
        }

        // set new cell
        actor.LocationData.Cell = cell;

        // update and apply height on cell if flag is true
        if (actor._shouldAutoUpdateHeight)
        {
            float newHeight = actor.LocationData.Cell.HeightOnCell(actor.transform.position);

            if (!float.NaN.Equals(newHeight))
                actor.LocationData.Height = newHeight;

            actor.ApplyLocationData(actor.LocationData);
        }

        return false;
    }

    public bool IsPlayer() { return GetInstanceID() == GameManager.Instance.PlayerData.Player.GetInstanceID(); }

    // spawn position
    public Vector3 SpawnPosition = Vector3.positiveInfinity;

    // location data on terrain
    public ActorLocationData LocationData = new ActorLocationData();

    // callback for being hit by player bullet
    public abstract int Hit(Player.BulletTypes bulletType);

    // return true and it will automatically be removed from active actors
    public abstract bool ShouldBeRemoved();

    // actor collider
    private CapsuleCollider _collider = null;

    // flag for updating actors height on the cell, or not
    protected bool _shouldAutoUpdateHeight = true;

    /// <summary>
    /// Get collider
    /// </summary>
    public CapsuleCollider Collider
    {
        get
        {
            if (_collider == null)
                _collider = gameObject.GetComponent<CapsuleCollider>();

            return _collider;
        }
    }

    // height of collider
    private float _height = float.NaN;

    /// <summary>
    /// Get or calculate height of collider
    /// </summary>
    public float ColliderHeight
    {
        get
        {
            if(float.IsNaN(_height))
                _height = (Collider.height*0.5f) * Collider.transform.localScale.y;

            return _height;
        }
    }

    /// <summary>
    /// Apply location data to transform
    /// </summary>
    /// <param name="locationData"></param>
    public void ApplyLocationData(ActorLocationData locationData)
    {
        transform.position = new Vector3(transform.position.x, locationData.Height + ColliderHeight, transform.position.z);
    }
}
