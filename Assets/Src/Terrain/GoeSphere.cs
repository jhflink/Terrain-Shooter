using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Terrain gizmo representing a floating sphere that can grow or shrink
/// </summary>
public class GoeSphere : MonoBehaviour, ITerrainEditorGizmoRepresentation
{
    public float MaxSphereSize = 3.0f;
    public bool Invisible = false;

    private Mesh _mesh = null;
    private MeshRenderer _meshRenderer = null;
    private MeshFilter _meshFilter = null;
    private float[] _vertexOffset = null;
    private ComputeBufferContainer<float> _vertexComputeBuffer = null;
    private Material _material = null;

    public bool Growing
    {
        set { _material.SetFloat("_Speed", value ? 20 : 10); }
    }

    // Start is called before the first frame update
    void Start()
    {
        ITerrainEditorGizmoRepresentation.CurrentRepresentation = this;

        _meshRenderer = GetComponent<MeshRenderer>();
        _meshFilter = GetComponent<MeshFilter>();
        _mesh = _meshFilter.mesh;
        _material = _meshRenderer.material;

        _vertexOffset = new float[_mesh.vertices.Length * 3];
        Vector3[] vertices = _mesh.vertices;
        for(int i=0, floatCounter = 0; i< vertices.Length; i++, floatCounter += 3)
        {
            _vertexOffset[floatCounter] = vertices[i].x;
            _vertexOffset[floatCounter+1] = vertices[i].y;
            _vertexOffset[floatCounter+2] = vertices[i].z;
        }
        //System.Array.Copy(_mesh.vertices, _vertexOffset, _mesh.vertices.Length);

        _vertexComputeBuffer = new ComputeBufferContainer<float>(_vertexOffset.Length * 3, sizeof(float) * 3, _vertexOffset.Length * 3);
        _vertexComputeBuffer.SetData(_vertexOffset);
        _vertexComputeBuffer.SendDataToMeshRenderer(_meshRenderer, "_vertexPositions");

        _meshRenderer.enabled = !Invisible;
    }

    public GameObject GameObject()
    {
        return gameObject;
    }

    public bool ShouldIncrease()
    {
        return transform.localScale.x <= 0.0f;
    }

    // Update is called once per frame
    float timer = 0.0f;
    void Update()
    {
        timer += Time.deltaTime;
        if(timer>=0.1f)
        {
            timer = 0.0f;
        }
    }

    void UpdateVertexOffset()
    {

    }

    public void Decrease(float amount, ref bool threasholdReached)
    {
        transform.localScale = new Vector3(transform.localScale.x - amount, transform.localScale.y - amount, transform.localScale.z - amount);

        if (transform.localScale.x < 0.0f)
        {
            Growing = false;
            transform.localScale = new Vector3(0.0f, 0.0f, 0.0f);
            threasholdReached = true;
        }
        else
            Growing = true;

        if (Invisible)
            threasholdReached = false;
    }

    public void Increase(float amount, ref bool threasholdReached)
    {
        transform.localScale = new Vector3(transform.localScale.x + amount, transform.localScale.y + amount, transform.localScale.z + amount);
        if (transform.localScale.x > MaxSphereSize)
        {
            Growing = false;
            transform.localScale = new Vector3(MaxSphereSize, MaxSphereSize, MaxSphereSize);
            threasholdReached = true;
        }
        else
            Growing = true;

        if (Invisible)
            threasholdReached = false;
    }

    public void ResetCooldown()
    {
        Growing = false;
    }
}
