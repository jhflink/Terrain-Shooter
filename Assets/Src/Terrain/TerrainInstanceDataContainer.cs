using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Container to save and load vertex data of terrain
/// </summary>
public class TerrainInstanceDataContainer {
    public string Id = "";
    public (int x, int y) Size = (0, 0);

    public List<float> VertexHeightData = null;

    public TerrainInstanceDataContainer() { }

    public TerrainInstanceDataContainer((int x, int y) size, string id) {
        Id = id;
        Size = size;
    }

    /// <summary>
    /// Save data container to file
    /// </summary>
    /// <param name="dataContainerToSave"></param>
    /// <param name="filePath"></param>
    public static void SaveToFile(in TerrainInstanceDataContainer dataContainerToSave, string filePath) {
        string serializedData = IOUtility.SerializeObject(dataContainerToSave, typeof(TerrainInstanceDataContainer));
        IOUtility.CreateXML(serializedData, filePath);
    }

    /// <summary>
    /// Load data container from file
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static TerrainInstanceDataContainer LoadFromFile(string filePath) {
        TerrainInstanceDataContainer dataContainer = null;
        string serializedData = IOUtility.LoadXML(filePath);
        if (!string.IsNullOrEmpty(serializedData)) {
            dataContainer = (TerrainInstanceDataContainer)IOUtility.DeserializeObject(serializedData, typeof(TerrainInstanceDataContainer));
        }

        return dataContainer;
    }
}