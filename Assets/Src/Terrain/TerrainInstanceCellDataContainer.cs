using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Container to bundle vertices into a cell
/// </summary>
public class TerrainInstanceCellDataContainer {

    // cell index
    public (int x, int y) Index = (0, 0);

    // indexes of vertices
    public int[] VerticesIndexes = null;

    // left and right triangle indexes
    public (int left, int right) TrianglesIndex;

    // world position
    public Vector3 WorldPosition = Vector3.negativeInfinity;

    // instance owner
    public TerrainInstance Owner = null;

    // list of actors currently on the cell
    // TODO: move outside terrain system
    public List<BaseActor> Actors = new List<BaseActor>();

    // vertex indexes of left triangle
    private Vector3[] _leftTrianglePositions = new Vector3[3];

    // vertex indexes of right triangle
    private Vector3[] _rightTtrianglePositions = new Vector3[3];

    // world position of vertexes
    private Vector3[] _vertexWorldPositions = null;

    /// <summary>
    /// Create cell data with index and its vertices index
    /// </summary>
    /// <param name="index"></param>
    /// <param name="verticesIndexes"></param>
    public TerrainInstanceCellDataContainer((int x, int y) index, TerrainInstance owner, (int left, int right) trianglesIndex, params int[] verticesIndexes) {
        Index = index;
        VerticesIndexes = verticesIndexes;
        TrianglesIndex = trianglesIndex;
        Owner = owner;
    }

    /// <summary>
    /// Get the height on cell from position
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public float HeightOnCell(Vector3 position)
    {
        /// <summary>
        /// Test if point is on triangle
        /// </summary>
        bool TestPointOnTriangle(Vector3 localPosition, int triangleIndex, ref Vector3[] trianglePosition)
        {
            // get traingle vertex points
            Owner.VertexPointsFromTriangleIndex(triangleIndex, in trianglePosition);

            // test point on triangle
            bool result = IsPointInTriangle(new Vector3(localPosition.x, 0.0f, localPosition.z),
                                   trianglePosition[2],
                                   trianglePosition[1],
                                   trianglePosition[0]);

            return result;
        }

        // transform world to local positon
        Vector3 localPosition = Owner.InvereseTransformPoint(position);

        // test which triangle the point is intersection with
        Vector3[] triangleVertexPositions = null;
        if (TestPointOnTriangle(localPosition, TrianglesIndex.left, ref _leftTrianglePositions))
            triangleVertexPositions = _leftTrianglePositions;
        else if (TestPointOnTriangle(localPosition, TrianglesIndex.right, ref _rightTtrianglePositions))
            triangleVertexPositions = _rightTtrianglePositions;
        else
        {
            Debug.LogError("No triangle");
            return float.NaN; 
        }

        // calculate the height on the triangle
        float heightOnPlace = GetYOnTriangle(triangleVertexPositions[0],
                                             triangleVertexPositions[1],
                                             triangleVertexPositions[2],
                                             localPosition.x, localPosition.z);

        return heightOnPlace;
    }

    private Vector2 _centerPosition = Vector2.zero;
    /// <summary>
    /// Calculate center position of cell
    /// </summary>
    /// <returns></returns>
    public Vector2 CenterPosition()
    {
        if (_centerPosition.Equals(Vector2.zero) == false)
            return _centerPosition;

        Vector2 min = Vector2.positiveInfinity;
        Vector2 max = Vector2.negativeInfinity;

        for (int i = 0; i < VerticesIndexes.Length; i++)
        {
            Vector3 vertexPosition = Owner.VertexPosition(VerticesIndexes[i]);
            if (vertexPosition.x < min.x)
                min.Set(vertexPosition.x, min.y);

            if (vertexPosition.x > max.x)
                max.Set(vertexPosition.x, max.y);

            if (vertexPosition.z < min.y)
                min.Set(min.y, vertexPosition.z);

            if (vertexPosition.z > max.y)
                max.Set(max.y, vertexPosition.z);
        }

        return _centerPosition = Owner.TransformPoint(new Vector2(min.x + (max.x-min.x)*0.5f, min.y + (max.y-min.y)*0.5f));
    }

