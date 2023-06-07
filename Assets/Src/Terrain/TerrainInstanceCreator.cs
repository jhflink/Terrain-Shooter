using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component to add to the main terrain editor game object in order to instansiate a terrain
/// </summary>
public class TerrainInstanceCreator : MonoBehaviour {
    // id of terrain instance
    public string id = "";

    // the material that will apply to the mesh
    public Material material = null;

    // the gradient of the vertex height colors
    public Gradient gradient;

    // should the mesh recieve shadows or not
    public bool recieveShadows = true;

    // should we generate a mesh collider for it
    public bool generateMeshCollider = true;

    // a animation curve to specify the height distribution from the perlin noise
    public AnimationCurve heightDistributionCurve;

    // the min,max scale value that will be randomized for the perlin noise generation
    public Vector2Int minMaxPerlinNoiseScale = new Vector2Int();
}
