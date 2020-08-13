using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class DarwClylinderPIpes : MonoBehaviour
{
    public Material m;
    MaterialPropertyBlock prop = null;

    [SerializeField] int Count = 20; 
    [SerializeField, Range(1f, 15f)] float width = 10f;
    [SerializeField, Range(1f, 5f)] float height = 3f;
    [SerializeField] float separate = 3f;
    [SerializeField, Range(0f, 1f)] float scaleMult = 1f;

    [SerializeField] float noiseScale = 3.0f;
    [SerializeField] float noiseSpeed = 1.0f;


    MeshFilter mf;
    Matrix4x4[] mat;

    // Start is called before the first frame update
    void Start()
    {
        mf = GetComponent<MeshFilter>();
        mat = new Matrix4x4[Count*2];
    }

    // Update is called once per frame
    void Update()
    {
        if(mat.Length != Count*2) mat = new Matrix4x4[Count*2];
        if(prop == null) prop = new MaterialPropertyBlock();

        float w = (width - separate)/2;
        float scaleXZ = w / Count;
        float step = scaleXZ;
        scaleXZ *= scaleMult;

        float startX = separate / 2; 

        for(int i = 0; i < Count; i++){
            Vector3 p0 = new Vector3(startX + step * i, transform.position.y, transform.position.z);
            Vector3 p1 = new Vector3(-(startX + step * i), transform.position.y, transform.position.z);

            float map = Mathf.SmoothStep(0.05f, 1, 1 - (p0.x - startX) / ((width/2) - startX));
            float scaleY0 = height * map * Mathf.Pow(Mathf.PerlinNoise(p0.x*noiseScale + Time.time * noiseSpeed, 0), 2);
            float scaleY1 = height * map * Mathf.Pow(Mathf.PerlinNoise(p1.x*noiseScale + Time.time * -noiseSpeed, 1), 2);

            mat[i * 2] = Matrix4x4.Translate(p0) * Matrix4x4.Scale(new Vector3(scaleXZ, scaleY0, scaleXZ));
            mat[(i * 2) + 1] = Matrix4x4.Translate(p1) * Matrix4x4.Scale(new Vector3(scaleXZ, scaleY1, scaleXZ));
        }

        Graphics.DrawMeshInstanced(
            mf.mesh, 0, m,
            mat, Count * 2, prop,
            ShadowCastingMode.TwoSided, true,
            gameObject.layer, null
        );
    }
}
