using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class to keep and handle all modificatrions of terrain data
/// </summary>
public class TerrainInstance {
    // basic data container for the terrain
    private TerrainInstanceDataContainer _dataContainer = null;

    // all cells
    private TerrainInstanceCellDataContainer[] _cellDataContainer = null;

    // terrain game object on the Unity scene
    private GameObject _terrainObject = null;

    // mesh components attached to the game object
    private Mesh _terrainMesh = null;
    private MeshFilter _terrainMeshFilter = null;
    private MeshRenderer _terrainMeshRenderer = null;
    private MeshCollider _terrainMeshCollider = null;
    private Material _terrainMaterial = null;

    // the basic data needed to create a terrain
    // provided through a component on the TerrainEditMain object
    private TerrainInstanceCreator _terrainInstanceCreator = null;

    // data for perlin noise generation
    private TerrainEditorPerlinNoiseGeneratorDataContainer _terrainPerlinNoiseData = null;

    // the mesh data
    private TerrainInstanceMeshData meshData = null;

    // bounds
    private BoxCollider _boundsCollider;

    // buffers and data structures to send position and colors to the shader
    // ComputBuffers are used since the maximum array length that can be sent is 1023
    private ComputeBufferContainer<float> _positionBufferContainer = null;
    private ComputeBufferContainer<float> _colorsBufferContainer = null;
    private const int _positionBufferLength = 4;
    private const int _colorBufferLength = 3;

