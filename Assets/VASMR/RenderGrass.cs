using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(ComputeGrassBuffer))]
public class RenderGrass : MonoBehaviour
{

    ComputeGrassBuffer computeGrassBuffer;

    ComputeBuffer argsBuffer;

    [SerializeField] Material material = null;
    MaterialPropertyBlock prop;

    // Geom param
    [SerializeField, Range(.001f, .1f)] float radius = .2f;
    [SerializeField, Range(.01f, .65f)] float length = .2f;
    [SerializeField, Range(.001f, .05f)] float bendForward = .0004f;
    [SerializeField, Range(1f, 4f)] float bendCurve = 2f;
    [SerializeField, Range(0f, 1f)] float rotRand = .5f;

    // Wind param
    [SerializeField] Vector3 wind = new Vector3(0f,0f,1f);
    [SerializeField, Range(0.1f, .5f)] float windSpeed = .25f;
    [SerializeField, Range(.01f, 2f)] float windScale = 1f;
    [SerializeField, Range(0f, .2f)] float windStrength = .05f;

    Vector3 eps = new Vector3(.0001f, .0001f, .0001f);

    void Start()
    {
        computeGrassBuffer = GetComponent<ComputeGrassBuffer>();

        argsBuffer = new ComputeBuffer(1, 16, ComputeBufferType.IndirectArguments);
        var args = new int[4]{0,1,0,0};
        argsBuffer.SetData(args);
    }

    void Update()
    {
        ComputeBuffer _PositionBuffer = computeGrassBuffer.PositionBuffer;
        ComputeBuffer.CopyCount(_PositionBuffer, argsBuffer, 0);    

        if (prop == null) prop = new MaterialPropertyBlock();
        prop.SetBuffer("_PositionBuffer", _PositionBuffer);  
        prop.SetFloat("_Radius", radius);
        prop.SetFloat("_Length", length);
        prop.SetFloat("_bendForward", bendForward);
        prop.SetFloat("_bendCurve", bendCurve);
        prop.SetFloat("_rotRand",rotRand);
        prop.SetVector("_Wind", Vector3.Normalize(wind + eps));
        prop.SetFloat("_windSpeed", windSpeed);
        prop.SetFloat("_windScale", windScale);
        prop.SetFloat("_windStrength", windStrength);
        prop.SetFloat("_time", Time.time);

        Vector3 size = new Vector3(computeGrassBuffer.SizeX * 1.5f, 5, computeGrassBuffer.SizeY * 1.5f);  

        Graphics.DrawProceduralIndirect(
            material,
            new Bounds(transform.localPosition, size),
            MeshTopology.Points,
            argsBuffer, 0, null, 
            prop, ShadowCastingMode.TwoSided, 
            true, gameObject.layer
        );
    }
}