    /// <summary>
    /// Get the closest vertex point to a position
    /// </summary>
    /// <param name="position"></param>
    /// <param name="minDistance"></param>
    /// <returns></returns>
    public int ClosestVertexPointToPosition(Vector3 position, float minDistance=-1.0f)
    {
        // if we don't have a world position of vertex, calculate one
        if(_vertexWorldPositions==null)
        {
            _vertexWorldPositions = new Vector3[VerticesIndexes.Length];
            for (int i=0; i<VerticesIndexes.Length; i++)
            {
                _vertexWorldPositions[i] = Owner.TransformPoint(Owner.VertexPosition(VerticesIndexes[i]));
            }
        }

        // check closest distance
        float closestDistance = 0.0f;
        int closestVertexIndex = -1;
        for(int i=0;i<_vertexWorldPositions.Length;i++)
        {
            float d = Vector3.Distance(_vertexWorldPositions[i], position);
            if((closestVertexIndex == -1 || d<closestDistance) && (d < minDistance || minDistance < 0.0f))
            {
                closestDistance = d;
                closestVertexIndex = i;
            }
        }

        return closestVertexIndex;
    }

    /// <summary>
    /// Calculate the height of a cell from it's vertices
    /// </summary>
    /// <param name="CellData">CellData to get vertices from</param>
    /// <returns></returns>
    public static float HeightOfCell(TerrainInstanceCellDataContainer cellData) {
        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;

        for (int i = 0; i < cellData.VerticesIndexes.Length; i++) {
            float vertexHeight = cellData.Owner.VertexPosition(cellData.VerticesIndexes[i]).y;
            if (vertexHeight > maxHeight)
                maxHeight = vertexHeight;

            if (vertexHeight < minHeight)
                minHeight = vertexHeight;
        }

        float height = minHeight + (maxHeight - minHeight) * 0.5f;

        return height;
    }

    /// <summary>
    /// Get the world position of a cell
    /// </summary>
    /// <param name="CellData">Cell to calculate</param>
    /// <returns>The world position of cells lower left corner</returns>
    public static Vector3 CalculateCellWorldPosition(TerrainInstanceCellDataContainer cellData) {
        // if we have a cached world position just return it
        //if (!cellData.WorldPosition.Equals(Vector3.negativeInfinity))
          //  return cellData.WorldPosition;

        Vector2 min = Vector2.positiveInfinity;

        for (int i = 0; i < cellData.VerticesIndexes.Length; i++) {
            Vector3 vertexPosition = cellData.Owner.VertexPosition(cellData.VerticesIndexes[i]);
            if (vertexPosition.x < min.x)
                min.Set(vertexPosition.x, min.y);

            if (vertexPosition.z < min.y)
                min.Set(min.y, vertexPosition.z);
        }

        // transform the local position to world
        return cellData.WorldPosition = cellData.Owner.TransformPoint(new Vector3(min.x, HeightOfCell(cellData), min.y));
    }

    /// <summary>
    /// Test if point is inside triangle
    /// </summary>
    /// <param name="p"></param>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    public bool IsPointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        // Precompute the denominator
        float denominator = ((b.z - c.z) * (a.x - c.x)) + ((c.x - b.x) * (a.z - c.z));

        // Calculate the barycentric coordinates of the point with respect to the triangle
        float coordinate1 = ((b.z - c.z) * (p.x - c.x)) + ((c.x - b.x) * (p.z - c.z)) / denominator;
        float coordinate2 = ((c.z - a.z) * (p.x - c.x)) + ((a.x - c.x) * (p.z - c.z)) / denominator;

        // Use short-circuit evaluation to avoid calculating the third coordinate if the point is already outside the triangle
        if (coordinate1 < 0 || coordinate2 < 0)
        {
            return false;
        }

        float coordinate3 = 1 - coordinate1 - coordinate2;

        // Check if all coordinates are non-negative
        return coordinate3 >= 0;
    }

    /// <summary>
    /// Get the height value of a point on triangle
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="p3"></param>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public static float GetYOnTriangle(Vector3 p1, Vector3 p2, Vector3 p3, float x, float z)
    {
        Vector3 p2MinusP1 = p2 - p1;
        Vector3 p3MinusP1 = p3 - p1;

        Vector3 cross = Vector3.Cross(p2MinusP1, p3MinusP1);

        if (Mathf.Abs(cross.y) < 0.00001)
        {
            return 0.0f;
        }

        return -(cross.x * (x - p1.x) + cross.z * (z - p1.z)) / cross.y + p1.y;

    }
}