using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Interface to implement for visual representations of the gizmo
/// </summary>
public interface ITerrainEditorGizmoRepresentation
{
    public static ITerrainEditorGizmoRepresentation CurrentRepresentation = null;

    public bool ShouldIncrease();
    public void Increase(float amount, ref bool threasholdReached);
    public void Decrease(float amount, ref bool threasholdReached);
    public void ResetCooldown();
    public GameObject GameObject();
}