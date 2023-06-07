using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainInstanceMeshData {

    /// <summary>
    /// Our internal arrays of mesh data for manipulation/look-up
    /// use this instead of accessing the actual mesh data directly
    /// since that would create unneccessary copies of the data 
    /// then assign either of these back to the mesh when finished.
    /// </summary>
    private Vector3[] vertices = null;     // all vertices of the terrain
    private int[] activeVertices = null;   // use this if we just ant to render a sub part of the terrain
    private Vector2[] uvs = null;          // texture uvs
    private int[] triangles = null;        // triangles
    private Color[] colors = null;         // vertex colors

    // some helper accessors
    public int VertexCount => vertices.Length;
    public int ColorCount => colors.Length;
    public int ActiveVerticesCount => activeVertices.Length;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="vertexSize">Size of vertex array</param>
    /// <param name="trianglesSize">Size of triangle array</param>
    public TerrainInstanceMeshData(int vertexSize, int trianglesSize) {
        vertices = new Vector3[vertexSize];
        activeVertices = new int[vertexSize];
        uvs = new Vector2[vertexSize];
        colors = new Color[vertexSize];
        
        triangles = new int[trianglesSize];
    }

    /// <summary>
    /// Set a specific vertex color
    /// </summary>
    /// <param name="index"></param>
    /// <param name="color"></param>
    public void SetColorValue(int index, Color color) {
        if (index < 0 || index >= colors.Length)
            return;

        colors[index] = color;
    }

    /// <summary>
    /// Set a specific vertex position
    /// </summary>
    /// <param name="index"></param>
    /// <param name="position"></param>
    public void SetVertexValue(int index, Vector3 position) {
        if (index < 0 || index >= vertices.Length)
            return;

        vertices[index] = position;
    }

    /// <summary>
    /// Set a specific vertex height
    /// </summary>
    /// <param name="index"></param>
    /// <param name="position"></param>
    public void SetVertexHeight(int index, float height) {
        if (index < 0 || index >= vertices.Length)
            return;

        vertices[index].y = height;
    }

    /// <summary>
    /// Set a specific UV value
    /// </summary>
    /// <param name="index"></param>
    /// <param name="position"></param>
    public void SetUvValue(int index, Vector2 uv) {
        if (index < 0 || index >= uvs.Length)
            return;

        uvs[index] = uv;
    }

    /// <summary>
    /// Set specific active vertex status
    /// </summary>
    /// <param name="index"></param>
    /// <param name="position"></param>
    public void SetActiveVertexStatus(int index, int status) {
        if (index < 0 || index >= activeVertices.Length)
            return;

        activeVertices[index] = status;
    }

    /// <summary>
    /// Set a specific traingle value
    /// </summary>
    /// <param name="index"></param>
    /// <param name="position"></param>
    public void SetTriangleValue(int index, int triangleVertexIndex) {
        if (index < 0 || index >= triangles.Length)
            return;

        triangles[index] = triangleVertexIndex;
    }

    /// <summary>
    /// Copy color array to mesh
    /// </summary>
    /// <param name="copyToMesh"></param>
    public void CopyColorToMesh(Mesh copyToMesh) {
        copyToMesh.colors = colors;
    }

    /// <summary>
    /// Copy vertex array to mesh
    /// </summary>
    /// <param name="copyToMesh"></param>
    public void CopyVerticesToMesh(Mesh copyToMesh) {
        copyToMesh.vertices = vertices;
    }

    /// <summary>
    /// Copy UV array to mesh
    /// </summary>
    /// <param name="copyToMesh"></param>
    public void CopyUVsToMesh(Mesh copyToMesh) {
        copyToMesh.uv = uvs;
    }

    /// <summary>
    /// Copy triangle array to mesh
    /// </summary>
    /// <param name="copyToMesh"></param>
    public void CopyTrianglesToMesh(Mesh copyToMesh) {
        copyToMesh.triangles = triangles;
    }

    /// <summary>
    /// Copy all mesh data to mesh
    /// </summary>
    /// <param name="copyToMesh"></param>
    public void CopyAllMeshDataToMesh(Mesh copyToMesh) {
        CopyVerticesToMesh(copyToMesh);
        CopyColorToMesh(copyToMesh);
        CopyUVsToMesh(copyToMesh);
        CopyTrianglesToMesh(copyToMesh);
    }

    /// <summary>
    /// Increase vertex height by height and update color value
    /// </summary>
    /// <param name="vertexIndex">index of vertex to update</param>
    /// <param name="height">the height amount</param>
    /// <param name="colorsArray">the color array to save the new color to</param>
    public void IncreaseVertexHeightAndUpdateColor(int vertexIndex, float height, System.Func<float, Color> vertexColorCallback) {
        SetVertexHeightAndUpdateColor(vertexIndex, vertices[vertexIndex].y + height, vertexColorCallback);
    }

    /// <summary>
    /// Set a specific height to a vertex and update its color
    /// </summary>
    /// <param name="vertexIndex">index of vertex to update</param>
    /// <param name="height">the specific height value</param>
    /// <param name="colorsArray">array to save the new color value to</param>
    public void SetVertexHeightAndUpdateColor(int vertexIndex, float height, System.Func<float, Color> vertexColorCallback) {

        // assign new height value
        vertices[vertexIndex].y = height;

        // convert height value to color gradient value
        float colorHeight = Mathf.InverseLerp(-1.0f, 5, vertices[vertexIndex].y);

        // assign new color value
        colors[vertexIndex] = vertexColorCallback(colorHeight);
    }

    /// <summary>
    /// Get height of vertex
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Vector3 VertexPosition(int index) {
        if (index < 0 || index >= vertices.Length)
            return Vector3.zero;

        return vertices[index];
    }

    /// <summary>
    /// Get height of vertex
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public (int,int,int) TriangleData(int index)
    {
        if (index < 0 || index >= triangles.Length)
            return (-1,-1,-1);

        return (triangles[index], triangles[index+1], triangles[index+2]);
    }

    /// <summary>
    /// Get height of vertex
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public float VertexHeight(int index) {
        if (index < 0 || index >= vertices.Length)
            return float.NaN;

        return vertices[index].y;
    }

    /// <summary>
    /// Get height of vertex
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Color VertexColor(int index) {
        if (index < 0 || index >= colors.Length)
            return Color.magenta;

        return colors[index];
    }

    /// <summary>
    /// Get height of vertex
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public int ActiveVertexState(int index) {
        if (index < 0 || index >= activeVertices.Length)
            return -1;

        return activeVertices[index];
    }
}
