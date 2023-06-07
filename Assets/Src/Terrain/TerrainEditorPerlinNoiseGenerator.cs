using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainEditorPerlinNoiseGeneratorDataContainer {
    // width and height of noise
    public int sizeX;
    public int sizeY;

    // where to sample from in the noise (will be randomized)
    public float sampleOriginX;
    public float sampleOriginY;

    // the scale (zoom) of the noise
    public float scale;

    public int octaves;

    public float persistance;

    public float lacunarity;

    public TerrainEditorPerlinNoiseGeneratorDataContainer() { }
}

public class TerrainEditorPerlinNoiseGenerator {

    /// <summary>
    /// Generates a perlin noise based on the data container
    /// </summary>
    /// <param name="dataContainer">Container with the generation properites</param>
    /// <param name="noiseArray">The output array of noise values</param>
    public static void Generate(TerrainEditorPerlinNoiseGeneratorDataContainer dataContainer, out float[] noiseArray) {
        noiseArray = new float[dataContainer.sizeX * dataContainer.sizeY];

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        // Generate noise
        for (int y = 0; y < dataContainer.sizeY; y++) {
            for (int x = 0; x < dataContainer.sizeX; x++) {

                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int o = 0; o < dataContainer.octaves; o++) {
                    float xCoord = dataContainer.sampleOriginX + (float)x / dataContainer.sizeX * dataContainer.scale * frequency;
                    float yCoord = dataContainer.sampleOriginY + (float)y / dataContainer.sizeY * dataContainer.scale * frequency;

                    float result = Mathf.PerlinNoise(xCoord, yCoord) * 2 - 1;

                    noiseHeight += result * amplitude;

                    amplitude *= dataContainer.persistance;
                    frequency *= dataContainer.lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                    maxNoiseHeight = noiseHeight;

                if (noiseHeight < minNoiseHeight)
                    minNoiseHeight = noiseHeight;

                noiseArray[y * dataContainer.sizeX + x] = noiseHeight;
            }
        }

        // Apply the noise to specific coordinates
        for (int y = 0; y < dataContainer.sizeY; y++)
        {
            for (int x = 0; x < dataContainer.sizeX; x++)
            {
                noiseArray[y * dataContainer.sizeX + x] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseArray[y * dataContainer.sizeX + x]);
            }
        }
    }
}