    /// <summary>
    /// Get a specific vertex position
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Vector3 VertexPosition(int index) {
        return meshData.VertexPosition(index); 
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <param name="vertexPositions"></param>
    public void VertexPointsFromTriangleIndex(int index, in Vector3[] vertexPositions)
    {
        (int v1, int v2, int v3) triangleData = meshData.TriangleData(index);

        if (triangleData.v1 == -1)
            return;

        vertexPositions[0] = meshData.VertexPosition(triangleData.v1);
        vertexPositions[1] = meshData.VertexPosition(triangleData.v2);
        vertexPositions[2] = meshData.VertexPosition(triangleData.v3);
    }

    // some helper accessors
    public int VertexCount => meshData.VertexCount;
    public bool HasGameObject => _terrainObject != null;
    public string Id => _dataContainer.Id;
    public TerrainInstanceDataContainer DataContainer => _dataContainer;

    /// <summary>
    /// Hides or show the whole terrain by disabling the mesh renderer
    /// </summary>
    public bool Hide {
        set { _terrainMeshRenderer!.enabled = !value; }
    }

    /// <summary>
    /// Transform local point space to worold
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public Vector3 TransformPoint(Vector3 point) {
        return _terrainObject.transform.TransformPoint(point);
    }

    /// <summary>
    /// Transform world position to local
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public Vector3 InvereseTransformPoint(Vector3 point)
    {
        return _terrainObject.transform.InverseTransformDirection(point);
    }

    /// <summary>
    /// Set color of cells vertices
    /// </summary>
    /// <param name="cell">Cell to get vertices from</param>
    /// <param name="color">Color to apply</param>
    public void SetCellColor(TerrainInstanceCellDataContainer cell, Color color) {
        foreach (int vertexIndex in cell.VerticesIndexes)
            meshData.SetColorValue(vertexIndex, color);

        meshData.CopyColorToMesh(_terrainMesh);
        //_terrainMesh.colors = meshData.colors;
    }

    /// <summary>
    /// Reset the color values to it's original based on the height
    /// </summary>
    /// <param name="cell">Cell to get vertices from</param>
    public void ResetCellColor(TerrainInstanceCellDataContainer cell) {
        foreach(int vertexIndex in cell.VerticesIndexes)
        {
            float colorHeight = Mathf.InverseLerp(-1.0f, 5, meshData.VertexHeight(vertexIndex));
            meshData.SetColorValue(vertexIndex, _terrainInstanceCreator.gradient.Evaluate(colorHeight));
        }

        meshData.CopyColorToMesh(_terrainMesh);
    }

    /// <summary>
    /// Save all the vertices height data into a container
    /// that can be used to save the terrain to disk
    /// </summary>
    public void SaveHeightDataToDataContainer() {
        if (_dataContainer.VertexHeightData == null)
            _dataContainer.VertexHeightData = new List<float>(meshData.VertexCount);
        else
            _dataContainer.VertexHeightData.Clear();

        for (int i = 0; i < meshData.VertexCount; i++)
            _dataContainer.VertexHeightData.Add(meshData.VertexHeight(i));
    }

    /// <summary>
    /// Create a new terrain instance
    /// </summary>
    /// <param name="terrainInstanceCreator">Creator component to provide initial data</param>
    /// <param name="terrainObject">The game object to attach the mesh to</param>
    /// <param name="size">Size of terrain in number of cells</param>
    public TerrainInstance(TerrainInstanceCreator terrainInstanceCreator, GameObject terrainObject, Vector2Int size) {
        _terrainInstanceCreator = terrainInstanceCreator;
        InitializeTerrain(_dataContainer = new TerrainInstanceDataContainer((size.x, size.y), _terrainInstanceCreator.id), terrainObject, _terrainInstanceCreator.material);
    }

    /// <summary>
    /// Regenerate the height for the terrain
    /// </summary>
    /// <param name="size">Size of the terrain in cells</param>e
    /// <param name="verticesToModify">Send null to modify all the vertices, or a specific list of vertices to modify</param>
    public void ReGenerate((int x, int y) size, List<int> verticesToModify = null) {
        const int maxHeight = 10;
        const int minHeight = 2;

        // generate a new noise
        float[] heightNoise = null;
        TerrainEditorPerlinNoiseGenerator.Generate(_terrainPerlinNoiseData = new TerrainEditorPerlinNoiseGeneratorDataContainer() {
            sizeX = size.x+1,
            sizeY = size.y + 1,
            sampleOriginX = UnityEngine.Random.Range(0.0f, size.x + 1),
            sampleOriginY = UnityEngine.Random.Range(0.0f, size.y + 1),
            scale = UnityEngine.Random.Range(_terrainInstanceCreator.minMaxPerlinNoiseScale.x, _terrainInstanceCreator.minMaxPerlinNoiseScale.y),
            octaves = 8,
            persistance = 0.4f,
            lacunarity = 1.9f,
        },
        out heightNoise);

        // assign new height values
        int vertexCount = 0;
        int heightCounter = 0;

        for (int y = 0; y <= _dataContainer.Size.y; y++) {
            for (int x = 0; x <= _dataContainer.Size.x; x++) {
                if (verticesToModify == null || verticesToModify.Find(c => c == vertexCount) != -1) {
                    // set the vertex position and evaluate the noise height on the height distribution scale to get the distribution we want
                    meshData.SetVertexValue(vertexCount, new Vector3(x, _terrainInstanceCreator.heightDistributionCurve.Evaluate(heightNoise[heightCounter]) * maxHeight - minHeight, y));
                    
                    //_vertices[vertexCount] = new Vector3(x, heightNoise[heightCounter] * maxHeight - minHeight, y);
                    meshData.IncreaseVertexHeightAndUpdateColor(vertexCount, 0.0f, _terrainInstanceCreator.gradient.Evaluate);
                    heightCounter++;
                }
                vertexCount++;
            }
        }

        // send new values to gpu
        CopyVertexAndColorDataToGpu(meshData);//._vertices, meshData._colors, meshData._activeVertices);

        // update collider if we have one
        if (_terrainMeshCollider != null) {

            meshData.CopyVerticesToMesh(_terrainMesh);

            _terrainMesh.RecalculateBounds();
            _terrainMesh.RecalculateNormals();
            _terrainMeshCollider.sharedMesh = _terrainMesh;
        }
    }

    /// <summary>
    /// Flatten the height of a number of cells
    /// all the height will take on the height of the lowest vertex
    /// </summary>
    /// <param name="cells">A list of cell indexes of the affected cells</param>
    /// <returns>Returns the new height</returns>
    public float Flatten(List<(int x,int y)> cells) {

        float lowestHeight = float.MaxValue;

        // find lowest vertex height
        foreach((int x,int y) cellIndex in cells) {

            TerrainInstanceCellDataContainer cell = _cellDataContainer[cellIndex.y * DataContainer.Size.x + cellIndex.x];

            float lowestVertexHeight = (int)cell.VerticesIndexes.Min(v => meshData.VertexHeight(v));
            if (lowestVertexHeight < lowestHeight)
                lowestHeight = lowestVertexHeight;
        }

        // apply the new height to all the cells
        foreach ((int x, int y) cellIndex in cells) {

            TerrainInstanceCellDataContainer cell = _cellDataContainer[cellIndex.y * DataContainer.Size.x + cellIndex.x];
            foreach(int vertexIndex in cell.VerticesIndexes)
                meshData.SetVertexHeightAndUpdateColor(vertexIndex, lowestHeight, _terrainInstanceCreator.gradient.Evaluate);
        }

        // update mesh data
        UpdateMeshAfterHeightChange();

        return lowestHeight;
    }

    /// <summary>
    /// Flatten all cells of the terrain to a height
    /// </summary>
    internal void FlattenAllCellsToHeight(float height) {
        for (int i = 0; i < meshData.VertexCount; i++)
            meshData.SetVertexHeightAndUpdateColor(i, height, _terrainInstanceCreator.gradient.Evaluate);

        UpdateMeshAfterHeightChange();
    }

    /// <summary>
    /// Send a raycast to the terrains mesh collider
    /// </summary>
    /// <param name="ray">The ray to send</param>
    /// <param name="hitInfo">If we hit the hit data will be stored here</param>
    /// <param name="maxDistance">Max distance the ray will travel</param>
    /// <returns>true or false depending on if we get a hit or not</returns>
    public bool RaycastMeshCollider(Ray ray, out RaycastHit hitInfo, float maxDistance) {
        return _terrainMeshCollider.Raycast(ray, out hitInfo, maxDistance);
    }

    public Vector3 ClosestPointOnCollider(Vector3 position) {
        return _terrainMeshCollider.ClosestPoint(position);
    }

    /// <summary>
    /// Returns the cell closest to a poin
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public TerrainInstanceCellDataContainer ClosestCellToPoint(Vector3 point) {
        (int x, int y) index = (Math.Clamp(Mathf.FloorToInt(point.x),0,_dataContainer.Size.x-1), Math.Clamp(Mathf.FloorToInt(point.z), 0, _dataContainer.Size.y - 1));

        // calculate 1d index from 2d index
        int cellIndex = (index.y * _dataContainer.Size.x) + index.x;

        if (cellIndex >= _cellDataContainer.Length || cellIndex < 0)
            return null;

        return _cellDataContainer[cellIndex];
    }

    /// <summary>
    /// Get a cell by its index
    /// </summary>
    /// <param name="x">X index</param>
    /// <param name="y">Y index</param>
    /// <returns>if the index is within bounds return the cell, otherwise null</returns>
    public TerrainInstanceCellDataContainer CellFromIndex(int x, int y) {
        // calculate 1d index from 2d index
        int cellIndex = (y * _dataContainer.Size.x) + x;

        if (cellIndex < 0 || cellIndex >= _cellDataContainer.Length)
            return null;

        return _cellDataContainer[cellIndex];
    }

    public void CellsFromIndexes(List<(int, int)> indexes, out List<TerrainInstanceCellDataContainer> Cells)
    {
        Cells = new List<TerrainInstanceCellDataContainer>(indexes.Count);
        for (int i = 0; i < indexes.Count; i++)
            Cells.Add(CellFromIndex(indexes[i].Item1, indexes[i].Item2));
    }

    public bool WithinBounds(Vector3 position)
    {
        return _terrainMeshCollider.bounds.Contains(position);
    }

    public Vector3 ClosestPointOnBounds(Vector3 position)
    {
        return _terrainMeshCollider.ClosestPointOnBounds(position);
    }

    /// <summary>
    /// Initialize necessary terrain variables
    /// </summary>
    /// <param name="dataContainer"></param>
    /// <param name="terrainParentObject"></param>
    /// <param name="terrainMaterial"></param>
    public void InitializeTerrain(TerrainInstanceDataContainer dataContainer, GameObject terrainParentObject, Material terrainMaterial) {
        // assign main game object
        _terrainObject = terrainParentObject;

        // create mesh collider and add to game object
        if (_terrainInstanceCreator.generateMeshCollider)
            _terrainMeshCollider = _terrainObject.AddComponent<MeshCollider>();

        // assign terrain material
        _terrainMaterial = terrainMaterial;

        // create mesh renderer and add to game object
        if (_terrainMeshRenderer == null) {
            _terrainMeshRenderer = _terrainObject.AddComponent<MeshRenderer>();
        }

        // create mesh filter and add to game object
        if (_terrainMeshFilter == null) {
            _terrainMeshFilter = terrainParentObject.AddComponent<MeshFilter>();
        }

        // create mesh and add to game object
        if (_terrainMesh == null) {
            _terrainMeshFilter.sharedMesh = _terrainMesh = new Mesh();
            _terrainMesh.name = "Terrain Mesh";
        }

        // generate mesh data
        GenerateMeshData(dataContainer, out meshData, out _cellDataContainer);

        // clear any mesh data
        _terrainMesh.Clear();

        // copy all mesh data to the actual mesh
        meshData.CopyAllMeshDataToMesh(_terrainMesh);

        // recalculate normals and mesh bounds for collider
        _terrainMesh.RecalculateNormals();
        _terrainMesh.RecalculateBounds();

        // set mesh for the mesh collider
        if(_terrainMeshCollider!=null)
            _terrainMeshCollider.sharedMesh = _terrainMesh;

        // assign materia and if terrain should recieve shadows
        _terrainMeshRenderer.material = _terrainMaterial;
        _terrainMeshRenderer.receiveShadows = _terrainInstanceCreator.recieveShadows;

        // create our position and color buffer that will be sent to the shader
        _positionBufferContainer = new ComputeBufferContainer<float>(meshData.VertexCount,
                                                                     sizeof(float) * _positionBufferLength,
                                                                     meshData.VertexCount * _positionBufferLength);

        _colorsBufferContainer = new ComputeBufferContainer<float>(meshData.VertexCount,
                                                                   sizeof(float) * _colorBufferLength,
                                                                   meshData.ColorCount * _colorBufferLength);
        // copy the data to the shader
        CopyVertexAndColorDataToGpu(meshData);//._vertices, meshData._colors, meshData._activeVertices);
    }

    /// <summary>
    /// Free up some allocation when destroyed
    /// </summary>
    public void Destroy() {
        // free up the memory of the position and color ComputeBuffers used in the shader
        _colorsBufferContainer.Release();
        _positionBufferContainer.Release();
    }

    /// <summary>
    /// Send vertex and color data to the shader
    /// </summary>
    /// <param name="vertices">Vertex data</param>
    /// <param name="colors">Color data</param>
    /// <param name="activeVertices">Data specifying if a vertex will be rendered or not</param>
    private void CopyVertexAndColorDataToGpu(in TerrainInstanceMeshData meshData) {
        // keep track of how many vertices we've done
        int bufferCounter = 0;

        // set the data
        for (int i = 0; i < meshData.VertexCount; i++, bufferCounter += _positionBufferLength) {
            Vector3 vertexPosition = meshData.VertexPosition(i);
            _positionBufferContainer.SetData(bufferCounter, vertexPosition.x);
            _positionBufferContainer.SetData(bufferCounter+1, vertexPosition.y);
            _positionBufferContainer.SetData(bufferCounter+2, vertexPosition.z);
            _positionBufferContainer.SetData(bufferCounter+3, meshData.ActiveVertexState(i));
        }

        // reset buffer counter
        bufferCounter = 0;

        // set the data
        for (int i = 0; i < meshData.ColorCount; i++, bufferCounter += _colorBufferLength) {
            Color vertexColor = meshData.VertexColor(i);
            _colorsBufferContainer.SetData(bufferCounter, vertexColor.r);
            _colorsBufferContainer.SetData(bufferCounter+1, vertexColor.g);
            _colorsBufferContainer.SetData(bufferCounter+2, vertexColor.b);
        }

        _positionBufferContainer.SendDataToMeshRenderer(_terrainMeshRenderer, "_vertexPositions");
        _colorsBufferContainer.SendDataToMeshRenderer(_terrainMeshRenderer, "_vertexColors");

        // recalculate normals
        _terrainMesh.RecalculateNormals();
    }

    /// <summary>
    /// Generate and set up mesh data
    /// </summary>
    /// <param name="dataContainer"></param>
    /// <param name="verticesArray"></param>
    /// <param name="activeVertices"></param>
    /// <param name="uvsArray"></param>
    /// <param name="trianglesArray"></param>
    /// <param name="colorsArray"></param>
    /// <param name="cellDataArray"></param>
    private void GenerateMeshData(TerrainInstanceDataContainer dataContainer,
                                  out TerrainInstanceMeshData meshData,
                                  out TerrainInstanceCellDataContainer[] cellDataArray) {
        // Set up all the vertex data arrays
        int vertexLength = (dataContainer.Size.x + 1) * (dataContainer.Size.y + 1);
        int trianglesLength = dataContainer.Size.x * dataContainer.Size.y * 6;

        // create new mesh data
        meshData = new TerrainInstanceMeshData(vertexLength, trianglesLength);

        // create all the vertices
        int vertexCounter = 0;
        for (int y = 0; y < dataContainer.Size.y+1; y++) {
            for (int x = 0; x < dataContainer.Size.x+1; x++) {
                // set the vertex positions
                meshData.SetVertexValue(vertexCounter, new Vector3(x, 0.0f, y));

                // set the vertex uv
                meshData.SetUvValue(vertexCounter, new Vector2((float)x / dataContainer.Size.x, (float)y / dataContainer.Size.y));

                // set vertex active state
                meshData.SetActiveVertexStatus(vertexCounter, 1);

                // update the vertex color
                meshData.IncreaseVertexHeightAndUpdateColor(vertexCounter, 0.0f, _terrainInstanceCreator.gradient.Evaluate);

                // increment vertex counter
                vertexCounter++;
            }
        }

        // create triangle and cell data array
        cellDataArray = new TerrainInstanceCellDataContainer[dataContainer.Size.x * dataContainer.Size.y];

        // counters
        int cellCount = 0;
        vertexCounter = 0;
        int triangleCount = 0;

        for (int y = 0; y < dataContainer.Size.y; y++) {
            for (int x = 0; x < dataContainer.Size.x; x++) {
                // left triangle
                meshData.SetTriangleValue(triangleCount + 0, vertexCounter + 0);
                meshData.SetTriangleValue(triangleCount + 1, vertexCounter + dataContainer.Size.x + 1);
                meshData.SetTriangleValue(triangleCount + 2, vertexCounter + 1);

                // right triangle
                meshData.SetTriangleValue(triangleCount + 3, vertexCounter + 1);
                meshData.SetTriangleValue(triangleCount + 4, vertexCounter + dataContainer.Size.x + 1);
                meshData.SetTriangleValue(triangleCount + 5, vertexCounter + dataContainer.Size.x + 2);

                // create cell data from 4 vertices
                cellDataArray[cellCount] = new TerrainInstanceCellDataContainer((cellCount % dataContainer.Size.x,
                                                                                 cellCount / dataContainer.Size.y),
                                                                                 this,
                                                                                 (triangleCount, triangleCount + 3),
                                                                                 vertexCounter,
                                                                                 vertexCounter + 1,
                                                                                 vertexCounter + dataContainer.Size.x + 1,
                                                                                 vertexCounter + dataContainer.Size.x + 2);

                // increment counters
                cellCount++;
                vertexCounter++;
                triangleCount += 6;

            }

            // increment vertex counter
            vertexCounter++;
        }
    }

    /// <summary>
    /// Update the cells from a terrain state list
    /// </summary>
    /// <param name="terrainStateList">List of vertices indexs and height</param>
    /// <param name="undoRedoHeight">A functionc all that will flip the height or not depending on circumstances</param>
    public void UndoRedoTerrainState(in List<(int index,double height)> terrainStateList, System.Func<float,float> undoRedoHeight) {
        // apply state
        for (int i = 0; i < terrainStateList.Count; i++){
            meshData.IncreaseVertexHeightAndUpdateColor(terrainStateList[i].index, undoRedoHeight((float)terrainStateList[i].height), _terrainInstanceCreator.gradient.Evaluate);
        }

        UpdateMeshAfterHeightChange();
    }

    /// <summary>
    /// Set position of the terrain game object, height will be locked
    /// </summary>
    /// <param name="vector3">Position vector</param>
    internal void SetXZPosition(Vector3 vector3) {
        _terrainObject.transform.position = new Vector3(vector3.x, _terrainObject.transform.position.y, vector3.z);
    }

    /// <summary>
    /// Call this after vertex or colors has been modify to commit the changes to the mesh
    /// </summary>
    private void UpdateMeshAfterHeightChange() {
        // copy the data to the gpu
        CopyVertexAndColorDataToGpu(meshData);//meshData._._vertices, meshData._colors, meshData._activeVertices);

        // if we have a collider we have to update the vertices
        // on the mesh to generate a new collider
        if (_terrainMeshCollider != null) {

            meshData.CopyVerticesToMesh(_terrainMesh);
            _terrainMesh.RecalculateBounds();
            _terrainMesh.RecalculateNormals();
            _terrainMeshCollider.sharedMesh = _terrainMesh;
        }
    }

    /// <summary>
    /// Set a height on all vertices and update changes
    /// </summary>
    /// <param name="height">Height</param>
    /// <param name="colorCopyTerrainInstance">terrain to merge colors from</param>
    public void SetAllHeightAndUpdate(float height, TerrainInstance colorCopyTerrainInstance) {

        for (int i = 0; i < meshData.VertexCount; i++) {
            //meshData.vertices[i].y = height;
            meshData.SetVertexHeight(i, height);

            if(colorCopyTerrainInstance!=null) {
                meshData.SetColorValue(i, _terrainInstanceCreator.gradient.Evaluate(0.0f) + colorCopyTerrainInstance.meshData.VertexColor(i) * 0.5f);
            }
        }

        UpdateMeshAfterHeightChange();
    }

    /// <summary>
    /// Set height on all a list of cell indexes
    /// </summary>
    /// <param name="cells">list of cell indexes to operate on</param>
    /// <param name="height">the vertex height</param>
    public void SetHeightAndUpdate(List<(int x, int y)> cells, float height) {

        foreach((int x, int y) cellIndex in cells) {
            TerrainInstanceCellDataContainer cell = CellFromIndex(cellIndex.x, cellIndex.y);
            for (int i = 0; i < cell.VerticesIndexes.Length; i++) {
                meshData.SetVertexHeightAndUpdateColor(cell.VerticesIndexes[i], height, _terrainInstanceCreator.gradient.Evaluate);
            }
        }

        UpdateMeshAfterHeightChange();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="height"></param>
    public void SetHeightAndUpdate(TerrainInstanceCellDataContainer cell, float height)
    {
        for (int i = 0; i < cell.VerticesIndexes.Length; i++)
        {
            meshData.SetVertexHeightAndUpdateColor(cell.VerticesIndexes[i], meshData.VertexHeight(cell.VerticesIndexes[i]) + height, _terrainInstanceCreator.gradient.Evaluate);
        }
        
        UpdateMeshAfterHeightChange();
    }

    /// <summary>
    /// Increase height of vertices in cells by providing a start cell index and a size
    /// </summary>
    /// <param name="startIndex">index of the cell to start from</param>
    /// <param name="size">size to go from the start index in x,y</param>
    /// <param name="height">height increase amount</param>
    /// <param name="affectedVertices">get an list of vertex indexes and their heights that have been modified</param>
    /// <param name="colorCopyTerrain"></param>
    public void IncreaseHeightAndUpdate((int x, int y) startIndex, Vector2Int size, float height, in List<(int, double)> affectedVertices, TerrainInstance colorCopyTerrain) {

        List<(int x, int y)> cells = new List<(int x, int y)>((size.x + 1) * (size.y + 1));

        for (int y = 0; y < size.y + 1; y++) {
            for (int x = 0; x < size.x + 1; x++) {
                cells.Add((startIndex.x+x, startIndex.y+y));
            }
        }

        IncreaseHeightAndUpdate(cells, height, affectedVertices, colorCopyTerrain);
    }

    /// <summary>
    /// Increase height on a set of cells and update changes
    /// </summary>
    /// <param name="cells">list of cell indexes to apply to</param>
    /// <param name="height">height increase amount</param>
    /// <param name="affectedVertices">get an list of vertex indexes and their heights that have been modified</param>
    internal void IncreaseHeightAndUpdate(List<(int x,int y)> cells, float height, in List<(int, double)> affectedVertices, TerrainInstance colorCopyTerrain) {
        for (int c = 0; c < cells.Count; c++) {

            TerrainInstanceCellDataContainer cell = CellFromIndex(cells[c].x, cells[c].y);
            for (int i = 0; i < cell.VerticesIndexes.Length; i++) {

                meshData.IncreaseVertexHeightAndUpdateColor(cell.VerticesIndexes[i], height, _terrainInstanceCreator.gradient.Evaluate);

                if (colorCopyTerrain != null) {
                    // copy color
                    meshData.SetColorValue(cell.VerticesIndexes[i], _terrainInstanceCreator.gradient.Evaluate(0.0f) + colorCopyTerrain.meshData.VertexColor(cell.VerticesIndexes[i]) * 0.5f);
                }

                if (affectedVertices != null)
                    affectedVertices.Add((cell.VerticesIndexes[i], height));
            }
        }

        UpdateMeshAfterHeightChange();
    }

    /// <summary>
    /// Set color on all vertices and update changes
    /// </summary>
    /// <param name="color">the color to set</param>
    public void SetAllColorAndUpdate(Color color) {
        for (int i = 0; i < meshData.ColorCount; i++)
            meshData.SetColorValue(i, color);

        CopyVertexAndColorDataToGpu(meshData);
    }

    /// <summary>
    /// Assign a new data container and update the vertices based on it's vertex height data
    /// </summary>
    /// <param name="dataContainer">the data container to set</param>
    public void AssignDataContainerAndUpdateVertices(TerrainInstanceDataContainer dataContainer) {
        _dataContainer = dataContainer;
        for(int i=0; i<dataContainer.VertexHeightData.Count; i++) {
            meshData.SetVertexHeightAndUpdateColor(i, (float)dataContainer.VertexHeightData[i], _terrainInstanceCreator.gradient.Evaluate);
        }

        UpdateMeshAfterHeightChange();
    }


    /// <summary>
    /// Copy cell data height/color from one set of cells to another
    /// </summary>
    /// <param name="toTerrainInstance">the terrain instance to copy to</param>
    /// <param name="toCellStartIndex">start index of copy to cells/param>
    /// <param name="heightOffset">height offset on the copy to cell</param>
    /// <param name="fromCellStartIndex">start index of copy from cells</param>
    /// <param name="numberOfCells">size of copy area</param>
    /// <param name="markedCellIndexs">list to save marked cells</param>
    public void AssignCellDataToCell(TerrainInstance toTerrainInstance, Vector2Int toCellStartIndex, float heightOffset, Vector2Int fromCellStartIndex, Vector2Int numberOfCells, out List<(int x, int y)> markedCellIndexs) {

        markedCellIndexs = new List<(int x, int y)>((numberOfCells.x+1) * (numberOfCells.y+1));

        // reset tiles
        for (int i = 0; i < meshData.ActiveVerticesCount; i++)
            toTerrainInstance.meshData.SetActiveVertexStatus(i, 0);

        int counter = 0;
        for (int y = 0; y < numberOfCells.y+1; y++) {
            for(int x= 0; x < numberOfCells.x+1; x++) {

                // get cell to copy to
                TerrainInstanceCellDataContainer toCell = toTerrainInstance.CellFromIndex(toCellStartIndex.x + x, toCellStartIndex.y + y);

                // get cell to copy from
                TerrainInstanceCellDataContainer fromCell = CellFromIndex(fromCellStartIndex.x + x, fromCellStartIndex.y + y);

                // if we got both cells do the copy
                if (toCell != null && fromCell != null) {

                    markedCellIndexs.Add((fromCellStartIndex.x + x, fromCellStartIndex.y + y));

                    // copy vertex heights and assign active state
                    for (int i = 0; i < toCell.VerticesIndexes.Length; i++) {

                        // copy height and set vertex as active
                        toTerrainInstance.meshData.SetVertexHeight(toCell.VerticesIndexes[i], meshData.VertexHeight(fromCell.VerticesIndexes[i]) + heightOffset);
                        toTerrainInstance.meshData.SetActiveVertexStatus(toCell.VerticesIndexes[i], 1);

                        // copy color
                        toTerrainInstance.meshData.SetColorValue(toCell.VerticesIndexes[i], toTerrainInstance._terrainInstanceCreator.gradient.Evaluate(0.0f)+ meshData.VertexColor(fromCell.VerticesIndexes[i])*0.5f);
                    }
                }

                counter++;
            }
        }

        toTerrainInstance.UpdateMeshAfterHeightChange();
    }

    /// <summary>
    /// Copy cell data from one cell to another
    /// </summary>
    /// <param name="toTerrainInstance">the terrain instance to copy to</param>
    /// <param name="toCell">to the cell to copy to</param>
    /// <param name="heightOffset">height offset on the copy to cell</param>
    /// <param name="fromCell">the copy from cell</param>
    /// <param name="updateMesh">should we updated the mesh?</param>
    /*public void AssignCellDataToCell(TerrainInstanceCellDataContainer toCell, float heightOffset, TerrainInstanceCellDataContainer fromCell, bool updateMesh=true) {

        for (int i = 0; i < toCell.Owner.meshData.ActiveVerticesCount; i++)
            toCell.Owner.meshData.SetActiveVertexStatus(i, 0);

        // copy vertex height and set as active
        for (int i=0; i<toCell.VerticesIndexes.Length; i++) {
            toCell.Owner.meshData.SetVertexHeight(toCell.VerticesIndexes[i], meshData.VertexHeight(fromCell.VerticesIndexes[i]) + heightOffset);
            toCell.Owner.meshData.SetVertexHeight(toCell.VerticesIndexes[i], 1);

            // copy color
            toCell.Owner.meshData.SetColorValue(toCell.VerticesIndexes[i], toCell.Owner._terrainInstanceCreator.gradient.Evaluate(0.0f) + meshData.VertexColor(fromCell.VerticesIndexes[i]) * 0.5f);
        }

        if (updateMesh)
            toCell.Owner.UpdateMeshAfterHeightChange();
    }*/
    public void AssignCellDataToCell(TerrainInstance toTerrainInstance, TerrainInstanceCellDataContainer toCell, float heightOffset, TerrainInstanceCellDataContainer fromCell, bool updateMesh = true) {
        for (int i = 0; i < meshData.ActiveVerticesCount; i++)
            toTerrainInstance.meshData.SetActiveVertexStatus(i, 0);

        // copy vertex height and set as active
        for (int i = 0; i < toCell.VerticesIndexes.Length; i++)
        {
            toTerrainInstance.meshData.SetVertexHeight(toCell.VerticesIndexes[i], meshData.VertexHeight(fromCell.VerticesIndexes[i]) + heightOffset);
            toTerrainInstance.meshData.SetActiveVertexStatus(toCell.VerticesIndexes[i], 1);

            // copy color
            toTerrainInstance.meshData.SetColorValue(toCell.VerticesIndexes[i], toTerrainInstance._terrainInstanceCreator.gradient.Evaluate(0.0f) + meshData.VertexColor(fromCell.VerticesIndexes[i]) * 0.5f);
        }

        if (updateMesh)
            toTerrainInstance.UpdateMeshAfterHeightChange();
    }
}