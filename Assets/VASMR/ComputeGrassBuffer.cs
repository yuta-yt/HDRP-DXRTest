using System.Runtime.InteropServices;
using UnityEngine;

public class ComputeGrassBuffer : MonoBehaviour
{

    // Compute Shader param
    public ComputeShader compute;
    
    [SerializeField, Range(1f, 100f)]
    float sizeX;
    [SerializeField, Range(1f, 100f)]
    float sizeY;

    public float SizeX{
        get{return sizeX;}
    }

    public float SizeY{
        get{return sizeY;}
    }


    [SerializeField]
    int count = 100000;
    int kernel;

    // Noise param
    [SerializeField, Range(-1f, .95f)]
    float threshold;

    [SerializeField, Range(.01f, 1f)]
    float scale;

    [SerializeField]
    Vector2 offset;

    ComputeBuffer _PositionBuffer;
    public ComputeBuffer PositionBuffer{
        get{ return _PositionBuffer; }
    } 

    int groupSize;

    void Start()
    {
        int cq = Mathf.CeilToInt(Mathf.Sqrt(count));
        int rem = (cq * cq) - count;

        _PositionBuffer = new ComputeBuffer(count + rem, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Append);
        
        kernel = compute.FindKernel("SamplePos");
        groupSize = Mathf.FloorToInt(cq / 8);

        compute.SetInt("_groupSize", groupSize);
        compute.SetVector("_step", new Vector2(sizeX / (float)cq, sizeY / (float)cq));
        compute.SetVector("_size", new Vector2(sizeX, sizeY));
    }

    void Update()
    {
        _PositionBuffer.SetCounterValue(0);
        compute.SetBuffer(kernel, "_PositionBuffer", _PositionBuffer);

        compute.SetFloat("_threshold", threshold);
        compute.SetFloat("_scale", scale);
        compute.SetVector("_offset", new Vector3(offset.x, 0f, offset.y));

        compute.Dispatch(kernel, groupSize, groupSize, 1);
    }

    void onDestroy(){
        if(_PositionBuffer != null)
            _PositionBuffer.Release();
    }
}
