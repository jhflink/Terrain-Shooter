using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeBufferContainer<T> {
    // the actual buffer
    private ComputeBuffer _computeBuffer = null;

    // the data held by the buffer
    private T[] _data = null;

    /// <summary>
    /// A container keeping a compute buffer and the data that will be sent to the gpu
    /// </summary>
    /// <param name="length">Number of elements in the buffer.</param>
    /// <param name="elementSize">Size of one element in the buffer. Has to match size of buffer type in the shader.</param>
    /// <param name="dataSize">Size of the data contaienr</param>
    public ComputeBufferContainer(int length, int elementSize, int dataSize) {
        _computeBuffer = new ComputeBuffer(length, elementSize, ComputeBufferType.Default);
        _data = new T[dataSize];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <param name="data"></param>
    public void SetData(int index, T data) {
        _data[index] = data;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <param name="data"></param>
    public void SetData(T[] data)
    {
        _data = data;
    }

    /// <summary>
    /// Release the buffer memory
    /// </summary>
    public void Release() {
        _computeBuffer.Release();
    }

    /// <summary>
    /// Send the data to the buffer in the shader
    /// </summary>
    /// <param name="meshRenderer"></param>
    /// <param name="dataNameInShader"></param>
    public void SendDataToMeshRenderer(MeshRenderer meshRenderer, string dataNameInShader) {
        _computeBuffer.SetData(_data);
        meshRenderer.material.SetBuffer(dataNameInShader, _computeBuffer);
    }
}
